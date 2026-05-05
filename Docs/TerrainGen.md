# Terrain Generator – Settings Guide 🛠️

This guide explains every public parameter in `TerrainGeneratorWithRivers.cs` so you can shape your procedural world exactly as you want.  
All values can be changed in the **Inspector** – the terrain will regenerate when you press **Play**.

---

## 1. General Settings

| Parameter | Description | Typical range |
|-----------|-------------|---------------|
| **Seed** | Integer seed that makes the whole generation **deterministic**. Same seed → same world. Change it to get a completely new layout. | any int |
| **Heightmap Resolution** | Number of height samples on one side. Must be `2^n + 1` (e.g., `1025`, `2049`, `4097`). Higher = smoother slopes, but **much slower** generation and higher memory usage. | 2049 is a good quality/performance balance |
| **Alphamap Resolution** | Resolution of the texture‑splatting map (where colours blend). Powers of two (1024, 2048). Lower values make biome borders fuzzy; higher values give crisp transitions. | 1024 or 2048 |
| **Terrain Size** | World‑space width and length of the terrain (metres). e.g., `2048` means a 2×2 km map. | 1024–4096 |
| **Terrain Height** | Maximum world‑space height when the raw height value = 1.0. If a mountain peak is at 1.0, its actual height in the scene equals this value. | 300–800 |

---

## 2. Height Noise (Shape of the Land)

These parameters build the **base terrain** before any rivers or biomes.

| Parameter | What it does | What happens if you… |
|-----------|--------------|----------------------|
| **Noise Scale** | “Zoom” level of the noise. **Bigger** = larger, softer features (rolling hills). **Smaller** = tight, chaotic features (jagged peaks). | Increase → broad continents. Decrease → lots of small hills. |
| **Octaves** | How many layers of noise are added together. Each layer is smaller and adds finer detail. | More → rugged, realistic terrain (slower). Fewer → smooth, simple shapes. |
| **Persistence** | How much each octave contributes. `0.5` means each octave has half the amplitude of the previous one. | Higher (0.7–1.0) → later octaves are stronger → very jagged terrain. Lower (0.2–0.4) → early octaves dominate → smoother land. |
| **Lacunarity** | How quickly the frequency increases between octaves. `2.0` doubles the detail frequency each step. | Higher (3–4) → more erratic, high‑frequency detail. Lower (1.5) → detail changes more gradually. |

> **Visual recipe**  
> - **Rolling hills** → high scale (600), low octaves (4), low persistence (0.3).  
> - **Alpine mountains** → lower scale (200), high octaves (8), persistence around 0.6.

---

## 3. Moisture Noise (Wet / Dry Map)

This noise is **completely independent** from the height. It decides where the landscape is dry (desert) or wet (lush).

| Parameter | What it does |
|-----------|--------------|
| **Moisture Noise Scale** | Like `Noise Scale` but for moisture. Larger = big, slow climate zones. Smaller = a patchwork of tiny wet/dry areas. |
| **Moisture Octaves** | Number of detail layers for the moisture map. Usually fewer than height octaves (3–5) because climate varies smoothly. |
| **Moisture Persistence** | Controls the “intensity” of smaller moisture details. Lower values keep the map smoother. |
| **Moisture Lacunarity** | How fast the moisture frequency increases per octave. Default 2–2.5 works well. |

> **Tip**: If you want one large desert and one large jungle, use a high moisture scale (800–1000) and low octaves (3). For a mosaic of micro‑climates, use a low scale (200) and higher octaves.

---

## 4. Biome Thresholds (The Core Rules)

These three numbers directly implement your biome logic:

- **High Alt + Any Moisture → Snowcaps**  
- **Low Alt + Low Moisture → Desert**  
- **Low Alt + High Moisture → Rivers/Lush Valleys**

| Parameter (range 0–1) | Meaning | Changing it… |
|------------------------|---------|--------------|
| **Snow Height** | Elevation **above which** everything becomes snow (white texture). Moisture is **ignored** here. | Lower (0.4) → more snow, even on moderate hills. Higher (0.85) → snow only on the very highest peaks. |
| **Low Elevation Max** | Elevation **below which** the terrain is considered “lowland”. This is where desert or lush appears. | Raise (0.5) → more of the map becomes lowland, shrinking the grassy mid‑altitudes. Lower (0.15) → only very low coastal areas become desert/lush. |
| **Dry Moisture Max** | Inside the lowland, if the moisture value is **below this threshold** → Desert. If it is **above** → Lush Valley. | Raise (0.6) → more lowland turns into desert (even moderately moist areas become sand). Lower (0.1) → desert only in the very driest spots; most lowland becomes lush. |

> **Remember**: Between `LowElevationMax` and `SnowHeight` lies the **grass biome** (default mid‑elevation texture).

### Example configurations

