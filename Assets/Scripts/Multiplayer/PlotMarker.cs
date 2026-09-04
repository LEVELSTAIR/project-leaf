#if STEAM_BUILD
using Steamworks;
using UnityEngine;

/// <summary>
/// Placed on plot anchor transforms in Eden_Shared.unity.
/// The player interacts to claim the plot; visual state mirrors PlotRegistry.
/// Assign plotId (unique per anchor) in the Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlotMarker : MonoBehaviour, IInteractable
{
    [Header("Plot")]
    public int plotId;

    [Header("Visuals")]
    public GameObject unclaimedVisual;
    public GameObject claimedVisual;

    public string InteractionPrompt
    {
        get
        {
            var reg = PlotRegistry.Instance;
            if (reg == null) return "Claim Plot";
            if (reg.IsPlotClaimed(plotId)) return "Plot claimed";
            return "Claim this plot";
        }
    }

    private bool subscribed;

    private void Start()
    {
        TrySubscribe();
        RefreshVisuals();
    }

    private void Update()
    {
        if (!subscribed) TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed || PlotRegistry.Instance == null) return;
        PlotRegistry.Instance.OnClaimsChanged += _ => RefreshVisuals();
        subscribed = true;
        RefreshVisuals();
    }

    public void Highlight(bool active) { }

    public void Interact()
    {
        var reg = PlotRegistry.Instance;
        if (reg == null) return;

        if (reg.IsPlotClaimed(plotId))
        {
            HUDManager.Instance?.ShowMessage("This plot is already claimed.");
            return;
        }

        ulong steamId = SteamUser.GetSteamID().m_SteamID;
        reg.ClaimPlotServerRpc(plotId, steamId);
    }

    private void RefreshVisuals()
    {
        bool claimed = PlotRegistry.Instance != null && PlotRegistry.Instance.IsPlotClaimed(plotId);
        if (unclaimedVisual != null) unclaimedVisual.SetActive(!claimed);
        if (claimedVisual != null) claimedVisual.SetActive(claimed);
    }
}
#endif
