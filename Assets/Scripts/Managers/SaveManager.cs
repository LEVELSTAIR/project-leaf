using UnityEngine;
using System.IO;
using System;
using UnityEngine.InputSystem;   // Required for Keyboard.current

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "save.json";
    [SerializeField] private KeyCode saveKey = KeyCode.F5;   // Not used, kept for reference

    private string savePath;
    private SaveData currentSaveData;

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
        // ---------- Use the new Input System ----------
        // Check if F5 was pressed this frame
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SaveGame();
        }
    }

    /// <summary>
    /// Saves the current player state to disk.
    /// </summary>
    public void SaveGame()
    {
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("SaveManager: No player found to save.");
            return;
        }

        // Gather data
        SaveData data = new SaveData();

        // Position & rotation
        data.SetPosition(player.transform.position);
        data.SetRotation(player.transform.rotation);

        // Health
        PlayerHealthManager health = player.GetComponent<PlayerHealthManager>();
        if (health != null)
        {
            data.currentHealth = health.CurrentHealth;
            data.maxHealth = health.MaxHealth;
        }

        // Oxygen
        PlayerOxygen oxygen = player.GetComponent<PlayerOxygen>();
        if (oxygen != null)
        {
            data.currentOxygen = oxygen.CurrentOxygen;
            data.maxOxygen = oxygen.MaxOxygen;
        }

        // Serialize to JSON and write to file
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        Debug.Log($"Game saved to {savePath}");
    }

    /// <summary>
    /// Loads save data from disk. Returns true if a valid save exists.
    /// </summary>
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

    /// <summary>
    /// Checks if a save file exists.
    /// </summary>
    public bool HasSave() => File.Exists(savePath);

    /// <summary>
    /// Deletes the save file.
    /// </summary>
    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Save file deleted.");
        }
    }
}