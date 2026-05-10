using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Burst job that tests AABB bounds against frustum planes.
/// </summary>
[BurstCompile]
struct FrustumCullJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float4> planes;   // 6 planes (left, right, bottom, top, near, far)
    [ReadOnly] public NativeArray<Bounds> bounds;
    public NativeArray<bool> results;               // true = visible

    public void Execute(int index)
    {
        Bounds b = bounds[index];
        float3 center = b.center;
        float3 extents = b.extents;

        bool inside = true;
        for (int i = 0; i < 6; i++)
        {
            float4 p = planes[i];
            float3 normal = p.xyz;
            float distance = p.w;

            // Project AABB onto plane normal
            float r = extents.x * math.abs(normal.x) + extents.y * math.abs(normal.y) + extents.z * math.abs(normal.z);
            float d = math.dot(normal, center) + distance;

            if (d + r < 0)
            {
                inside = false;
                break;
            }
        }
        results[index] = inside;
    }
}

public class HighPerfFrustumCullingManager : MonoBehaviour
{
    [Header("Target Setup")]
    public Transform rootContainer;               // All renderers under this will be managed
    public List<GameObject> customObjects;        // Or provide explicit list

    [Header("Culling Settings")]
    public bool enableCulling = true;
    public float updateInterval = 0.0f;           // 0 = every frame
    public CullTarget targetComponent = CullTarget.Renderer;
    public bool disableGameObject = false;

    public enum CullTarget { Renderer, Behaviour, ParticleSystem, LODGroup }

