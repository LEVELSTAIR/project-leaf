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

[RequireComponent(typeof(Terrain))]
public class TerrainGeneratorWithRivers : MonoBehaviour
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

    [Header("Rivers")]
    public bool carveRivers = true;
    public List<RiverDefinition> rivers = new List<RiverDefinition>();
    public float riverDepth = 0.15f;
    [Range(0.001f, 0.1f)] public float riverWidth = 0.02f;
    [Range(10, 100)] public int riverSplineSteps = 50;

    // ---------- Jobs ----------
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
        public int width;                         // heightmap resolution
        public float halfWidth;                   // river half‑width in pixels
        public float depth;
        [ReadOnly] public NativeArray<float2> pathPoints;   // dense path in pixel coordinates
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
                // Gaussian profile: 1 in centre, ~0 at edge
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
        public NativeArray<float4> weights; // x:grass, y:snow, z:desert, w:lush

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

    // ---------- Unity callbacks ----------
    Terrain terrain;
    TerrainData terrainData;

    void Awake()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;
        Generate();
    }

    public void Generate()
    {
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.alphamapResolution = alphamapResolution;
        terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);

        var rand = new Unity.Mathematics.Random((uint)seed);
        float2 hOff = rand.NextFloat2(-100000f, 100000f);
        float2 mOff = rand.NextFloat2(-100000f, 100000f);
        int total = heightmapResolution * heightmapResolution;

        // Height
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
        }
            .Schedule(total, 256).Complete();

        // Falloff
        NativeArray<float> falloff = new NativeArray<float>(total, Allocator.TempJob);
        BuildFalloff(falloff);
        new FalloffMultiplyJob { width = heightmapResolution, heightmap = rawHeight, falloff = falloff }
            .Schedule(total, 256).Complete();
        falloff.Dispose();

        // Rivers
        if (carveRivers && rivers != null)
        {
            float pixelHalfWidth = riverWidth * heightmapResolution; // half‑width in pixels

            foreach (var river in rivers)
            {
                if (river.controlPoints.Count < 2) continue;

                // Build smooth spline in UV space
                NativeArray<Vector2> uvPath = BuildSplinePoints(river.controlPoints, riverSplineSteps);
                if (uvPath.Length < 2)
                {
                    uvPath.Dispose();
                    continue;
                }

                // Convert to pixel coordinates
                NativeArray<float2> pixelPath = new NativeArray<float2>(uvPath.Length, Allocator.TempJob);
                float scale = (float)(heightmapResolution - 1);
                for (int i = 0; i < uvPath.Length; i++)
                {
                    Vector2 uv = uvPath[i];
                    pixelPath[i] = new float2(uv.x * scale, uv.y * scale);
                }
                uvPath.Dispose();

                // Carve
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

        // Apply
        float[,] h2D = new float[heightmapResolution, heightmapResolution];
        for (int y = 0; y < heightmapResolution; y++)
            for (int x = 0; x < heightmapResolution; x++)
                h2D[y, x] = rawHeight[y * heightmapResolution + x];
        terrainData.SetHeightsDelayLOD(0, 0, h2D);

        // Moisture
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
        }
            .Schedule(total, 256).Complete();

        // Splatmap
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
        }
            .Schedule(splatRes * splatRes, 256).Complete();

        int layers = terrainData.terrainLayers.Length;
        if (layers >= 4)
        {
            float[,,] alpha = new float[splatRes, splatRes, layers];
            for (int y = 0; y < splatRes; y++)
                for (int x = 0; x < splatRes; x++)
                {
                    float4 w = splatWeights[y * splatRes + x];
                    alpha[y, x, 0] = w.x; alpha[y, x, 1] = w.y;
                    alpha[y, x, 2] = w.z; alpha[y, x, 3] = w.w;
                }
            terrainData.SetAlphamaps(0, 0, alpha);
        }
        else Debug.LogError("Assign at least 4 terrain layers (Grass, Snow, Desert, Lush).");

        // Cleanup
        rawHeight.Dispose(); moist.Dispose(); splatWeights.Dispose();
        terrain.Flush();
    }

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

    void OnDrawGizmos()
    {
        if (rivers == null) return;
        foreach (var river in rivers)
        {
            if (river.controlPoints.Count < 2) continue;
            Gizmos.color = Color.blue;
            for (int i = 0; i < river.controlPoints.Count - 1; i++)
            {
                Vector3 p1 = new Vector3(river.controlPoints[i].x * terrainSize, 0, river.controlPoints[i].y * terrainSize);
                Vector3 p2 = new Vector3(river.controlPoints[i + 1].x * terrainSize, 0, river.controlPoints[i + 1].y * terrainSize);
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}