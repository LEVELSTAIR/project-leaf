using UnityEngine;
using System.Collections.Generic;

public class SeedManager : MonoBehaviour
{
    public static SeedManager Instance { get; private set; }

    [Header("Seed Settings")]
    public List<SeedData> availableSeeds = new List<SeedData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public SeedData GetSeedData(string seedName)
    {
        return availableSeeds.Find(seed => seed.seedName == seedName);
    }

    public SeedData GetSeedDataByName(string seedName) => GetSeedData(seedName);

    public bool HasSeed(string seedName, int amount)
    {
        if (InventoryManager.Instance != null)
        {
            return InventoryManager.Instance.HasItem(seedName, ItemType.Seed, amount);
        }
        return false;
    }

    public void AddSeeds(string seedName, int amount)
    {
        if (InventoryManager.Instance != null)
        {
            Debug.Log($"[SeedManager] AddSeeds called: '{seedName}' x{amount}. InventoryMgr exists: {InventoryManager.Instance != null}");
            InventoryManager.Instance.AddItem(seedName, ItemType.Seed, amount);
        }
    }

    public void RemoveSeeds(string seedName, int amount)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(seedName, ItemType.Seed, amount);
        }
    }

    public int GetSeedCount(string seedName)
    {
        if (InventoryManager.Instance != null)
        {
            return InventoryManager.Instance.GetItemAmount(seedName, ItemType.Seed);
        }
        return 0;
    }

    // This method can be used to get a summary of all seeds in the inventory
    public Dictionary<string, int> GetAllSeedData()
    {
        if (InventoryManager.Instance != null)
        {
            return InventoryManager.Instance.GetAllSeeds();
        }

        return new Dictionary<string, int> { { "Nodata", 0 } };
    }

}