- **Large deserts** → `LowElevationMax = 0.45`, `DryMoistureMax = 0.6`.  
- **Lush, green world** → `LowElevationMax = 0.3`, `DryMoistureMax = 0.15`.  
- **Frozen north** → `SnowHeight = 0.4`.

---

## 5. Falloff (Island Border)

Creates a circular mask that pushes the edges down to sea level, giving you a natural **island** or **closed valley**.

| Parameter | Description | Effect |
|-----------|-------------|--------|
| **Falloff Inner Radius** (0–1) | Normalised distance from centre where the falloff **begins**. Inside this radius the terrain is untouched. | Increase it → more central land remains at full height; edges become cliffs. Decrease it → the island becomes a sharp mountain in the middle. |
| **Falloff Outer Radius** (0–1) | Distance where the falloff **reaches zero** (sea level). Between inner and outer the height smoothly fades. | If very close to `Inner Radius`, you get an abrupt cliff at the coast. If close to 1.0, you get a wide, gentle beach. |

- **Typical island**: `Inner = 0.45`, `Outer = 0.95`.  
- **Bowl / enclosed valley**: `Inner = 0.1`, `Outer = 0.98` (edges rise all around).

---

## 6. Rivers (Spline‑Based Carving)

Rivers are **carved into the heightmap** using smooth Catmull‑Rom splines. They physically lower the terrain.

| Parameter | Description |
|-----------|-------------|
| **Carve Rivers** | Master toggle – enable/disable all river carving. |
| **Rivers** (list) | Each entry contains a list of **control points** in UV space (0‑to‑1 across the terrain). At least two points needed for a river. The spline will smoothly travel through them. |
| **River Depth** | How deep the riverbed is carved (normalised 0–1). `0.15` means the centre of the river will be 15% of the maximum terrain height lower than the original ground. |
| **River Width** | Half‑width of the river in UV units. `0.02` on a 2048‑size map ≈ 40 metres. The actual carved width is twice this value. |
| **River Spline Steps** | Number of interpolated points between each pair of control points. Higher = smoother curves (50 is usually enough). Too low → you might see corners in the river path. |

### Adding a river

- In the Inspector, under **Rivers**, click **+** to add a new `RiverDefinition`.  
- Open its **Control Points** list and add/remove points as needed.  
- Each point is a `Vector2` where:
  - `(0,0)` = bottom‑left  
  - `(1,1)` = top‑right  
- Example river from north‑west to south‑east:  
  `(0.2, 0.8)` → `(0.5, 0.5)` → `(0.8, 0.2)`

### Debugging rivers

- Turn on **Gizmos** in the Scene view – blue lines show the straight segments between control points (not the smooth spline, but enough to see where the river will go).  
- If you don’t see any carving, temporarily set:
  - `riverDepth = 0.4`  
  - `riverWidth = 0.08`  
  - Disable falloff (`Inner = 0`, `Outer = 1`)  
  - This will carve a huge trench you can’t miss. Then dial back the values to natural sizes.

---

## 7. Required Terrain Layers (Textures)

The script expects **at least 4** terrain layers (in this exact order) on the Terrain object:

1. **Grass** – medium elevations (index 0)  
2. **Snow** – high altitude (index 1)  
3. **Desert** – low + dry (index 2)  
4. **Lush** – low + wet (index 3)  

You can add more layers (e.g., rock, dirt), but they won’t be painted automatically by this script unless you extend the `SplatmapJob`.

---

## 8. Performance Considerations

- **Heightmap resolution** has the biggest impact. `2049` gives good detail and runs under 1 second on modern machines. `4097` looks gorgeous but takes 3–5 seconds.  
- **Alphamap resolution** affects splatmap generation time very little. Stick with `2048` or `1024`.  
- **Octaves** increase computation linearly. 6–8 octaves are still fast thanks to Burst.  
- **River spline steps** – stay under 100. Even 50 steps per segment is plenty smooth.  

All noise generation and carving happens inside **Burst‑compiled jobs**, so the CPU threads are used efficiently. The `SetHeightsDelayLOD` call ensures Unity’s internal LOD rebuild only happens once at the end.

---

## 9. Quick Recipes

| Desired effect | Settings to tweak |
|----------------|-------------------|
| Swiss Alps | `noiseScale = 250`, `octaves = 8`, `persistence = 0.6`, `snowHeight = 0.5` |
| Desert planet | `lowElevationMax = 0.8`, `dryMoistureMax = 0.7`, moisture scale low |
| Jungle islands | `lowElevationMax = 0.3`, `dryMoistureMax = 0.1`, moisture scale high |
| Massive river canyon | Add a river with 3–4 points, `depth = 0.3`, `width = 0.06` |
| Flat interior with cliff sides | `falloffInnerRadius = 0.7`, `falloffOuterRadius = 0.72` |

---

Now you can shape your procedural world like clay – happy worldbuilding!