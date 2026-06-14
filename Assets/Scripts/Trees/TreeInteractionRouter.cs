using UnityEngine;

[RequireComponent(typeof(SeedTree), typeof(TreeCuttable))]
public class TreeInteractionRouter : MonoBehaviour, IInteractable
{
    private SeedTree seedTree;
    private TreeCuttable treeCuttable;

    public string InteractionPrompt
    {
        get
        {
            if (WeaponHolderController.Instance == null)
                return "Error: Missing weapon controller";

            string currentTool = WeaponHolderController.Instance.CurrentTool;

            if (currentTool == "WateringCan")
            {
                return seedTree?.GetInteractionPrompt() ?? "Collect seeds?";
            }
            else if (currentTool == "Axe")
            {
                return treeCuttable?.GetInteractionPrompt() ?? "Cut tree?";
            }
            else
            {
                return "Equip watering can (1) or axe (2)";
            }
        }
    }

    private void Awake()
    {
        seedTree = GetComponent<SeedTree>();
        treeCuttable = GetComponent<TreeCuttable>();

        if (seedTree == null) Debug.LogError("TreeInteractionRouter: SeedTree component missing!", this);
        if (treeCuttable == null) Debug.LogError("TreeInteractionRouter: TreeCuttable component missing!", this);
    }

    public void Interact()
    {
        if (WeaponHolderController.Instance == null) return;

        string currentTool = WeaponHolderController.Instance.CurrentTool;

        if (currentTool == "WateringCan")
        {
            seedTree?.CollectSeeds();
        }
        else if (currentTool == "Axe")
        {
            treeCuttable?.CutTree();
        }
        else
        {
            HUDManager.Instance?.ShowMessage("You need a watering can or an axe!", 2f);
        }
    }

    public void Highlight(bool active)
    {
        // You can optionally highlight the tree here
        seedTree?.Highlight(active);
        // treeCuttable doesn't have a highlight, but you can add one if needed
    }
}