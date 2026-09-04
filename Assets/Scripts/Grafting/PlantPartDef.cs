using UnityEngine;

public enum PartFamily { Berry, Pine, Blossom, Fungal }
public enum PartSlotType { Trunk, Foliage, Bloom, Accent }

/// <summary>
/// Defines a single modular mesh part in the hybrid plant library.
/// Part IDs must be unique across all slot types (e.g. "TR_PINE", "FO_BERRY2").
/// </summary>
[CreateAssetMenu(fileName = "NewPlantPart", menuName = "Grafting/Plant Part Definition")]
public class PlantPartDef : ScriptableObject
{
    [Tooltip("Stable ID used in HybridId encoding. Never rename after a part ships.")]
    public string partId;

    public PartFamily family;
    public PartSlotType slotType;

    [Tooltip("Prefab that will be instantiated and attached to the appropriate socket on the HybridPlantRoot.")]
    public GameObject prefab;

    [Tooltip("Display name shown in grafting UI.")]
    public string displayName;

    public Sprite icon;
}
