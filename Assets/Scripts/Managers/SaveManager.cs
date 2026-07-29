using UnityEngine;
using System.IO;
using System;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "save.json";

    private string savePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            SaveGame();
    }

    public void SaveGame()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("SaveManager: No player found.");
            return;
        }

        SaveData data = new SaveData();

        // ---- Player ----
        data.SetPosition(player.transform.position);
        data.SetRotation(player.transform.rotation);

        PlayerHealthManager health = player.GetComponent<PlayerHealthManager>();
        if (health != null)
        {
            data.currentHealth = health.CurrentHealth;
            data.maxHealth = health.MaxHealth;
        }

        PlayerOxygen oxygen = player.GetComponent<PlayerOxygen>();
        if (oxygen != null)
        {
            data.currentOxygen = oxygen.CurrentOxygen;
            data.maxOxygen = oxygen.MaxOxygen;
        }

        // ---- Trees (via unified controller) ----
        SaveTrees(data);

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Game saved to {savePath}");
    }

    public bool LoadGame(out SaveData data)
    {
        data = null;
        if (!File.Exists(savePath))
            return false;

        try
        {
            string json = File.ReadAllText(savePath);
            data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return false;

            Debug.Log($"Game loaded from {savePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load save: {e.Message}");
            return false;
        }
    }

    public bool HasSave() => File.Exists(savePath);
    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Save file deleted.");
        }
    }

    // ---------- Unified tree saving ----------
    private void SaveTrees(SaveData data)
    {
        data.treeStates.Clear();

        TreeIdentifier[] allTreeIDs = FindObjectsOfType<TreeIdentifier>();
        foreach (var idComp in allTreeIDs)
        {
            Debug.Log($"Tree ID: {idComp.UniqueID}, active: {idComp.gameObject.activeInHierarchy}");
            GameObject go = idComp.gameObject;
            TreeState state = new TreeState
            {
                id = idComp.UniqueID,
                posX = go.transform.position.x,
                posY = go.transform.position.y,
                posZ = go.transform.position.z,
                rotX = go.transform.rotation.x,
                rotY = go.transform.rotation.y,
                rotZ = go.transform.rotation.z,
                rotW = go.transform.rotation.w
            };

            // ---- Prefab key ----
            TreeUnifiedInteraction unified = go.GetComponent<TreeUnifiedInteraction>();
            if (unified != null)
            {
                // Use the unified controller to get all state
                var unifiedState = unified.GetUnifiedState();
                state.isCutDown = unifiedState.isCutDown;
                state.currentHits = unifiedState.currentHits;
                state.respawnTimeRemaining = unifiedState.respawnTimeRemaining;
                state.isRegrowing = unifiedState.isRegrowing;
                state.regrowTimeRemaining = unifiedState.regrowTimeRemaining;
                state.harvestCount = unifiedState.harvestCount;
                state.regrowCycleCount = unifiedState.regrowCycleCount;

                // Prefab key: use SeedData seedName if available, else fallback
                SeedTree seed = go.GetComponent<SeedTree>();
                if (seed != null && seed.SeedData != null)
                    state.prefabKey = seed.SeedData.seedName;
                else
                    state.prefabKey = "DefaultTree";
            }
            else
            {
                // Fallback if no unified controller (should not happen)
                Debug.LogWarning($"Tree {go.name} has no TreeUnifiedInteraction – using individual components.");
                TreeCuttable cuttable = go.GetComponent<TreeCuttable>();
                SeedTree seed = go.GetComponent<SeedTree>();
                if (cuttable != null)
                {
                    state.isCutDown = cuttable.IsCutDown;
                    state.currentHits = cuttable.CurrentHits;
                    state.respawnTimeRemaining = cuttable.RespawnTimeRemaining;
                }
                if (seed != null)
                {
                    state.isRegrowing = seed.IsRegrowing;
                    state.regrowTimeRemaining = seed.RegrowTimeRemaining;
                    state.harvestCount = seed.HarvestCount;
                    state.regrowCycleCount = seed.RegrowCycleCount;
                    if (seed.SeedData != null)
                        state.prefabKey = seed.SeedData.seedName;
                }
            }

            data.treeStates.Add(state);
        }
    }

    // ---------- Unified tree loading ----------
    public void ApplyTreeStates(SaveData data)
    {
        if (data == null || data.treeStates == null || data.treeStates.Count == 0)
            return;

        // 1. Apply to existing trees
        TreeIdentifier[] existingIDs = FindObjectsOfType<TreeIdentifier>();
        List<string> matchedIDs = new List<string>();

        // Debugs
        Debug.Log($"Matched {matchedIDs.Count} trees.  Instantiating for missing states.");
        foreach (var state in data.treeStates)
        {
            if (!matchedIDs.Contains(state.id))
            {
                Debug.Log($"Missing tree with ID {state.id}, instantiating new one.");
            }
        }

        foreach (var idComp in existingIDs)
        {
            TreeState state = data.treeStates.Find(s => s.id == idComp.UniqueID);
            if (state == null) continue;

            matchedIDs.Add(state.id);
            GameObject go = idComp.gameObject;
            go.transform.position = new Vector3(state.posX, state.posY, state.posZ);
            go.transform.rotation = new Quaternion(state.rotX, state.rotY, state.rotZ, state.rotW);

            // Use unified controller if present
            TreeUnifiedInteraction unified = go.GetComponent<TreeUnifiedInteraction>();
            if (unified != null)
            {
                var unifiedState = new TreeUnifiedInteraction.UnifiedTreeState
                {
                    isCutDown = state.isCutDown,
                    currentHits = state.currentHits,
                    respawnTimeRemaining = state.respawnTimeRemaining,
                    isRegrowing = state.isRegrowing,
                    regrowTimeRemaining = state.regrowTimeRemaining,
                    harvestCount = state.harvestCount,
                    regrowCycleCount = state.regrowCycleCount
                };
                unified.LoadUnifiedState(unifiedState);
            }
            else
            {
                // Fallback to individual component loading (should not be needed)
                Debug.LogWarning($"No TreeUnifiedInteraction on {go.name}, falling back to component loading.");
                TreeCuttable cuttable = go.GetComponent<TreeCuttable>();
                SeedTree seed = go.GetComponent<SeedTree>();
                // We removed LoadState, so we need to handle this or ensure unified exists.
                // For safety, we can still set via the new public setters if we add them.
                // But we assume unified is present.
            }
        }

        // 2. Instantiate missing trees
        if (TreePrefabRegistry.Instance == null)
        {
            Debug.LogWarning("TreePrefabRegistry.Instance not found.");
            return;
        }

        foreach (TreeState state in data.treeStates)
        {
            if (matchedIDs.Contains(state.id)) continue;

            GameObject prefab = TreePrefabRegistry.Instance.GetPrefab(state.prefabKey);
            if (prefab == null)
            {
                Debug.LogWarning($"No prefab for key '{state.prefabKey}' – skipping tree {state.id}");
                continue;
            }

            Vector3 pos = new Vector3(state.posX, state.posY, state.posZ);
            Quaternion rot = new Quaternion(state.rotX, state.rotY, state.rotZ, state.rotW);
            GameObject newTree = Instantiate(prefab, pos, rot);

            TreeIdentifier newID = newTree.GetComponent<TreeIdentifier>();
            if (newID != null)
                newID.SetID(state.id, true);
            else
                Debug.LogWarning($"Instantiated tree '{newTree.name}' has no TreeIdentifier.");

            // Apply state via unified controller
            TreeUnifiedInteraction unified = newTree.GetComponent<TreeUnifiedInteraction>();
            if (unified != null)
            {
                var unifiedState = new TreeUnifiedInteraction.UnifiedTreeState
                {
                    isCutDown = state.isCutDown,
                    currentHits = state.currentHits,
                    respawnTimeRemaining = state.respawnTimeRemaining,
                    isRegrowing = state.isRegrowing,
                    regrowTimeRemaining = state.regrowTimeRemaining,
                    harvestCount = state.harvestCount,
                    regrowCycleCount = state.regrowCycleCount
                };
                unified.LoadUnifiedState(unifiedState);
            }
            else
            {
                Debug.LogWarning($"Instantiated tree '{newTree.name}' has no TreeUnifiedInteraction, state not applied.");
            }
        }
    }
}