    [Header("Debug")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.cyan;

    private Camera _camera;
    private float _lastUpdateTime;

    // Native containers for job
    private NativeArray<Bounds> _boundsNative;
    private NativeArray<bool> _visibilityNative;
    private NativeArray<float4> _planesNative;

    // Managed lists
    private List<Component> _targetComponents = new List<Component>();
    private List<GameObject> _targetGameObjects = new List<GameObject>();

    private JobHandle _currentJobHandle;
    private bool _jobScheduled = false;

    // Optional: cache LODGroup bounds if they never change
    private Dictionary<LODGroup, Bounds> _cachedLODBounds = new Dictionary<LODGroup, Bounds>();

    private void Start()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            Debug.LogError("HighPerfFrustumCullingManager requires a Camera component.");
            enabled = false;
            return;
        }
        RefreshTargetList();
    }

    public void RefreshTargetList()
    {
        // Gather all target GameObjects
        List<GameObject> allTargets = new List<GameObject>();
        if (rootContainer != null)
        {
            Renderer[] renderers = rootContainer.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
                allTargets.Add(rend.gameObject);
        }
        else if (customObjects != null && customObjects.Count > 0)
        {
            allTargets.AddRange(customObjects);
        }
        else
        {
            Debug.LogWarning("No rootContainer or customObjects assigned.");
            return;
        }

        _targetComponents.Clear();
        _targetGameObjects.Clear();
        _cachedLODBounds.Clear();

        foreach (GameObject go in allTargets)
        {
            if (go == null) continue;

            if (disableGameObject)
            {
                _targetGameObjects.Add(go);
                continue;
            }

            Component comp = null;
            switch (targetComponent)
            {
                case CullTarget.Renderer:
                    comp = go.GetComponent<Renderer>();
                    break;
                case CullTarget.Behaviour:
                    comp = go.GetComponent<Behaviour>();
                    break;
                case CullTarget.ParticleSystem:
                    comp = go.GetComponent<ParticleSystem>();
                    break;
                case CullTarget.LODGroup:
                    comp = go.GetComponent<LODGroup>();
                    // Pre-cache bounds for static LOD Groups
                    if (comp != null && !_cachedLODBounds.ContainsKey((LODGroup)comp))
                        _cachedLODBounds[(LODGroup)comp] = CalculateLODGroupWorldBounds((LODGroup)comp);
                    break;
            }

            if (comp != null)
                _targetComponents.Add(comp);
            else
                Debug.LogWarning($"No {targetComponent} component on {go.name}", go);
        }

        int count = disableGameObject ? _targetGameObjects.Count : _targetComponents.Count;
        if (count == 0) return;

        // Recreate native arrays
        if (_boundsNative.IsCreated) _boundsNative.Dispose();
        if (_visibilityNative.IsCreated) _visibilityNative.Dispose();

        _boundsNative = new NativeArray<Bounds>(count, Allocator.Persistent);
        _visibilityNative = new NativeArray<bool>(count, Allocator.Persistent);
        _planesNative = new NativeArray<float4>(6, Allocator.Persistent);
    }

    private void Update()
    {
        if (!enableCulling) return;

        if (updateInterval <= 0f)
            PerformCulling();
        else if (Time.time - _lastUpdateTime >= updateInterval)
        {
            _lastUpdateTime = Time.time;
            PerformCulling();
        }
    }

    private void PerformCulling()
    {
        if (_camera == null) return;
        if (!_boundsNative.IsCreated || _boundsNative.Length == 0) return;

        // Complete previous job
        if (_jobScheduled)
            _currentJobHandle.Complete();

        // Update bounds from current transforms
        UpdateBoundsArray();

        // Update frustum planes
        float4[] planes = FrustumCuller.GetFrustumPlanesFloat4(_camera);
        for (int i = 0; i < 6; i++)
            _planesNative[i] = planes[i];

        // Schedule culling job
        FrustumCullJob job = new FrustumCullJob
        {
            planes = _planesNative,
            bounds = _boundsNative,
            results = _visibilityNative
        };
        _currentJobHandle = job.Schedule(_boundsNative.Length, 64);
        _jobScheduled = true;
        JobHandle.ScheduleBatchedJobs(); // optional, helps kick off job
    }

    private void UpdateBoundsArray()
    {
        int count = _boundsNative.Length;
        if (disableGameObject)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject go = _targetGameObjects[i];
                if (go != null && go.activeInHierarchy)
                {
                    Renderer rend = go.GetComponent<Renderer>();
                    if (rend != null)
                        _boundsNative[i] = rend.bounds;
                    else
                        _boundsNative[i] = new Bounds(go.transform.position, Vector3.one * 0.1f);
                }
                else
                {
                    _boundsNative[i] = new Bounds(Vector3.zero, Vector3.zero);
                }
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                Component comp = _targetComponents[i];
                if (comp != null && comp.gameObject.activeInHierarchy)
                {
                    Renderer rend = comp as Renderer;
                    if (rend != null)
                    {
                        _boundsNative[i] = rend.bounds;
                    }
                    else
                    {
                        LODGroup lod = comp as LODGroup;
                        if (lod != null)
                        {
                            // Use cached bounds if available (for static objects)
                            if (_cachedLODBounds.TryGetValue(lod, out Bounds cached))
                                _boundsNative[i] = cached;
                            else
                                _boundsNative[i] = CalculateLODGroupWorldBounds(lod);
                        }
                        else
                        {
                            // Fallback: point test at transform position
                            _boundsNative[i] = new Bounds(comp.transform.position, Vector3.one * 0.1f);
                        }
                    }
                }
                else
                {
                    _boundsNative[i] = new Bounds(Vector3.zero, Vector3.zero);
                }
            }
        }
    }

    /// <summary>
    /// Calculates the world-space axis-aligned bounding box of all renderers in an LOD Group.
    /// </summary>
    private Bounds CalculateLODGroupWorldBounds(LODGroup lodGroup)
    {
        Bounds totalBounds = new Bounds();
        bool hasBounds = false;

        LOD[] lods = lodGroup.GetLODs();
        foreach (LOD lod in lods)
        {
            foreach (Renderer renderer in lod.renderers)
            {
                if (renderer == null) continue;
                if (!hasBounds)
                {
                    totalBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    totalBounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (!hasBounds)
            totalBounds = new Bounds(lodGroup.transform.position, Vector3.one * 0.1f);

        return totalBounds;
    }

    private void LateUpdate()
    {
        if (!_jobScheduled) return;

        _currentJobHandle.Complete();
        _jobScheduled = false;
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        int count = _visibilityNative.Length;
        if (disableGameObject)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject go = _targetGameObjects[i];
                if (go == null) continue;
                bool visible = _visibilityNative[i];
                if (go.activeSelf != visible)
                    go.SetActive(visible);
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                Component comp = _targetComponents[i];
                if (comp == null) continue;
                bool visible = _visibilityNative[i];
                SetComponentActive(comp, visible);
            }
        }
    }

    private void SetComponentActive(Component comp, bool active)
    {
        switch (targetComponent)
        {
            case CullTarget.Renderer:
                ((Renderer)comp).enabled = active;
                break;
            case CullTarget.Behaviour:
                ((Behaviour)comp).enabled = active;
                break;
            case CullTarget.ParticleSystem:
                var ps = (ParticleSystem)comp;
                if (active && !ps.isPlaying) ps.Play();
                else if (!active && ps.isPlaying) ps.Stop();
                break;
            case CullTarget.LODGroup:
                ((LODGroup)comp).enabled = active;
                break;
        }
    }

    private void OnDestroy()
    {
        if (_boundsNative.IsCreated) _boundsNative.Dispose();
        if (_visibilityNative.IsCreated) _visibilityNative.Dispose();
        if (_planesNative.IsCreated) _planesNative.Dispose();
        if (_jobScheduled) _currentJobHandle.Complete();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || _camera == null) return;
        Gizmos.color = gizmoColor;
        Matrix4x4 temp = Gizmos.matrix;
        Gizmos.matrix = _camera.transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, _camera.fieldOfView, _camera.farClipPlane, _camera.nearClipPlane, _camera.aspect);
        Gizmos.matrix = temp;
    }
}