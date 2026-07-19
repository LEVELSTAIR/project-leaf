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
        {
            SaveGame();
        }
    }

    public void SaveGame()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("SaveManager: No player found to save.");
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

        // ---- Trees (unified) ----
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

    // ---------- Unified Tree Saving ----------

    private void SaveTrees(SaveData data)
    {
        data.treeStates.Clear();

        TreeIdentifier[] allTreeIDs = FindObjectsOfType<TreeIdentifier>();
        foreach (var idComp in allTreeIDs)
        {
            GameObject go = idComp.gameObject;
            TreeState state = new TreeState
            {
                id = idComp.UniqueID,

                // Position & Rotation
                posX = go.transform.position.x,
                posY = go.transform.position.y,
                posZ = go.transform.position.z,
                rotX = go.transform.rotation.x,
                rotY = go.transform.rotation.y,
                rotZ = go.transform.rotation.z,
                rotW = go.transform.rotation.w
            };

            // TreeCuttable data
            TreeCuttable cuttable = go.GetComponent<TreeCuttable>();
            if (cuttable != null)
            {
                state.isCutDown = cuttable.IsCutDown;
                state.currentHits = cuttable.CurrentHits;
                state.respawnTimeRemaining = cuttable.RespawnTimeRemaining;
            }
            else
            {
                state.isCutDown = false;
                state.currentHits = 0;
                state.respawnTimeRemaining = 0f;
            }

            // SeedTree data
            SeedTree seedTree = go.GetComponent<SeedTree>();
            if (seedTree != null)
            {
                state.isRegrowing = seedTree.IsRegrowing;
                state.regrowTimeRemaining = seedTree.RegrowTimeRemaining;
                state.harvestCount = seedTree.HarvestCount;
                state.regrowCycleCount = seedTree.RegrowCycleCount;
            }
            else
            {
                state.isRegrowing = false;
                state.regrowTimeRemaining = 0f;
                state.harvestCount = 0;
                state.regrowCycleCount = 0;
            }

            data.treeStates.Add(state);
        }
    }

    public void ApplyTreeStates(SaveData data)
    {
        if (data == null || data.treeStates == null || data.treeStates.Count == 0)
            return;

        // Apply to all existing trees in the scene
        TreeIdentifier[] allTreeIDs = FindObjectsOfType<TreeIdentifier>();
        foreach (var idComp in allTreeIDs)
        {
            TreeState state = data.treeStates.Find(s => s.id == idComp.UniqueID);
            if (state == null) continue;

            GameObject go = idComp.gameObject;

            // Restore position/rotation
            go.transform.position = new Vector3(state.posX, state.posY, state.posZ);
            go.transform.rotation = new Quaternion(state.rotX, state.rotY, state.rotZ, state.rotW);

            // Apply to components
            TreeCuttable cuttable = go.GetComponent<TreeCuttable>();
            if (cuttable != null) cuttable.LoadState(state);

            SeedTree seedTree = go.GetComponent<SeedTree>();
            if (seedTree != null) seedTree.LoadState(state);
        }
    }
}