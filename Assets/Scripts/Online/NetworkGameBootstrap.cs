#if STEAM_BUILD
using System.Collections;
using Arborvale.Shared;
using Unity.Netcode;
using UnityEngine;

namespace Arborvale.Online
{
    /// <summary>
    /// Scene-level bootstrap placed in Eden_Shared.unity.
    /// Authenticates with the backend, creates the OnlineServicesBridge,
    /// and starts the NGO host/client via the LobbyService.
    ///
    /// Assign apiBaseUrl in the Inspector (never hardcode secrets here).
    /// </summary>
    public class NetworkGameBootstrap : MonoBehaviour
    {
        [Header("Backend")]
        [Tooltip("URL of the arborvale-backend API, e.g. https://api.arborvale.io")]
        public string apiBaseUrl = "https://api.arborvale.io";

        private BackendApiClient apiClient;

        private void Start()
        {
            StartCoroutine(BootstrapRoutine());
        }

        private IEnumerator BootstrapRoutine()
        {
            if (!SteamManager.Initialized)
            {
                Debug.LogError("[Bootstrap] Steam is not initialized. Cannot boot online session.");
                yield break;
            }

            // Create the API client and authenticate
            apiClient = new BackendApiClient(apiBaseUrl);
            var authTask = SteamAuthService.AuthenticateAsync(apiClient, apiBaseUrl);

            while (!authTask.IsCompleted)
                yield return null;

            if (authTask.IsFaulted)
            {
                Debug.LogError($"[Bootstrap] Steam auth failed: {authTask.Exception?.GetBaseException().Message}");
                yield break;
            }

            // Register the online services locator
            var bridge = new OnlineServicesBridge(apiClient);
            OnlineServices.Register(bridge);
            Debug.Log("[Bootstrap] Online services registered.");

            // Hand off to LobbyService to start or join a session
            if (LobbyService.Instance != null)
                LobbyService.Instance.OnBootstrapComplete();
        }

        private void OnDestroy()
        {
            OnlineServices.Unregister();
            apiClient?.Dispose();
        }
    }
}
#endif
