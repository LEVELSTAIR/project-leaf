using UnityEngine;

public class AntiPlantManager : MonoBehaviour
{
    public static AntiPlantManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
