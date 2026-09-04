#if STEAM_BUILD
using System;
using System.Collections;
using Arborvale.Shared;
using Steamworks;
using UnityEngine;

/// <summary>
/// Handles Steam MicroTxn flow:
///   1. Client calls Purchase(bundleId) → POST /v1/mtx/init → get orderId
///   2. Steam overlay launches automatically (no client action needed)
///   3. MicroTxnAuthorizationResponse_t callback fires
///   4. Client calls POST /v1/mtx/finalize → updated wallet returned
///
/// Partner-site bundle config and pricing are managed in Steamworks Partner portal.
/// Never hardcode prices or bundle values here.
/// </summary>
public class MtxService : MonoBehaviour
{
    public static MtxService Instance { get; private set; }

    private Callback<MicroTxnAuthorizationResponse_t> mtxCallback;
    private string pendingOrderId;
    private bool awaitingCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        mtxCallback = Callback<MicroTxnAuthorizationResponse_t>.Create(OnMtxCallback);
    }

    private void OnDisable()
    {
        mtxCallback?.Dispose();
    }

    /// <summary>Initiates a premium currency purchase for the given bundle ID.</summary>
    public void Purchase(string bundleId, string language = "en")
    {
        if (!OnlineServices.IsAvailable)
        {
            HUDManager.Instance?.ShowMessage("Purchase requires an online connection.");
            return;
        }

        if (awaitingCallback)
        {
            HUDManager.Instance?.ShowMessage("A purchase is already in progress.");
            return;
        }

        StartCoroutine(InitRoutine(bundleId, language));
    }

    private IEnumerator InitRoutine(string bundleId, string language)
    {
        var task = OnlineServices.Instance.InitMtxAsync(bundleId, language);
        while (!task.IsCompleted) yield return null;

        if (task.IsFaulted)
        {
            Debug.LogError($"[MtxService] Init failed: {task.Exception?.GetBaseException().Message}");
            HUDManager.Instance?.ShowMessage("Purchase could not be started.");
            yield break;
        }

        pendingOrderId = task.Result?.OrderId;
        if (string.IsNullOrEmpty(pendingOrderId))
        {
            HUDManager.Instance?.ShowMessage("Purchase could not be started.");
            yield break;
        }

        awaitingCallback = true;
        Debug.Log($"[MtxService] Order {pendingOrderId} initiated — awaiting Steam overlay.");
    }

    private void OnMtxCallback(MicroTxnAuthorizationResponse_t result)
    {
        if (!awaitingCallback) return;
        awaitingCallback = false;

        bool authorized = result.m_bAuthorized == 1;
        string orderId = pendingOrderId;
        pendingOrderId = null;

        StartCoroutine(FinalizeRoutine(orderId, authorized));
    }

    private IEnumerator FinalizeRoutine(string orderId, bool authorized)
    {
        if (!authorized)
        {
            Debug.Log("[MtxService] Purchase cancelled by player.");
            yield break;
        }

        var task = OnlineServices.Instance.FinalizeMtxAsync(orderId);
        while (!task.IsCompleted) yield return null;

        if (task.IsFaulted)
        {
            Debug.LogError($"[MtxService] Finalize failed: {task.Exception?.GetBaseException().Message}");
            HUDManager.Instance?.ShowMessage("Purchase could not complete. Please contact support.");
            yield break;
        }

        WalletWidget.Instance?.Refresh(task.Result);
        HUDManager.Instance?.ShowMessage("Purchase complete! Your balance has been updated.");
        Debug.Log("[MtxService] Purchase finalized successfully.");
    }
}
#endif
