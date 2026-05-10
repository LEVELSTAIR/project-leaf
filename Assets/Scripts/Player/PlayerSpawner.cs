using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private GameObject playerPrefab;      // the player prefab (if you use one)
    [SerializeField] private Transform spawnPoint;         // where to spawn

    void Start()
    {
        // If you already placed the player manually in the scene, just move it
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            Debug.Log("Player moved to spawn point.");
        }
        // If you prefer to instantiate the player (e.g. after death)
        else if (playerPrefab != null)
        {
            Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogWarning("No player found and no prefab assigned.");
        }
    }
}