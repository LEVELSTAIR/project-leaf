using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public class RiverDefinition
{
    public List<Vector2> controlPoints = new List<Vector2>
    {
        new Vector2(0.2f, 0.8f),
        new Vector2(0.8f, 0.2f)
    };
}

[System.Serializable]
public class FlatPlainDefinition
{
    public string name = "Plain";
    public List<Vector2> polygonPoints = new List<Vector2>()
    {
        new Vector2(0.4f, 0.4f),
        new Vector2(0.6f, 0.4f),
        new Vector2(0.6f, 0.6f),
        new Vector2(0.4f, 0.6f)
    };
    [Range(0f, 1f)] public float height = 0.2f;
}

[RequireComponent(typeof(Terrain))]
public class TerrainGeneratorNew : MonoBehaviour
{
    [Header("General")]
    public int seed = 42;
    public int heightmapResolution = 2049;
    public int alphamapResolution = 2048;
    public float terrainSize = 2048f;
    public float terrainHeight = 600f;

    [Header("Height Noise")]
    public float noiseScale = 400f;
    public int octaves = 6;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Moisture Noise")]
    public float moistureNoiseScale = 500f;
    public int moistureOctaves = 4;
    [Range(0f, 1f)] public float moisturePersistence = 0.4f;
    public float moistureLacunarity = 2.5f;

    [Header("Biome Thresholds")]
    [Range(0f, 1f)] public float snowHeight = 0.7f;
    [Range(0f, 1f)] public float lowElevationMax = 0.3f;
    [Range(0f, 1f)] public float dryMoistureMax = 0.3f;

    [Header("Falloff")]
    [Range(0f, 1f)] public float falloffInnerRadius = 0.5f;
    [Range(0f, 1f)] public float falloffOuterRadius = 0.98f;

    [Header("Flat Plains")]
    public List<FlatPlainDefinition> flatPlains = new List<FlatPlainDefinition>();

    [Header("Plain From Assigned Object")]
    public GameObject assignedObjectForPlain;
    [Range(0f, 1f)] public float assignedObjectPlainHeight = 0.2f;

    [Header("Rivers")]
    public bool carveRivers = true;
    public List<RiverDefinition> rivers = new List<RiverDefinition>();
    public float riverDepth = 0.15f;
    [Range(0.001f, 0.1f)] public float riverWidth = 0.02f;
    [Range(10, 100)] public int riverSplineSteps = 50;

