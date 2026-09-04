#if STEAM_BUILD
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace Arborvale.Transport
{
    /// <summary>
    /// NGO 2.9.1 transport using SteamNetworkingSockets P2P.
    /// Attach to the NetworkManager GameObject and set as its Transport.
    ///
    /// Host flow:  CreateListenSocketP2P → accept incoming connections via
    ///             SteamNetConnectionStatusChangedCallback_t.
    /// Client flow: ConnectP2P to the host's SteamID → wait for Connected callback.
    ///
    /// All sends are reliable by default. Set reliableChannelId for unreliable
    /// override (NGO uses channel 0 = reliable, 1 = unreliable by convention).
    /// </summary>
    public class SteamNetworkingSocketsTransport : NetworkTransport
    {
        [Header("Steam Networking")]
        [Tooltip("Virtual port for P2P connections. Host and clients must use the same value.")]
        public int virtualPort = 0;

        [Tooltip("Max messages to poll per frame.")]
        public int maxMessagesPerPoll = 64;

        private HSteamListenSocket listenSocket;
        private HSteamNetConnection serverConnection; // client-only: connection to host

        // host-only: maps NGO clientId → HSteamNetConnection
        private readonly Dictionary<ulong, HSteamNetConnection> clientIdToConn = new();
        // host-only: maps HSteamNetConnection → NGO clientId
        private readonly Dictionary<HSteamNetConnection, ulong> connToClientId = new();
        private ulong nextClientId = 1; // 0 is reserved for server

        private Callback<SteamNetConnectionStatusChangedCallback_t> connStatusChanged;

        private bool isServer;
        private bool isClient;

        private readonly IntPtr[] msgBuffer = new IntPtr[64];

        // ─── NetworkTransport overrides ───────────────────────────────────────

        public override ulong ServerClientId => 0;

        public override void Initialize(NetworkManager networkManager = null)
        {
            connStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
        }

        public override bool StartServer()
        {
            isServer = true;
            listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(virtualPort, 0, null);
            if (listenSocket == HSteamListenSocket.Invalid)
            {
                Debug.LogError("[SteamTransport] Failed to create listen socket.");
                return false;
            }
            Debug.Log($"[SteamTransport] Listening on virtual port {virtualPort}.");
            return true;
        }

        public override bool StartClient()
        {
            isClient = true;
            // Host SteamID is set by LobbyService before StartClient is called.
            if (!HostSteamId.IsValid())
            {
                Debug.LogError("[SteamTransport] HostSteamId not set. Call SetHostSteamId() before starting client.");
                return false;
            }
            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID(HostSteamId);
            serverConnection = SteamNetworkingSockets.ConnectP2P(ref identity, virtualPort, 0, null);
            if (serverConnection == HSteamNetConnection.Invalid)
            {
                Debug.LogError("[SteamTransport] ConnectP2P failed.");
                return false;
            }
            Debug.Log($"[SteamTransport] Connecting to host {HostSteamId}...");
            return true;
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (clientIdToConn.TryGetValue(clientId, out var conn))
            {
                SteamNetworkingSockets.CloseConnection(conn, 0, "Server disconnect", false);
                connToClientId.Remove(conn);
                clientIdToConn.Remove(clientId);
            }
        }

        public override void DisconnectLocalClient()
        {
            if (serverConnection != HSteamNetConnection.Invalid)
            {
                SteamNetworkingSockets.CloseConnection(serverConnection, 0, "Client disconnect", false);
                serverConnection = HSteamNetConnection.Invalid;
            }
        }

        public override void Shutdown()
        {
            foreach (var conn in clientIdToConn.Values)
                SteamNetworkingSockets.CloseConnection(conn, 0, "Shutdown", false);
            clientIdToConn.Clear();
            connToClientId.Clear();

            if (listenSocket != HSteamListenSocket.Invalid)
            {
                SteamNetworkingSockets.CloseListenSocket(listenSocket);
                listenSocket = HSteamListenSocket.Invalid;
            }

            if (serverConnection != HSteamNetConnection.Invalid)
            {
                SteamNetworkingSockets.CloseConnection(serverConnection, 0, "Shutdown", false);
                serverConnection = HSteamNetConnection.Invalid;
            }

            connStatusChanged?.Dispose();
            connStatusChanged = null;
            isServer = isClient = false;
        }

        public override unsafe void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery)
        {
            HSteamNetConnection conn;
            if (isServer)
            {
                if (!clientIdToConn.TryGetValue(clientId, out conn))
                    return;
            }
            else
            {
                conn = serverConnection;
            }

            int flags = delivery == NetworkDelivery.Unreliable || delivery == NetworkDelivery.UnreliableSequenced
                ? Constants.k_nSteamNetworkingSend_Unreliable
                : Constants.k_nSteamNetworkingSend_Reliable;

            fixed (byte* ptr = payload.Array)
            {
                SteamNetworkingSockets.SendMessageToConnection(
                    conn,
                    (IntPtr)(ptr + payload.Offset),
                    (uint)payload.Count,
                    flags,
                    out _);
            }
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            payload = default;
            receiveTime = Time.realtimeSinceStartup;

            HSteamNetConnection pollConn = isServer
                ? (clientIdToConn.Count > 0 ? default : HSteamNetConnection.Invalid)
                : serverConnection;

            // Server: poll each client connection
            if (isServer)
            {
                foreach (var (cId, conn) in clientIdToConn)
                {
                    int count = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, msgBuffer, maxMessagesPerPoll);
                    for (int i = 0; i < count; i++)
                    {
                        var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(msgBuffer[i]);
                        byte[] data = new byte[msg.m_cbSize];
                        Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);
                        SteamNetworkingMessage_t.Release(msgBuffer[i]);

                        clientId = cId;
                        payload = new ArraySegment<byte>(data);
                        return NetworkEvent.Data;
                    }
                }
                return NetworkEvent.Nothing;
            }

            // Client: poll server connection
            if (serverConnection != HSteamNetConnection.Invalid)
            {
                int count = SteamNetworkingSockets.ReceiveMessagesOnConnection(serverConnection, msgBuffer, maxMessagesPerPoll);
                if (count > 0)
                {
                    var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(msgBuffer[0]);
                    byte[] data = new byte[msg.m_cbSize];
                    Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);
                    SteamNetworkingMessage_t.Release(msgBuffer[0]);

                    clientId = ServerClientId;
                    payload = new ArraySegment<byte>(data);
                    return NetworkEvent.Data;
                }
            }

            return NetworkEvent.Nothing;
        }

        public override ulong GetCurrentRtt(ulong clientId) => 0;

        // ─── Steam callbacks ──────────────────────────────────────────────────

        private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t cb)
        {
            var state = cb.m_info.m_eState;

            if (isServer)
            {
                switch (state)
                {
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                        // Accept the incoming connection
                        var result = SteamNetworkingSockets.AcceptConnection(cb.m_hConn);
                        if (result != EResult.k_EResultOK)
                        {
                            SteamNetworkingSockets.CloseConnection(cb.m_hConn, 0, "Accept failed", false);
                            return;
                        }
                        break;

                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                        ulong newId = nextClientId++;
                        clientIdToConn[newId] = cb.m_hConn;
                        connToClientId[cb.m_hConn] = newId;
                        InvokeOnTransportEvent(NetworkEvent.Connect, newId, default, Time.realtimeSinceStartup);
                        Debug.Log($"[SteamTransport] Client connected: clientId={newId}");
                        break;

                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                        if (connToClientId.TryGetValue(cb.m_hConn, out ulong disconnectedId))
                        {
                            clientIdToConn.Remove(disconnectedId);
                            connToClientId.Remove(cb.m_hConn);
                            SteamNetworkingSockets.CloseConnection(cb.m_hConn, 0, "Disconnected", false);
                            InvokeOnTransportEvent(NetworkEvent.Disconnect, disconnectedId, default, Time.realtimeSinceStartup);
                            Debug.Log($"[SteamTransport] Client disconnected: clientId={disconnectedId}");
                        }
                        break;
                }
            }
            else if (isClient)
            {
                switch (state)
                {
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                        InvokeOnTransportEvent(NetworkEvent.Connect, ServerClientId, default, Time.realtimeSinceStartup);
                        Debug.Log("[SteamTransport] Connected to host.");
                        break;

                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                        serverConnection = HSteamNetConnection.Invalid;
                        InvokeOnTransportEvent(NetworkEvent.Disconnect, ServerClientId, default, Time.realtimeSinceStartup);
                        Debug.Log("[SteamTransport] Disconnected from host.");
                        break;
                }
            }
        }

        // ─── Host SteamID (set by LobbyService before StartClient) ───────────

        public static CSteamID HostSteamId { get; private set; } = CSteamID.Nil;

        public static void SetHostSteamId(CSteamID steamId)
        {
            HostSteamId = steamId;
            Debug.Log($"[SteamTransport] Host SteamID set to {steamId}");
        }
    }
}
#endif
