using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGenerator : MonoBehaviour
{
    [Header("General Settings")]
    public int seed = 42;
    public int heightmapResolution = 2049;   // must be 2^n + 1
    public int alphamapResolution = 2048;    // power of two
    public float terrainSize = 2048f;
    public float terrainHeight = 600f;

    [Header("Height Noise")]
    public float noiseScale = 400f;
    public int octaves = 6;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Moisture Noise (independent)")]
    public float moistureNoiseScale = 500f;
    public int moistureOctaves = 4;
    [Range(0f, 1f)] public float moisturePersistence = 0.4f;
    public float moistureLacunarity = 2.5f;

    [Header("Biome Thresholds")]
    [Range(0f, 1f)] public float snowHeight = 0.7f;      // above this → snow
    [Range(0f, 1f)] public float lowElevationMax = 0.3f; // below this → desert / lush
    [Range(0f, 1f)] public float dryMoistureMax = 0.3f;  // below this → desert

    [Header("Falloff (circular island)")]
    [Range(0f, 1f)] public float falloffInnerRadius = 0.5f;  // start of falloff
    [Range(0f, 1f)] public float falloffOuterRadius = 0.98f; // fully zero at edge

    // --- Burst-compatible jobs ---

    [BurstCompile]
    private struct HeightNoiseJob : IJobParallelFor
    {
        public int width;
        public float scale;
        public int octaves;
        public float persistence;
        public float lacunarity;
        public float2 offset;
        public NativeArray<float> heightmap;

        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;
            float amplitude = 1f;
            float frequency = 1f;
            float noiseVal = 0f;
            float maxVal = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sx = (x + offset.x) / scale * frequency;
                float sy = (y + offset.y) / scale * frequency;
                noiseVal += noise.cnoise(new float2(sx, sy)) * amplitude;
                maxVal += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            // Normalise to roughly 0-1
            heightmap[index] = (noiseVal / maxVal + 1f) * 0.5f;
        }
    }

    [BurstCompile]
    private struct MoistureNoiseJob : IJobParallelFor
    {
        public int width;
        public float scale;
        public int octaves;
        public float persistence;
        public float lacunarity;
        public float2 offset;
        public NativeArray<float> moisture;

        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;
            float amplitude = 1f;
            float frequency = 1f;
            float noiseVal = 0f;
            float maxVal = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sx = (x + offset.x) / scale * frequency;
                float sy = (y + offset.y) / scale * frequency;
                noiseVal += noise.cnoise(new float2(sx, sy)) * amplitude;
                maxVal += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            moisture[index] = math.saturate((noiseVal / maxVal + 1f) * 0.5f);
        }
    }

    [BurstCompile]
    private struct FalloffMultiplyJob : IJobParallelFor
    {
        public int width;
        public NativeArray<float> heightmap;
        [ReadOnly] public NativeArray<float> falloff;

        public void Execute(int index)
        {
            heightmap[index] *= falloff[index];
        }
    }

    [BurstCompile]
    private struct SplatmapJob : IJobParallelFor
    {
        public int width; // alphamap resolution
        public int heightmapRes; // full heightmap resolution
        [ReadOnly] public NativeArray<float> heightmap;
        [ReadOnly] public NativeArray<float> moisture;
        public float snowHeight;
        public float lowElevationMax;
        public float dryMoistureMax;
        public NativeArray<float4> splatWeights; // float4 for 4 layers

        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;

            // Sample height & moisture at this alphamap pixel
            // Bilinear sample to avoid sharp edges (optional)
            float2 uv = new float2((float)x / (width - 1), (float)y / (width - 1));
            float h = SampleBilinear(heightmap, heightmapRes, uv.x, uv.y);
            float m = SampleBilinear(moisture, heightmapRes, uv.x, uv.y);

            // Biome decision
            float4 weights = float4.zero;

            if (h >= snowHeight)
            {
                weights.y = 1f; // Snow
            }
            else if (h <= lowElevationMax)
            {
                if (m <= dryMoistureMax)
                    weights.z = 1f; // Desert
                else
                    weights.w = 1f; // Lush
            }
            else
            {
                weights.x = 1f; // Grass (default)
            }

            splatWeights[index] = weights;
        }

        private float SampleBilinear(NativeArray<float> map, int res, float u, float v)
        {
            float fx = u * (res - 1);
            float fy = v * (res - 1);
            int x0 = math.clamp((int)math.floor(fx), 0, res - 1);
            int x1 = math.clamp(x0 + 1, 0, res - 1);
            int y0 = math.clamp((int)math.floor(fy), 0, res - 1);
            int y1 = math.clamp(y0 + 1, 0, res - 1);
            float tx = fx - x0;
            float ty = fy - y0;

            float v00 = map[y0 * res + x0];
            float v10 = map[y0 * res + x1];
            float v01 = map[y1 * res + x0];
            float v11 = map[y1 * res + x1];

            return math.lerp(math.lerp(v00, v10, tx), math.lerp(v01, v11, tx), ty);
        }
    }
    // --- End of jobs ---

    private Terrain terrain;
    private TerrainData terrainData;

    private void Awake()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;
        Generate();
    }

    public void Generate()
    {
        // Initialise terrain settings
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.alphamapResolution = alphamapResolution;
        terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);

        // Get consistent random offsets from seed
        Unity.Mathematics.Random rand = new Unity.Mathematics.Random((uint)seed);
        float2 heightOffset = rand.NextFloat2(-100000f, 100000f);
        float2 moistureOffset = new float2(rand.NextFloat(-100000f, 100000f), rand.NextFloat(-100000f, 100000f));

        // 1. Generate heightmap
        int totalPixels = heightmapResolution * heightmapResolution;
        NativeArray<float> rawHeight = new NativeArray<float>(totalPixels, Allocator.TempJob);

        var heightJob = new HeightNoiseJob
        {
            width = heightmapResolution,
            scale = noiseScale,
            octaves = octaves,
            persistence = persistence,
            lacunarity = lacunarity,
            offset = heightOffset,
            heightmap = rawHeight
        };
        heightJob.Schedule(totalPixels, 256).Complete();

        // Generate falloff mask
        NativeArray<float> falloff = new NativeArray<float>(totalPixels, Allocator.TempJob);
        BuildFalloff(falloff);

        // Apply falloff
        var falloffJob = new FalloffMultiplyJob
        {
            width = heightmapResolution,
            heightmap = rawHeight,
            falloff = falloff
        };
        falloffJob.Schedule(totalPixels, 256).Complete();
        falloff.Dispose();

        // (Optional) River carving can be inserted here, passing rawHeight.
        // CarveRivers(rawHeight);

        // Set heightmap to TerrainData (with DelayLOD for performance)
        float[,] heights2D = new float[heightmapResolution, heightmapResolution];
        for (int y = 0; y < heightmapResolution; y++)
        for (int x = 0; x < heightmapResolution; x++)
            heights2D[y, x] = rawHeight[y * heightmapResolution + x];

        terrainData.SetHeightsDelayLOD(0, 0, heights2D);

        // 2. Generate moisture map (independent)
        NativeArray<float> moistureMap = new NativeArray<float>(totalPixels, Allocator.TempJob);
        var moistureJob = new MoistureNoiseJob
        {
            width = heightmapResolution,
            scale = moistureNoiseScale,
            octaves = moistureOctaves,
            persistence = moisturePersistence,
            lacunarity = moistureLacunarity,
            offset = moistureOffset,
            moisture = moistureMap
        };
        moistureJob.Schedule(totalPixels, 256).Complete();

        // 3. Splatmapping (biome texturing)
        int splatRes = alphamapResolution;
        int splatPixels = splatRes * splatRes;
        NativeArray<float4> splatWeights = new NativeArray<float4>(splatPixels, Allocator.TempJob);

        var splatJob = new SplatmapJob
        {
            width = splatRes,
            heightmapRes = heightmapResolution,
            heightmap = rawHeight,
            moisture = moistureMap,
            snowHeight = snowHeight,
            lowElevationMax = lowElevationMax,
            dryMoistureMax = dryMoistureMax,
            splatWeights = splatWeights
        };
        splatJob.Schedule(splatPixels, 256).Complete();

        // Convert to float[,,] for SetAlphamaps
        int numLayers = terrainData.terrainLayers.Length; // Should be 4
        float[,,] alphamaps = new float[splatRes, splatRes, numLayers];
        for (int y = 0; y < splatRes; y++)
        for (int x = 0; x < splatRes; x++)
        {
            float4 w = splatWeights[y * splatRes + x];
            alphamaps[y, x, 0] = w.x; // Grass
            alphamaps[y, x, 1] = w.y; // Snow
            alphamaps[y, x, 2] = w.z; // Desert
            alphamaps[y, x, 3] = w.w; // Lush
            // If you have fewer layers, define less in the float4
        }

        terrainData.SetAlphamaps(0, 0, alphamaps);

        // Cleanup
        rawHeight.Dispose();
        moistureMap.Dispose();
        splatWeights.Dispose();

        // Finalise LOD builds (automatically triggered after this frame)
        terrain.Flush();
    }

    private void BuildFalloff(NativeArray<float> falloff)
    {
        float half = heightmapResolution * 0.5f;
        float inner = falloffInnerRadius * half;
        float outer = falloffOuterRadius * half;
        for (int y = 0; y < heightmapResolution; y++)
        for (int x = 0; x < heightmapResolution; x++)
        {
            float dx = x - half;
            float dy = y - half;
            float dist = math.sqrt(dx * dx + dy * dy);
            float t = math.saturate((dist - inner) / (outer - inner));
            falloff[y * heightmapResolution + x] = 1f - t; // 1 in center, 0 at edge
        }
    }

    // (Optional) Simple river carving using a few spline paths.
    // You can uncomment and feed some predefined Vectors to carve rivers.
    /*
    private void CarveRivers(NativeArray<float> heightmap)
    {
        // Example: carve a straight river from (0.2,0.8) to (0.8,0.2) with a given width
        RiverCarveJob carveJob = new RiverCarveJob {
            width = heightmapResolution,
            heightmap = heightmap,
            // Precomputed path pixels (omitted for brevity)
        };
        carveJob.Schedule().Complete();
    }
    */
}