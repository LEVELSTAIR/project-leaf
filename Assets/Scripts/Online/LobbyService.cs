#if STEAM_BUILD
using Arborvale.Shared;
using Arborvale.Transport;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace Arborvale.Online
{
    /// <summary>
    /// Manages Steam Lobby lifecycle and NGO host/client startup.
    /// Placed in Eden_Shared.unity alongside NetworkGameBootstrap and NetworkManager.
    ///
    /// Host: call HostLobby() to create a FriendsOnly lobby and start NGO host.
    /// Client: Steam fires GameLobbyJoinRequested_t (overlay invite / join friend),
    ///         which triggers JoinLobby automatically.
    ///
    /// Transport: SteamNetworkingSocketsTransport (Arborvale.Transport assembly).
    /// </summary>
    public class LobbyService : MonoBehaviour
    {
        public static LobbyService Instance { get; private set; }

        [Header("Lobby Settings")]
        public int maxPlayers = 8;
        [Tooltip("Lobby metadata key storing the host's SteamID (uint64 string).")]
        public string hostSteamIdKey = "HostSteamId";
        [Tooltip("Lobby metadata key used by each member to store their SteamID.")]
        public string memberSteamIdKey = "SteamId";

        private CSteamID currentLobby;

        // Steamworks callbacks
        private CallResult<LobbyCreated_t> onLobbyCreated;
        private Callback<LobbyEnter_t> onLobbyEntered;
        private Callback<GameLobbyJoinRequested_t> onJoinRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            onLobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            onLobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            onJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        }

        private void OnDisable()
        {
            onLobbyCreated?.Dispose();
            onLobbyEntered?.Dispose();
            onJoinRequested?.Dispose();
        }

        /// <summary>Called by NetworkGameBootstrap after Steam auth completes.</summary>
        public void OnBootstrapComplete()
        {
            Debug.Log("[LobbyService] Bootstrap complete. Awaiting lobby decision (call HostLobby or wait for invite).");
        }

        /// <summary>Creates a new FriendsOnly Steam lobby and starts NGO host.</summary>
        public void HostLobby()
        {
            var handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
            onLobbyCreated.Set(handle);
            Debug.Log("[LobbyService] Requesting lobby creation...");
        }

        /// <summary>Joins an existing Steam lobby. Fires OnLobbyEntered when complete.</summary>
        public void JoinLobby(CSteamID lobbyId)
        {
            SteamMatchmaking.JoinLobby(lobbyId);
            Debug.Log($"[LobbyService] Joining lobby {lobbyId}...");
        }

        public void LeaveLobby()
        {
            if (currentLobby.IsValid())
            {
                SteamMatchmaking.LeaveLobby(currentLobby);
                currentLobby = CSteamID.Nil;
            }
            NetworkManager.Singleton?.Shutdown();
        }

        // ─── Callbacks ───────────────────────────────────────────────────────

        private void OnLobbyCreated(LobbyCreated_t cb, bool ioFailure)
        {
            if (ioFailure || cb.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogError($"[LobbyService] Lobby creation failed: {cb.m_eResult}");
                return;
            }

            currentLobby = new CSteamID(cb.m_ulSteamIDLobby);
            var myId = SteamUser.GetSteamID();

            // Publish host SteamID in lobby metadata so joining clients can find it.
            SteamMatchmaking.SetLobbyData(currentLobby, hostSteamIdKey, myId.m_SteamID.ToString());
            // Also set own member data so the host can remap clientIds on reconnect.
            SteamMatchmaking.SetLobbyMemberData(currentLobby, memberSteamIdKey, myId.m_SteamID.ToString());

            Debug.Log($"[LobbyService] Lobby created: {currentLobby}. Starting NGO host...");
            NetworkManager.Singleton?.StartHost();

            // Hook client-connect to remap SteamIds → NGO clientIds (for WorldSaveService restore).
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnLobbyEntered(LobbyEnter_t cb)
        {
            currentLobby = new CSteamID(cb.m_ulSteamIDLobby);

            // Read host SteamID from lobby data and hand it to the transport.
            string hostIdStr = SteamMatchmaking.GetLobbyData(currentLobby, hostSteamIdKey);
            if (!ulong.TryParse(hostIdStr, out ulong hostIdRaw))
            {
                Debug.LogError($"[LobbyService] Could not parse host SteamID from lobby data: '{hostIdStr}'");
                return;
            }

            var hostId = new CSteamID(hostIdRaw);
            var mySteamId = SteamUser.GetSteamID();

            if (hostId == mySteamId)
            {
                // We're the host — lobby-entered fires for host too, but NGO is already started.
                return;
            }

            SteamNetworkingSocketsTransport.SetHostSteamId(hostId);

            // Publish own SteamID as member data for the host's remap logic.
            SteamMatchmaking.SetLobbyMemberData(currentLobby, memberSteamIdKey, mySteamId.m_SteamID.ToString());

            Debug.Log($"[LobbyService] Joined lobby {currentLobby}. Connecting to host {hostId}...");
            NetworkManager.Singleton?.StartClient();
        }

        private void OnJoinRequested(GameLobbyJoinRequested_t cb)
        {
            // Fired when the player accepts a Steam overlay invite.
            Debug.Log($"[LobbyService] Overlay join requested for lobby {cb.m_steamIDLobby}.");
            JoinLobby(cb.m_steamIDLobby);
        }

        // ─── Client-ID remap for WorldSaveService ────────────────────────────

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            // Find the newly connected client's SteamID from lobby member data,
            // then remap it in PlotRegistry so WorldSaveService can restore their plot.
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobby);
            for (int i = 0; i < memberCount; i++)
            {
                var memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobby, i);
                string steamIdStr = SteamMatchmaking.GetLobbyMemberData(currentLobby, memberId, memberSteamIdKey);
                if (!ulong.TryParse(steamIdStr, out ulong steamIdRaw)) continue;

                var steamId = new CSteamID(steamIdRaw);

                // The host's own clientId is always 0 (ServerClientId); skip remapping host.
                if (memberId == SteamUser.GetSteamID()) continue;

                // PlotRegistry lives in Assembly-CSharp, which this assembly cannot
                // reference — signal it through the shared event instead.
                NetworkSessionEvents.RaiseClientSteamIdResolved(steamIdRaw, clientId);
                Debug.Log($"[LobbyService] Resolved clientId {clientId} → SteamID {steamId}");
                break; // Only one new client connects at a time
            }
        }
    }
}
#endif
