using UnityEngine;

namespace ProjectLeaf.Garden
{
    public class PlantController : MonoBehaviour
    {
    [System.Serializable]
    public struct GrowthStage
    {
        public GameObject visualPrefab;
        public float durationSeconds;
    }

    [Header("Growth Settings")]
    public string plantName = "Unknown Plant";
    public GrowthStage[] stages;
    public bool isNightPlant = false;
    
    [Header("Water Settings")]
    public float waterDepletionTime = 60f; // Time until thirsty
    public float droughtDeathTime = 30f;   // Time after thirsty to die

    private int currentStageIndex = 0;
    private float stageTimer = 0f;
    private float waterTimer = 0f;
    private bool isThirsty = false;
    private bool isDead = false;

    // References
    private GameObject currentVisual;

        private void OnEnable()
        {
            PlantManager.Instance?.RegisterPlant(this);
        }

        private void OnDisable()
        {
            PlantManager.Instance?.UnregisterPlant(this);
        }

        private void Start()
        {
            waterTimer = waterDepletionTime;
            UpdateVisuals();
        }

    private void Update()
    {
        if (isDead) return;

        HandleWaterAndGrowth(Time.deltaTime);
    }

    private void HandleWaterAndGrowth(float delta)
    {
        // Water Logic
        waterTimer -= delta;
        if (waterTimer <= 0)
        {
            if (!isThirsty)
            {
                isThirsty = true;
                // Show Thirsty Notification / UI Icon
                NotificationManager.Instance?.ShowNotification($"{plantName} needs water!");
            }
            
            // Drought Logic
            waterTimer -= delta; // Continue counting down into negative (drought timer)
            if (waterTimer <= -droughtDeathTime)
            {
                Die();
            }
        }

        // Growth Logic (Only if watered && correct time of day)
        // Note: Night/Day check requires DayNightManager reference
        bool canGrow = !isThirsty;

        if (canGrow)
        {
            stageTimer += delta;
            if (currentStageIndex < stages.Length - 1)
            {
                if (stageTimer >= stages[currentStageIndex].durationSeconds)
                {
                    AdvanceStage();
                }
            }
        }
    }

    public void WaterPlant()
    {
        waterTimer = waterDepletionTime;
        isThirsty = false;
        NotificationManager.Instance?.ShowNotification($"{plantName} watered.");
        // Update UI
    }

    private void AdvanceStage()
    {
        currentStageIndex++;
        stageTimer = 0f;
        UpdateVisuals();
        NotificationManager.Instance?.ShowNotification($"{plantName} grew!");
    }

    private void UpdateVisuals()
    {
        if (currentVisual != null) Destroy(currentVisual);

        if (currentStageIndex < stages.Length && stages[currentStageIndex].visualPrefab != null)
        {
            currentVisual = Instantiate(stages[currentStageIndex].visualPrefab, transform);
        }
    }

        private void Die()
        {
            isDead = true;
            NotificationManager.Instance?.ShowNotification($"{plantName} has died.");
            // Change visual to dead plant
        }

        public void UpdateGrowth(float gameHours)
        {
            if (isDead) return;

            // Convert game hours to simulation seconds if needed, 
            // but here we might just want to advance growth by a "tick"
            // Let's assume gameHours is the amount of in-game time passed.
            HandleWaterAndGrowth(gameHours * 3600f); // Assuming gameHours is real hours? 
            // Or maybe just use the Delta provided.
        }
    }
}