    // ---------- Jobs (same as before) ----------
    [BurstCompile]
    struct HeightNoiseJob : IJobParallelFor
    {
        public int width;
        public float scale;
        public int octaves;
        public float persistence, lacunarity;
        public float2 offset;
        public NativeArray<float> heightmap;
        public void Execute(int i)
        {
            int x = i % width, y = i / width;
            float amp = 1f, freq = 1f, val = 0f, maxV = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float sx = (x + offset.x) / scale * freq;
                float sy = (y + offset.y) / scale * freq;
                val += noise.cnoise(new float2(sx, sy)) * amp;
                maxV += amp;
                amp *= persistence;
                freq *= lacunarity;
            }
            heightmap[i] = (val / maxV + 1f) * 0.5f;
        }
    }

    [BurstCompile]
    struct MoistureNoiseJob : IJobParallelFor
    {
        public int width;
        public float scale;
        public int octaves;
        public float persistence, lacunarity;
        public float2 offset;
        public NativeArray<float> moisture;
        public void Execute(int i)
        {
            int x = i % width, y = i / width;
            float amp = 1f, freq = 1f, val = 0f, maxV = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float sx = (x + offset.x) / scale * freq;
                float sy = (y + offset.y) / scale * freq;
                val += noise.cnoise(new float2(sx, sy)) * amp;
                maxV += amp;
                amp *= persistence;
                freq *= lacunarity;
            }
            moisture[i] = math.saturate((val / maxV + 1f) * 0.5f);
        }
    }

    [BurstCompile]
    struct FalloffMultiplyJob : IJobParallelFor
    {
        public int width;
        public NativeArray<float> heightmap;
        [ReadOnly] public NativeArray<float> falloff;
        public void Execute(int i) { heightmap[i] *= falloff[i]; }
    }

    [BurstCompile]
    struct RiverCarveJob : IJobParallelFor
    {
        public int width;
        public float halfWidth;
        public float depth;
        [ReadOnly] public NativeArray<float2> pathPoints;
        public NativeArray<float> heightmap;

        public void Execute(int i)
        {
            int x = i % width;
            int y = i / width;
            float2 pixelPos = new float2(x, y);

            float minDistSq = float.MaxValue;
            for (int j = 0; j < pathPoints.Length - 1; j++)
            {
                float2 a = pathPoints[j];
                float2 b = pathPoints[j + 1];
                float dSq = DistSqPointToSegment(pixelPos, a, b);
                if (dSq < minDistSq) minDistSq = dSq;
            }

            if (minDistSq < halfWidth * halfWidth)
            {
                float dist = math.sqrt(minDistSq);
                float t = dist / halfWidth;
                float carveFactor = math.exp(-t * t * 3f);
                heightmap[i] = math.max(heightmap[i] - depth * carveFactor, 0f);
            }
        }

        float DistSqPointToSegment(float2 p, float2 a, float2 b)
        {
            float2 ab = b - a;
            float2 ap = p - a;
            float t = math.dot(ap, ab) / math.dot(ab, ab);
            t = math.clamp(t, 0f, 1f);
            float2 closest = a + t * ab;
            return math.distancesq(p, closest);
        }
    }

    [BurstCompile]
    struct SplatmapJob : IJobParallelFor
    {
        public int width, heightmapRes;
        [ReadOnly] public NativeArray<float> heightmap, moisture;
        public float snowHeight, lowElevationMax, dryMoistureMax;
        public NativeArray<float4> weights;

        public void Execute(int i)
        {
            int x = i % width, y = i / width;
            float2 uv = new float2((float)x / (width - 1), (float)y / (width - 1));
            float h = SampleBilinear(heightmap, heightmapRes, uv.x, uv.y);
            float m = SampleBilinear(moisture, heightmapRes, uv.x, uv.y);
            float4 w = float4.zero;
            if (h >= snowHeight) w.y = 1f;
            else if (h <= lowElevationMax)
            {
                if (m <= dryMoistureMax) w.z = 1f; else w.w = 1f;
            }
            else w.x = 1f;
            weights[i] = w;
        }

        float SampleBilinear(NativeArray<float> map, int res, float u, float v)
        {
            float fx = u * (res - 1), fy = v * (res - 1);
            int x0 = math.clamp((int)math.floor(fx), 0, res - 1);
            int x1 = math.clamp(x0 + 1, 0, res - 1);
            int y0 = math.clamp((int)math.floor(fy), 0, res - 1);
            int y1 = math.clamp(y0 + 1, 0, res - 1);
            float tx = fx - x0, ty = fy - y0;
            float v00 = map[y0 * res + x0], v10 = map[y0 * res + x1];
            float v01 = map[y1 * res + x0], v11 = map[y1 * res + x1];
            return math.lerp(math.lerp(v00, v10, tx), math.lerp(v01, v11, tx), ty);
        }
    }

    // ---------- Public Methods ----------
    [ContextMenu("Generate Terrain")]
    public void Generate()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("TerrainGeneratorNew: No Terrain component found on this GameObject.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            Debug.LogError("TerrainGeneratorNew: TerrainData is missing.");
            return;
        }

        terrainData.heightmapResolution = heightmapResolution;
        terrainData.alphamapResolution = alphamapResolution;
        terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);

        var rand = new Unity.Mathematics.Random((uint)seed);
        float2 hOff = rand.NextFloat2(-100000f, 100000f);
        float2 mOff = rand.NextFloat2(-100000f, 100000f);
        int total = heightmapResolution * heightmapResolution;

        // 1. Height noise
        NativeArray<float> rawHeight = new NativeArray<float>(total, Allocator.TempJob);
        new HeightNoiseJob
        {
            width = heightmapResolution,
            scale = noiseScale,
            octaves = octaves,
            persistence = persistence,
            lacunarity = lacunarity,
            offset = hOff,
            heightmap = rawHeight
        }.Schedule(total, 256).Complete();

        // 2. Falloff
        NativeArray<float> falloff = new NativeArray<float>(total, Allocator.TempJob);
        BuildFalloff(falloff);
        new FalloffMultiplyJob { width = heightmapResolution, heightmap = rawHeight, falloff = falloff }
            .Schedule(total, 256).Complete();
        falloff.Dispose();

        // 3. Flat plains
        if (flatPlains != null && flatPlains.Count > 0)
        {
            ApplyFlatPlains(rawHeight, heightmapResolution);
        }

        // 4. Rivers
        if (carveRivers && rivers != null)
        {
            float pixelHalfWidth = riverWidth * heightmapResolution;
            foreach (var river in rivers)
            {
                if (river.controlPoints.Count < 2) continue;
                NativeArray<Vector2> uvPath = BuildSplinePoints(river.controlPoints, riverSplineSteps);
                if (uvPath.Length < 2)
                {
                    uvPath.Dispose();
                    continue;
                }
                NativeArray<float2> pixelPath = new NativeArray<float2>(uvPath.Length, Allocator.TempJob);
                float scale = (float)(heightmapResolution - 1);
                for (int i = 0; i < uvPath.Length; i++)
                {
                    Vector2 uv = uvPath[i];
                    pixelPath[i] = new float2(uv.x * scale, uv.y * scale);
                }
                uvPath.Dispose();
                var carveJob = new RiverCarveJob
                {
                    width = heightmapResolution,
                    halfWidth = pixelHalfWidth,
                    depth = riverDepth,
                    pathPoints = pixelPath,
                    heightmap = rawHeight
                };
                carveJob.Schedule(total, 256).Complete();
                pixelPath.Dispose();
            }
        }

        // Apply heights
        float[,] h2D = new float[heightmapResolution, heightmapResolution];
        for (int y = 0; y < heightmapResolution; y++)
            for (int x = 0; x < heightmapResolution; x++)
                h2D[y, x] = rawHeight[y * heightmapResolution + x];
        terrainData.SetHeightsDelayLOD(0, 0, h2D);

        // 5. Moisture noise
        NativeArray<float> moist = new NativeArray<float>(total, Allocator.TempJob);
        new MoistureNoiseJob
        {
            width = heightmapResolution,
            scale = moistureNoiseScale,
            octaves = moistureOctaves,
            persistence = moisturePersistence,
            lacunarity = moistureLacunarity,
            offset = mOff,
            moisture = moist
        }.Schedule(total, 256).Complete();

        // 6. Splatmap
        int splatRes = alphamapResolution;
        NativeArray<float4> splatWeights = new NativeArray<float4>(splatRes * splatRes, Allocator.TempJob);
        new SplatmapJob
        {
            width = splatRes,
            heightmapRes = heightmapResolution,
            heightmap = rawHeight,
            moisture = moist,
            snowHeight = snowHeight,
            lowElevationMax = lowElevationMax,
            dryMoistureMax = dryMoistureMax,
            weights = splatWeights
        }.Schedule(splatRes * splatRes, 256).Complete();

        int layers = terrainData.terrainLayers.Length;
        if (layers >= 4)
        {
            float[,,] alpha = new float[splatRes, splatRes, layers];
            for (int y = 0; y < splatRes; y++)
                for (int x = 0; x < splatRes; x++)
                {
                    float4 w = splatWeights[y * splatRes + x];
                    alpha[y, x, 0] = w.x;
                    alpha[y, x, 1] = w.y;
                    alpha[y, x, 2] = w.z;
                    alpha[y, x, 3] = w.w;
                }
            terrainData.SetAlphamaps(0, 0, alpha);
        }
        else Debug.LogError("Assign at least 4 terrain layers (Grass, Snow, Desert, Lush).");

        rawHeight.Dispose();
        moist.Dispose();
        splatWeights.Dispose();
        terrain.Flush();
    }

    // ---------- Flat Plains Logic ----------
    void ApplyFlatPlains(NativeArray<float> heightmap, int resolution)
    {
        int width = resolution;
        int height = resolution;
        float maxCoord = width - 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 uv = new Vector2(x / maxCoord, y / maxCoord);
                int index = y * width + x;

                foreach (var plain in flatPlains)
                {
                    if (plain.polygonPoints.Count < 3) continue;
                    if (IsPointInPolygon(uv, plain.polygonPoints))
                    {
                        heightmap[index] = plain.height;
                        break;
                    }
                }
            }
        }
    }

    bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        bool inside = false;
        int count = polygon.Count;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 vi = polygon[i];
            Vector2 vj = polygon[j];
            bool intersect = ((vi.y > point.y) != (vj.y > point.y)) &&
                             (point.x < (vj.x - vi.x) * (point.y - vi.y) / (vj.y - vi.y) + vi.x);
            if (intersect) inside = !inside;
        }
        return inside;
    }

    // ---------- Falloff ----------
    void BuildFalloff(NativeArray<float> falloff)
    {
        float half = heightmapResolution * 0.5f;
        float inner = falloffInnerRadius * half, outer = falloffOuterRadius * half;
        for (int y = 0; y < heightmapResolution; y++)
            for (int x = 0; x < heightmapResolution; x++)
            {
                float dx = x - half, dy = y - half;
                float dist = math.sqrt(dx * dx + dy * dy);
                float t = math.saturate((dist - inner) / (outer - inner));
                falloff[y * heightmapResolution + x] = 1f - t;
            }
    }

    // ---------- Spline Helpers ----------
    NativeArray<Vector2> BuildSplinePoints(List<Vector2> pts, int stepsPerSeg)
    {
        if (pts.Count < 2) return new NativeArray<Vector2>(0, Allocator.TempJob);
        int total = (pts.Count - 1) * stepsPerSeg + 1;
        NativeArray<Vector2> arr = new NativeArray<Vector2>(total, Allocator.TempJob);
        int idx = 0;
        for (int seg = 0; seg < pts.Count - 1; seg++)
        {
            Vector2 p0 = pts[Mathf.Max(seg - 1, 0)];
            Vector2 p1 = pts[seg];
            Vector2 p2 = pts[seg + 1];
            Vector2 p3 = pts[Mathf.Min(seg + 2, pts.Count - 1)];
            for (int s = 0; s < stepsPerSeg; s++)
                arr[idx++] = CatmullRom(p0, p1, p2, p3, s / (float)stepsPerSeg);
        }
        arr[idx] = pts[pts.Count - 1];
        return arr;
    }

    Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    // ---------- Create Plain From 3D Object (core logic) ----------
    public FlatPlainDefinition CreatePlainUnderObject(GameObject obj, float height, string plainName = "PlainFromObject")
    {
        if (obj == null) return null;

        // 1. Try to get a SphereCollider → create circular plain
        SphereCollider sphere = obj.GetComponent<SphereCollider>();
        if (sphere != null)
        {
            Vector3 center = sphere.bounds.center;
            float radius = sphere.radius * Mathf.Max(obj.transform.lossyScale.x, obj.transform.lossyScale.z);
            return CreateCircularPlain(center, radius, height, plainName);
        }

        // 2. Fallback to rectangular plain (using renderer or collider bounds)
        Bounds bounds = GetObjectBounds(obj);
        if (bounds.size.magnitude < 0.01f) return null;

        Terrain terrain = GetComponent<Terrain>();
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        float minU = (bounds.min.x - terrainPos.x) / terrainSize.x;
        float maxU = (bounds.max.x - terrainPos.x) / terrainSize.x;
        float minV = (bounds.min.z - terrainPos.z) / terrainSize.z;
        float maxV = (bounds.max.z - terrainPos.z) / terrainSize.z;

        // clamp & validate...
        List<Vector2> rect = new List<Vector2>
    {
        new Vector2(minU, minV),
        new Vector2(maxU, minV),
        new Vector2(maxU, maxV),
        new Vector2(minU, maxV)
    };

        FlatPlainDefinition plain = new FlatPlainDefinition { name = plainName, polygonPoints = rect, height = height };
        flatPlains.Add(plain);
        return plain;
    }

    // New helper: creates a circular plain (approximated by a polygon with 32 points)
    private FlatPlainDefinition CreateCircularPlain(Vector3 worldCenter, float worldRadius, float height, string name)
    {
        Terrain terrain = GetComponent<Terrain>();
        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        // Convert world center to UV coordinates
        float centerU = (worldCenter.x - terrainPos.x) / terrainSize.x;
        float centerV = (worldCenter.z - terrainPos.z) / terrainSize.z;
        float radiusU = worldRadius / terrainSize.x;
        float radiusV = worldRadius / terrainSize.z;

        int segments = 32;
        List<Vector2> circlePoints = new List<Vector2>();
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float u = centerU + Mathf.Cos(angle) * radiusU;
            float v = centerV + Mathf.Sin(angle) * radiusV;
            circlePoints.Add(new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v)));
        }

        FlatPlainDefinition plain = new FlatPlainDefinition { name = name, polygonPoints = circlePoints, height = height };
        flatPlains.Add(plain);
        return plain;
    }

    Bounds GetObjectBounds(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) return renderer.bounds;
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) return collider.bounds;
        return new Bounds(obj.transform.position, Vector3.one * 0.1f);
    }

    // ---------- Editor Context Menus ----------
