using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null && playerPrefab != null)
        {
            player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        if (player == null)
        {
            Debug.LogWarning("PlayerSpawner: No player found and no prefab assigned.");
            return;
        }

        // If we have a saved game, apply it
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
        {
            if (SaveManager.Instance.LoadGame(out SaveData data))
            {
                // Position & Rotation
                player.transform.position = data.GetPosition();
                player.transform.rotation = data.GetRotation();

                // Health
                var health = player.GetComponent<PlayerHealthManager>();
                if (health != null) health.LoadFromSave(data);

                // Oxygen
                var oxygen = player.GetComponent<PlayerOxygen>();
                if (oxygen != null) oxygen.LoadFromSave(data);

                Debug.Log("Player state restored from save.");
            }
        }
        else
        {
            // No save – just use the spawn point (already set)
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
        }
    }
}