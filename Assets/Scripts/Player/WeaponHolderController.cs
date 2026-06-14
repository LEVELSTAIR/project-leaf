using UnityEngine;

public class WeaponHolderController : MonoBehaviour
{
    public static WeaponHolderController Instance { get; private set; }

    [Header("Tool References")]
    public GameObject wateringCan;
    public GameObject axe;
    public GameObject torch;

    [Header("Hotbar Mapping")]
    [Tooltip("Hotbar slot index (1-based) that equips the watering can.")]
    public int wateringCanSlot = 1;
    [Tooltip("Hotbar slot index (1-based) that equips the axe.")]
    public int axeSlot = 2;
    [Tooltip("Hotbar slot index (1-based) that equips the torch.")]
    public int torchSlot = 3;

    [Header("Current State")]
    [SerializeField] private string currentTool = "WateringCan"; // "WateringCan" or "Axe" or "Torch"

    public string CurrentTool => currentTool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe to hotbar selection events
        if (KeyboardInputManager.Instance != null)
        {
            KeyboardInputManager.Instance.OnHotbarSlotSelected += OnHotbarSlotSelected;
        }

        // Initial equipment based on current hotbar slot
        if (KeyboardInputManager.Instance != null)
        {
            OnHotbarSlotSelected(KeyboardInputManager.Instance.CurrentHotbarSlot);
        }
        else
        {
            // Fallback: equip watering can by default
            EquipTool("WateringCan");
        }
    }

    private void OnHotbarSlotSelected(int slot)
    {
        if (slot == wateringCanSlot)
            EquipTool("WateringCan");
        else if (slot == axeSlot)
            EquipTool("Axe");
        else if (slot == torchSlot)
            EquipTool("Torch");
        // Other slots could keep the last tool, or do nothing
    }

    private void EquipTool(string toolName)
    {
        currentTool = toolName;

        // Activate the correct child, deactivate the other
        if (wateringCan != null) wateringCan.SetActive(toolName == "WateringCan");
        if (axe != null) axe.SetActive(toolName == "Axe");
        if (torch != null) torch.SetActive(toolName == "Torch");

        Debug.Log($"[WeaponHolder] Equipped: {toolName}");
    }

    private void OnDestroy()
    {
        if (KeyboardInputManager.Instance != null)
            KeyboardInputManager.Instance.OnHotbarSlotSelected -= OnHotbarSlotSelected;
    }
}