#if UNITY_EDITOR
    [ContextMenu("Create Plain From Selected Object")]
    private void CreatePlainFromSelectedObject_Editor()
    {
        if (UnityEditor.Selection.activeGameObject == null)
        {
            Debug.LogError("No GameObject selected. Select an object in the Hierarchy.");
            return;
        }
        GameObject selected = UnityEditor.Selection.activeGameObject;
        CreatePlainUnderObject(selected, assignedObjectPlainHeight, "Plain_" + selected.name);
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"Plain created under '{selected.name}'. Right-click again and select 'Generate Terrain' to apply.");
    }

    [ContextMenu("Create Plain From Assigned Object")]
    private void CreatePlainFromAssignedObject_Editor()
    {
        if (assignedObjectForPlain == null)
        {
            Debug.LogError("No GameObject assigned to 'assignedObjectForPlain' field. Drag a 3D object into that field first.");
            return;
        }
        CreatePlainUnderObject(assignedObjectForPlain, assignedObjectPlainHeight, "Plain_" + assignedObjectForPlain.name);
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"Plain created from assigned object '{assignedObjectForPlain.name}'. Right-click again and select 'Generate Terrain' to apply.");
    }
#endif

    // ---------- Gizmos ----------
    void OnDrawGizmos()
    {
        if (rivers != null)
        {
            float size = terrainSize;
            Terrain t = GetComponent<Terrain>();
            if (t != null && t.terrainData != null)
                size = t.terrainData.size.x;

            foreach (var river in rivers)
            {
                if (river.controlPoints.Count < 2) continue;
                Gizmos.color = Color.blue;
                for (int i = 0; i < river.controlPoints.Count - 1; i++)
                {
                    Vector3 p1 = new Vector3(river.controlPoints[i].x * size, 0, river.controlPoints[i].y * size);
                    Vector3 p2 = new Vector3(river.controlPoints[i + 1].x * size, 0, river.controlPoints[i + 1].y * size);
                    Gizmos.DrawLine(p1, p2);
                }
            }
        }

        if (flatPlains != null)
        {
            float size = terrainSize;
            Terrain t = GetComponent<Terrain>();
            if (t != null && t.terrainData != null)
                size = t.terrainData.size.x;

            Gizmos.color = Color.green;
            foreach (var plain in flatPlains)
            {
                if (plain.polygonPoints.Count < 3) continue;
                for (int i = 0; i < plain.polygonPoints.Count; i++)
                {
                    Vector3 a = new Vector3(plain.polygonPoints[i].x * size, 0.1f, plain.polygonPoints[i].y * size);
                    Vector3 b = new Vector3(plain.polygonPoints[(i + 1) % plain.polygonPoints.Count].x * size, 0.1f, plain.polygonPoints[(i + 1) % plain.polygonPoints.Count].y * size);
                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }
}
