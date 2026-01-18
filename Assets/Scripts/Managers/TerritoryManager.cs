using UnityEngine;

public class TerritoryManager : MonoBehaviour
{
    public static TerritoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
