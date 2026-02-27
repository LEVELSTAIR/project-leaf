# Unity Hierarchy & Prefab Architecture for Project Leaf

This document outlines the complete Unity hierarchy structure, prefabs, layers, and tags needed to implement all game systems described in the design document.

---

## User Review Required

> [!IMPORTANT]
> This is an architecture planning document, not code implementation. Please review and approve the structure before I proceed with creating scripts, prefabs, or scene setup.

> [!WARNING]
> Some systems (anti-plant AI, taming mechanics) are complex and may need iteration. The plan provides a solid foundation but may evolve during implementation.

---

## Table of Contents
1. [Layer & Tag Setup](#layer--tag-setup)
2. [Scene Hierarchy Structure](#scene-hierarchy-structure)
3. [Prefab Catalog](#prefab-catalog)
4. [System-by-System Breakdown](#system-by-system-breakdown)
5. [Data Scriptable Objects](#data-scriptable-objects)
6. [UI Toolkit Extensions](#ui-toolkit-extensions)

---

## Layer & Tag Setup

### Layers (Physics Collision)
| Layer # | Name | Purpose |
|---------|------|---------|
| 0 | Default | General objects |
| 6 | Player | Player character for collision detection |
| 7 | Ground | Terrain, walkable surfaces |
| 8 | Interactable | Trees, clay deposits, harvestable objects |
| 9 | Plant | All planted vegetation |
| 10 | AntiPlant | Hostile plants (for combat detection) |
| 11 | TamedPlant | Tamed anti-plants (neutral) |
| 12 | Pot | Planting pots |
| 13 | Decoration | Decorative objects (no gameplay collision) |
| 14 | BadLand | Toxic/oxygen-depleted areas |
| 15 | SafeZone | Oxygenated player territory |
| 16 | Cage | Anti-plant containment structures |

### Tags
| Tag | Usage |
|-----|-------|
| `Player` | Main player character |
| `MainCamera` | Primary camera |
| `Tree` | Sample-harvestable trees |
| `ClayDeposit` | Clay resource nodes |
| `Seed` | Dropped or found seeds |
| `Pot` | Planting containers |
| `Plant` | Active growing plants |
| `AntiPlant` | Hostile plants |
| `TamedPlant` | Tamed anti-plants |
| `WaterSource` | Water refill points |
| `OxygenBag` | Oxygen equipment |
| `Cage` | Anti-plant cages |
| `GardenTree` | Territory expansion trees |
| `NightSeed` | Night-only seeds |

---

## Scene Hierarchy Structure

### Login/Entry Scene: `Home.unity`
```
📁 --- MANAGERS ---
├── GameManager
├── AudioManager
├── SteamManager
└── CameraController (Entry camera with zoom-in cinematics)

📁 --- ENVIRONMENT ---
├── PlayerBase (visible from afar)
│   ├── BaseStructure
│   ├── InitialGarden
│   └── LoginCameraTarget (zoom destination)
└── DistantTerrain

📁 --- UI ---
├── LoginCanvas (UI Toolkit)
│   └── LoginScreen.uxml
└── TransitionOverlay
```

### Main Game Scene: `Game_Garden_Solo.unity`
```
📁 --- MANAGERS ---
├── GameManager
│   └── Components: GameStateManager, SaveManager
├── DayNightManager (✓ exists)
├── PlantManager
│   └── Components: PlantGrowthController, WateringSystem
├── SeedManager
├── AntiPlantManager
│   └── Components: HostilityController, TamingSystem
├── TerritoryManager
│   └── Components: OxygenZoneController, ExpansionHandler
├── NotificationManager
├── CraftingManager
└── AudioManager

📁 --- PLAYER ---
├── FirstPersonController (✓ exists as prefab)
│   ├── PlayerCamera
│   ├── InteractionController
│   ├── InventoryController
│   ├── OxygenSystem
│   ├── WateringCanHolder
│   └── CombatController

📁 --- ENVIRONMENT ---
├── Terrain
├── 📁 Trees
│   ├── SampleableTree_01
│   ├── SampleableTree_02
│   └── ... (pooled instances)
├── 📁 ClayDeposits
│   ├── ClayDeposit_01
│   └── ...
├── 📁 WaterSources
│   ├── Well
│   ├── River
│   └── Pond
├── 📁 Zones
│   ├── SafeZone_PlayerBase
│   ├── BadLand_North
│   ├── BadLand_East
│   └── ...
└── Skybox

📁 --- GARDEN SYSTEM ---
├── 📁 Pots
│   ├── Pot_01 (Pot prefab instance)
│   └── ...
├── 📁 ActivePlants
│   ├── Plant_Instance_001
│   └── ...
├── 📁 AntiPlants
│   ├── AntiPlant_001
│   └── ...
├── 📁 Cages
│   ├── Cage_001
│   └── ...
└── 📁 GardenTrees (Territory trees)
    ├── GardenTree_001
    └── ...

📁 --- DECORATIONS ---
├── PlayerDecorations (runtime-spawned)
└── EnvironmentDecorations

📁 --- UI ---
├── HUDDocument (✓ exists)
├── InventoryDocument
├── CraftingDocument
├── PlantInspectDocument
├── NotificationDocument
└── EscapeMenuDocument (✓ exists)
```

---

## Prefab Catalog

### Player Prefabs
| Prefab | Path | Components |
|--------|------|------------|
| `FirstPersonController` | ✓ Exists | Add: `InteractionController`, `InventoryController`, `OxygenSystem`, `CombatController` |
| `WateringCan` | `Prefabs/Tools/` | `WateringCanController`, equippable tool |
| `OxygenBag` | `Prefabs/Equipment/` | `OxygenBagController`, capacity, UI binding |

---

### Resource Prefabs
| Prefab | Path | Purpose |
|--------|------|---------|
| `SampleableTree` | `Prefabs/Resources/` | Tree with `TreeSampler` component, sample/seed drops |
| `ClayDeposit` | `Prefabs/Resources/` | Clay node with `HarvestableResource` component |
| `WaterSource` | `Prefabs/Resources/` | Refill watering can, `WaterRefillPoint` component |

---

### Gardening Prefabs
| Prefab | Path | Key Components |
|--------|------|----------------|
| `Pot_Clay` | `Prefabs/Garden/` | `PotController`, soil state, plant slot |
| `Pot_Ceramic` | `Prefabs/Garden/` | Higher-tier pot, faster growth |
| `Seed_Base` | `Prefabs/Seeds/` | `SeedData` reference, collectible |
| `Plant_Base` | `Prefabs/Plants/` | `PlantController`, growth stages, water needs |
| `GardenTree` | `Prefabs/Plants/` | `GardenTreeController`, oxygen radius, territory expansion |

---

### Anti-Plant Prefabs
| Prefab | Path | Key Components |
|--------|------|----------------|
| `AntiPlant_Base` | `Prefabs/AntiPlants/` | `AntiPlantController`, `AIBehavior`, `TamingProgress` |
| `AntiPlant_Passive` | `Prefabs/AntiPlants/` | Passive mob, doesn't attack other plants |
| `AntiPlant_Active` | `Prefabs/AntiPlants/` | Active mob, attacks player and plants |
| `Cage_Basic` | `Prefabs/Cages/` | `CageController`, containment, taming bonus |
| `Cage_Reinforced` | `Prefabs/Cages/` | Stronger cage, higher taming multiplier |

---

### Decoration Prefabs
| Prefab | Path | Purpose |
|--------|------|---------|
| `Decoration_Fence` | `Prefabs/Decorations/` | Placeable fence segment |
| `Decoration_Lamp` | `Prefabs/Decorations/` | Light source decoration |
| `Decoration_Statue` | `Prefabs/Decorations/` | Ornamental statue |
| `Decoration_Path` | `Prefabs/Decorations/` | Walkway tile |

---

### Zone Prefabs
| Prefab | Path | Purpose |
|--------|------|---------|
| `SafeZone_Marker` | `Prefabs/Zones/` | `OxygenZone` component, visual indicator |
| `BadLand_Zone` | `Prefabs/Zones/` | `ToxicZone` component, player damage trigger |
| `TerritoryExpansion_VFX` | `Prefabs/VFX/` | Visual effect for zone expansion |

---

## System-by-System Breakdown

### 1. Entry/Login System
```
📁 Scripts/Entry/
├── EntrySceneController.cs    // Manages login camera, Steam auth
├── CameraZoomController.cs    // Smooth zoom from far to base
└── LoginUIController.cs       // UI Toolkit binding for login
```

**Hierarchy Objects:**
- `CameraController` - Far view camera with animation
- `LoginCameraTarget` - Empty transform at player base (zoom destination)

---

### 2. Exploration & Resource System
```
📁 Scripts/Resources/
├── InteractionController.cs   // Player F-key interaction
├── TreeSampler.cs             // Tree sampling logic
├── SeedDropper.cs             // Seed drop probability
├── ClayHarvester.cs           // Clay gathering
└── IHarvestable.cs            // Interface for all harvestables
```

**Required Prefab Components:**
- `SampleableTree`: Collider (trigger), `TreeSampler`, visual mesh, outline on hover
- `ClayDeposit`: Collider, `HarvestableResource`, break stages

---

### 3. Pot & Planting System
```
📁 Scripts/Garden/
├── PotController.cs           // Pot state, plant assignment
├── PlantController.cs         // Growth stages, water needs
├── PlantGrowthSystem.cs       // Manager for all plant updates
├── WateringController.cs      // Watering can usage
└── PlantData.cs               // ScriptableObject for plant stats
```

**Pot Prefab Structure:**
```
Pot_Clay (Prefab)
├── PotMesh (visual)
├── SoilSlot (empty transform for plant placement)
├── InteractionCollider (trigger)
└── PlantSpawnPoint
```

**Plant Prefab Structure:**
```
Plant_Base (Prefab)
├── GrowthStage_0 (seed visual)
├── GrowthStage_1 (sprout)
├── GrowthStage_2 (mid-growth)
├── GrowthStage_3 (mature)
├── WaterNeedIndicator (particle/UI)
└── HealthBar (optional world-space UI)
```

---

### 4. Watering & Notification System
```
📁 Scripts/Watering/
├── WaterNeedSystem.cs         // Tracks all plants water status
├── WateringCanController.cs   // Tool logic
└── PlantWaterState.cs         // Per-plant water tracking
```

```
📁 Scripts/Notifications/
├── NotificationManager.cs     // Toast/HUD notifications
├── NotificationData.cs        // Notification types
└── WaterNotificationTrigger.cs
```

**UI Extension (UI Toolkit):**
```
📁 UI Toolkit/Notifications/
├── Notification.uxml          // Toast template
├── NotificationContainer.uxml // HUD area for notifications
└── Notification.uss           // Toast styling
```

---

### 5. Night Seed & Anti-Plant System
```
📁 Scripts/NightPlants/
├── NightSeedController.cs     // Night-only growth logic
├── NightCareSystem.cs         // Special night plant care
├── AntiPlantSpawner.cs        // 50% chance hostile spawn
└── NightSeedData.cs           // ScriptableObject
```

```
📁 Scripts/AntiPlants/
├── AntiPlantController.cs     // Base hostile plant logic
├── AntiPlantAI.cs             // Movement, attack patterns
├── AntiPlantType.cs           // Enum: Active, Passive
├── TamingSystem.cs            // Care progress, taming UI
├── TamingProgress.cs          // Per-plant taming state
└── PlantCageController.cs     // Cage containment logic
```

**Anti-Plant Prefab Structure:**
```
AntiPlant_Base (Prefab)
├── HostileVisual (evil appearance mesh)
├── TamedVisual (beautiful appearance - initially disabled)
├── AttackHitbox (trigger collider)
├── MovementController
├── HealthSystem
├── TamingMeter (world-space UI)
└── CageAnchorPoint
```

**Cage Prefab Structure:**
```
Cage_Basic (Prefab)
├── CageMesh
├── ContainmentZone (trigger - prevents escape)
├── InteractionPoint
└── AntiPlantSlot (where contained plant snaps to)
```

---

### 6. Taming Transformation System
```
📁 Scripts/Taming/
├── TamingManager.cs           // Global taming logic
├── PlantTransformer.cs        // Visual transformation handler
└── TamedPlantController.cs    // Post-tame behavior
```

**Taming Flow:**
1. Anti-plant starts with `HostileVisual` active
2. Player cares for plant (water, defend, cage)
3. `TamingProgress` increases
4. At max taming → `PlantTransformer` executes:
   - Fade out `HostileVisual`
   - Fade in `TamedVisual`
   - Change layer from `AntiPlant` to `TamedPlant`
   - Enable daylight growth mode

---

### 7. Combat & Defense System
```
📁 Scripts/Combat/
├── PlayerCombatController.cs  // Attack, defense
├── DefenseStructure.cs        // Buildable barriers
├── DamageSystem.cs            // Health, damage calculations
└── PlantHealthSystem.cs       // Plants taking damage
```

---

### 8. Seed Drop System
```
📁 Scripts/Seeds/
├── SeedDropSystem.cs          // 10% drop chance from mature plants
├── SeedCollector.cs           // Player pickup logic
└── SeedInventory.cs           // Seed storage
```

**Add to PlantController:**
```csharp
// In PlantController.cs
[SerializeField] private float seedDropChance = 0.1f; // 10%
[SerializeField] private GameObject seedPrefab;

public void OnHarvest()
{
    if (Random.value <= seedDropChance)
    {
        Instantiate(seedPrefab, transform.position + Vector3.up, Quaternion.identity);
    }
}
```

---

### 9. Decoration System
```
📁 Scripts/Decoration/
├── DecorationPlacer.cs        // Placement mode controller
├── DecorationObject.cs        // Base decoration behavior
├── DecorationSnapSystem.cs    // Grid/object snapping
└── DecorationSaveData.cs      // Persistence
```

---

### 10. Territory & Oxygen System
```
📁 Scripts/Territory/
├── TerritoryManager.cs        // Zone tracking
├── OxygenZone.cs              // Safe zone component
├── ToxicZone.cs               // BadLand component
├── OxygenBagController.cs     // Player oxygen equipment
├── GardenTreeController.cs    // Territory expansion trees
└── ZoneExpansionVFX.cs        // Visual feedback
```

**Zone Expansion Flow:**
1. Player plants `GardenTree` in BadLand
2. After 2 game days, `GardenTreeController` triggers:
   - Spawn `OxygenZone` with radius
   - Animate expansion VFX
   - Convert terrain texture (optional)
   - Update `TerritoryManager`

**Player Oxygen Flow:**
1. Player enters BadLand without `OxygenBag` → Take damage
2. With `OxygenBag` → Drain oxygen meter
3. Refill at base or water source

---

### 10b. Terrain Texture Painting System (Zone Expansion)

This system dynamically changes terrain textures from BadLand (dead soil) to SafeZone (green grass) when a GardenTree fully matures.

#### Terrain Setup in Unity Editor

**Step 1: Configure Terrain Layers**
In your Terrain, add these terrain layers in this exact order:

| Layer Index | Name | Texture | Purpose |
|-------------|------|---------|---------|
| 0 | `GreenGrass` | Grass texture | Safe zone / player territory |
| 1 | `BadLandSoil` | Dead/toxic soil | BadLand areas |
| 2 | `Transition` | Blend texture | Smooth edge transition (optional) |
| 3+ | Other layers | Paths, rocks, etc. | Decorative |

**Step 2: Initial Terrain Painting**
- Paint the player's starting base area with `GreenGrass` (layer 0)
- Paint all outer/unexplored areas with `BadLandSoil` (layer 1)

#### Scripts for Terrain Painting

```
📁 Scripts/Territory/
├── TerrainPainter.cs          // Runtime terrain texture modification
├── ZoneExpansionController.cs // Orchestrates expansion animation
└── TerrainPaintingConfig.cs   // ScriptableObject for settings
```

**TerrainPainter.cs - Core Implementation:**
```csharp
using UnityEngine;

public class TerrainPainter : MonoBehaviour
{
    public static TerrainPainter Instance { get; private set; }
    
    [Header("Terrain Reference")]
    public Terrain targetTerrain;
    
    [Header("Layer Indices (match your Terrain Layers order)")]
    public int safeZoneLayerIndex = 0;      // GreenGrass
    public int badLandLayerIndex = 1;       // BadLandSoil
    
    [Header("Painting Settings")]
    public float brushFalloff = 0.8f;       // Soft edge blending
    public float paintSpeed = 2f;           // Animation speed
    
    private TerrainData terrainData;
    private int alphamapWidth;
    private int alphamapHeight;
    private int numLayers;
    
    private void Awake()
    {
        Instance = this;
        if (targetTerrain == null)
            targetTerrain = Terrain.activeTerrain;
            
        terrainData = targetTerrain.terrainData;
        alphamapWidth = terrainData.alphamapWidth;
        alphamapHeight = terrainData.alphamapHeight;
        numLayers = terrainData.alphamapLayers;
    }
    
    /// <summary>
    /// Converts world position to alphamap coordinates
    /// </summary>
    public Vector2Int WorldToAlphamapCoord(Vector3 worldPos)
    {
        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        
        // Normalize position relative to terrain
        float normalizedX = (worldPos.x - terrainPos.x) / terrainSize.x;
        float normalizedZ = (worldPos.z - terrainPos.z) / terrainSize.z;
        
        // Convert to alphamap coordinates
        int mapX = Mathf.RoundToInt(normalizedX * alphamapWidth);
        int mapZ = Mathf.RoundToInt(normalizedZ * alphamapHeight);
        
        return new Vector2Int(
            Mathf.Clamp(mapX, 0, alphamapWidth - 1),
            Mathf.Clamp(mapZ, 0, alphamapHeight - 1)
        );
    }
    
    /// <summary>
    /// Paints a circular area from BadLand to SafeZone
    /// </summary>
    /// <param name="worldCenter">Center of the expansion in world space</param>
    /// <param name="radius">Radius in world units</param>
    /// <param name="instant">If true, paint immediately. If false, use coroutine animation</param>
    public void PaintSafeZone(Vector3 worldCenter, float radius, bool instant = false)
    {
        if (instant)
        {
            ApplyCircularPaint(worldCenter, radius, 1f);
        }
        else
        {
            StartCoroutine(AnimatedPaint(worldCenter, radius));
        }
    }
    
    private System.Collections.IEnumerator AnimatedPaint(Vector3 center, float targetRadius)
    {
        float currentRadius = 0f;
        
        while (currentRadius < targetRadius)
        {
            currentRadius += paintSpeed * Time.deltaTime;
            ApplyCircularPaint(center, currentRadius, 1f);
            yield return null;
        }
        
        // Final pass to ensure full coverage
        ApplyCircularPaint(center, targetRadius, 1f);
    }
    
    private void ApplyCircularPaint(Vector3 worldCenter, float radius, float strength)
    {
        Vector2Int centerCoord = WorldToAlphamapCoord(worldCenter);
        
        // Calculate radius in alphamap units
        float worldToAlphaScale = alphamapWidth / terrainData.size.x;
        int radiusInAlphamap = Mathf.CeilToInt(radius * worldToAlphaScale);
        
        // Define the area to modify
        int startX = Mathf.Max(0, centerCoord.x - radiusInAlphamap);
        int startZ = Mathf.Max(0, centerCoord.y - radiusInAlphamap);
        int endX = Mathf.Min(alphamapWidth, centerCoord.x + radiusInAlphamap);
        int endZ = Mathf.Min(alphamapHeight, centerCoord.y + radiusInAlphamap);
        
        int sizeX = endX - startX;
        int sizeZ = endZ - startZ;
        
        if (sizeX <= 0 || sizeZ <= 0) return;
        
        // Get current alphamap data for the region
        // Note: GetAlphamaps uses (z, x) ordering!
        float[,,] alphamaps = terrainData.GetAlphamaps(startX, startZ, sizeX, sizeZ);
        
        // Modify each pixel in the region
        for (int z = 0; z < sizeZ; z++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                int worldX = startX + x;
                int worldZ = startZ + z;
                
                // Calculate distance from center
                float dist = Vector2.Distance(
                    new Vector2(worldX, worldZ),
                    new Vector2(centerCoord.x, centerCoord.y)
                );
                
                if (dist <= radiusInAlphamap)
                {
                    // Calculate blend factor with falloff
                    float blend = 1f;
                    float falloffStart = radiusInAlphamap * brushFalloff;
                    
                    if (dist > falloffStart)
                    {
                        blend = 1f - ((dist - falloffStart) / (radiusInAlphamap - falloffStart));
                    }
                    
                    blend *= strength;
                    
                    // Blend from BadLand to SafeZone
                    float currentSafe = alphamaps[z, x, safeZoneLayerIndex];
                    float currentBad = alphamaps[z, x, badLandLayerIndex];
                    
                    // Increase safe zone, decrease bad land
                    alphamaps[z, x, safeZoneLayerIndex] = Mathf.Lerp(currentSafe, 1f, blend);
                    alphamaps[z, x, badLandLayerIndex] = Mathf.Lerp(currentBad, 0f, blend);
                    
                    // Normalize all layers to sum to 1
                    NormalizeAlphamapPixel(alphamaps, x, z);
                }
            }
        }
        
        // Apply the modified alphamap back to terrain
        terrainData.SetAlphamaps(startX, startZ, alphamaps);
    }
    
    private void NormalizeAlphamapPixel(float[,,] alphamaps, int x, int z)
    {
        float sum = 0f;
        for (int layer = 0; layer < numLayers; layer++)
        {
            sum += alphamaps[z, x, layer];
        }
        
        if (sum > 0.001f)
        {
            for (int layer = 0; layer < numLayers; layer++)
            {
                alphamaps[z, x, layer] /= sum;
            }
        }
    }
}
```

**Integration with GardenTreeController:**
```csharp
// In GardenTreeController.cs
public class GardenTreeController : MonoBehaviour
{
    [Header("Territory Expansion")]
    public float expansionRadius = 15f;  // World units
    public float expansionDelay = 2f;    // Game days (use DayNightManager)
    
    private bool hasExpanded = false;
    private float plantedTime;
    
    private void Start()
    {
        plantedTime = Time.time; // Or use in-game time
    }
    
    public void CheckExpansion(float currentGameDays)
    {
        if (hasExpanded) return;
        
        if (currentGameDays >= expansionDelay)
        {
            TriggerExpansion();
        }
    }
    
    private void TriggerExpansion()
    {
        hasExpanded = true;
        
        // Paint terrain
        TerrainPainter.Instance?.PaintSafeZone(
            transform.position, 
            expansionRadius, 
            instant: false  // Animate the expansion
        );
        
        // Spawn oxygen zone collider
        TerritoryManager.Instance?.RegisterNewSafeZone(transform.position, expansionRadius);
        
        // Play VFX
        // Instantiate expansion particles, etc.
    }
}
```

#### Visual Hierarchy for Zone System

```
📁 Zones (in scene hierarchy)
├── SafeZone_PlayerBase
│   ├── ZoneTrigger (SphereCollider, trigger, layer: SafeZone)
│   ├── ZoneVisual (optional: glowing boundary particles)
│   └── ZoneAudio (ambient safe zone sounds)
├── BadLand_North
│   ├── ZoneTrigger (large BoxCollider, trigger, layer: BadLand)
│   ├── ToxicFogVFX (particle system)
│   └── ToxicAmbientAudio
└── GardenTree_Instance_001
    ├── TreeMesh
    ├── GardenTreeController (script)
    └── ExpansionVFX_Prefab (spawned on expansion)
```

#### Performance Considerations

> [!TIP]
> **Optimization Tips:**
> - Cache `TerrainData` reference on Awake
> - Use `SetAlphamapsDelayLOD()` for large batch operations
> - Limit expansion animations to one at a time
> - Consider chunking very large radius changes

> [!NOTE]
> **Save/Load:** Store modified alphamap regions or track GardenTree positions and reapply on load.

---

## Data Scriptable Objects

Create these in `Assets/Data/`:

```
📁 Data/
├── 📁 Plants/
│   ├── PlantData_Rose.asset
│   ├── PlantData_Sunflower.asset
│   └── PlantData_NightBloom.asset
├── 📁 Seeds/
│   ├── SeedData_Rose.asset
│   ├── SeedData_NightShade.asset
│   └── ...
├── 📁 AntiPlants/
│   ├── AntiPlantData_ThornyHorror.asset
│   └── AntiPlantData_VenomVine.asset
├── 📁 Decorations/
│   └── DecorationCatalog.asset
└── 📁 Settings/
    ├── WateringConfig.asset
    ├── TamingConfig.asset
    └── TerritoryConfig.asset
```

**Example PlantData ScriptableObject:**
```csharp
[CreateAssetMenu(menuName = "Project Leaf/Plant Data")]
public class PlantData : ScriptableObject
{
    public string plantName;
    public Sprite icon;
    public GameObject[] growthStagePrefabs;
    public float[] growthStageDurations;
    public float waterNeededPerStage;
    public float droughtToleranceTime; // Grace period before death
    public bool isNightPlant;
    public float seedDropChance;
    public SeedData producedSeed;
}
```

---

## UI Toolkit Extensions

### New UI Documents Needed:

| Document | Purpose |
|----------|---------|
| `Notification.uxml` | Toast notifications for water alerts |
| `PlantInspect.uxml` | Plant status panel (Q key) |
| `TamingMeter.uxml` | World-space taming progress |
| `OxygenMeter.uxml` | HUD oxygen indicator |
| `DecorationMode.uxml` | Decoration placement UI |
| `CraftingPanel.uxml` | Crafting interface |

### HUD Extensions (add to existing):
- Oxygen bar (when in BadLand)
- Notification toast container
- Active quest/task indicator

---

## Folder Structure Summary

```
📁 Assets/
├── 📁 Prefabs/
│   ├── 📁 Player/
│   │   └── FirstPersonController.prefab (✓ exists, extend)
│   ├── 📁 Tools/
│   │   ├── WateringCan.prefab
│   │   └── OxygenBag.prefab
│   ├── 📁 Resources/
│   │   ├── SampleableTree.prefab
│   │   ├── ClayDeposit.prefab
│   │   └── WaterSource.prefab
│   ├── 📁 Garden/
│   │   ├── Pot_Clay.prefab
│   │   └── Pot_Ceramic.prefab
│   ├── 📁 Plants/
│   │   ├── Plant_Base.prefab
│   │   └── GardenTree.prefab
│   ├── 📁 Seeds/
│   │   └── Seed_Base.prefab
│   ├── 📁 AntiPlants/
│   │   ├── AntiPlant_Base.prefab
│   │   └── Cage_Basic.prefab
│   ├── 📁 Decorations/
│   │   └── (decoration prefabs)
│   └── 📁 Zones/
│       ├── SafeZone_Marker.prefab
│       └── BadLand_Zone.prefab
├── 📁 Scripts/
│   ├── 📁 Entry/
│   ├── 📁 Resources/
│   ├── 📁 Garden/
│   ├── 📁 Watering/
│   ├── 📁 Notifications/
│   ├── 📁 NightPlants/
│   ├── 📁 AntiPlants/
│   ├── 📁 Taming/
│   ├── 📁 Combat/
│   ├── 📁 Seeds/
│   ├── 📁 Decoration/
│   └── 📁 Territory/
├── 📁 Data/
│   ├── 📁 Plants/
│   ├── 📁 Seeds/
│   ├── 📁 AntiPlants/
│   └── 📁 Settings/
└── 📁 UI Toolkit/
    ├── 📁 Notifications/
    ├── 📁 PlantInspect/
    └── 📁 Decoration/
```

---

## Verification Plan

### Manual Verification
Since this is an architecture/planning document, verification will happen during implementation:

1. **Layer Setup Test**: Create test objects on each layer, verify collision matrix works
2. **Prefab Instantiation**: Spawn each prefab type, ensure all components are attached
3. **System Integration**: Test each system interaction (e.g., plant seed → grow → harvest → seed drop)

---

## Recommended Implementation Order

1. **Phase 1: Core Infrastructure**
   - Layer/Tag setup
   - Folder structure
   - Base ScriptableObjects

2. **Phase 2: Resource Gathering**
   - SampleableTree prefab
   - ClayDeposit prefab
   - InteractionController

3. **Phase 3: Gardening Core**
   - Pot system
   - PlantController
   - WateringSystem
   - Notification system

4. **Phase 4: Night & Anti-Plants**
   - NightSeed mechanics
   - AntiPlant AI
   - Cage system

5. **Phase 5: Taming & Transformation**
   - TamingSystem
   - Visual transformation

6. **Phase 6: Territory**
   - OxygenZone
   - BadLand
   - GardenTree expansion

7. **Phase 7: Polish**
   - Decorations
   - Entry scene cinematics
   - Full UI integration
