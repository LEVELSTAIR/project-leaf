using System;

namespace Arborvale.Shared
{
    /// <summary>
    /// Cross-assembly signals raised by Arborvale.Online and consumed by game code
    /// in Assembly-CSharp (e.g. PlotRegistry), which asmdef assemblies cannot
    /// reference back directly.
    /// </summary>
    public static class NetworkSessionEvents
    {
        /// <summary>
        /// Raised on the server when a newly connected client's SteamID has been
        /// resolved from lobby member data. Args: (steamId, clientId).
        /// </summary>
        public static event Action<ulong, ulong> ClientSteamIdResolved;

        public static void RaiseClientSteamIdResolved(ulong steamId, ulong clientId)
            => ClientSteamIdResolved?.Invoke(steamId, clientId);
    }
}
