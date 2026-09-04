#if STEAM_BUILD
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arborvale.Shared;
using Steamworks;
using UnityEngine;

namespace Arborvale.Online
{
    /// <summary>
    /// Exchanges a Steam Web API session ticket for a backend JWT.
    /// Call AuthenticateAsync once at lobby join; tokens are stored in BackendApiClient.
    /// </summary>
    public static class SteamAuthService
    {
        public static async Task AuthenticateAsync(BackendApiClient client, string apiBaseUrl, CancellationToken ct = default)
        {
            if (!SteamManager.Initialized)
                throw new InvalidOperationException("Steam not initialized.");

            // Request a session ticket bound to our backend identity.
            // The ticket buffer is filled synchronously; the GetAuthSessionTicketResponse_t
            // callback only confirms validation, which the backend does server-side anyway.
            byte[] ticketBytes = new byte[1024];
            var identity = new SteamNetworkingIdentity();
            HAuthTicket handle = SteamUser.GetAuthSessionTicket(
                ticketBytes, ticketBytes.Length, out uint ticketLength, ref identity);
            if (handle == HAuthTicket.Invalid || ticketLength == 0)
                throw new InvalidOperationException("Failed to obtain Steam auth session ticket.");

            string hexTicket = BitConverter.ToString(ticketBytes, 0, (int)ticketLength).Replace("-", "");

            using var http = new HttpClient();
            string body = $"{{\"ticket\":\"{hexTicket}\"}}";
            var req = new HttpRequestMessage(HttpMethod.Post, apiBaseUrl.TrimEnd('/') + "/v1/auth/steam")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            var resp = await http.SendAsync(req, ct);
            string json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"Auth failed {resp.StatusCode}: {json}");

            // DTOs are records — JsonUtility would deserialize them to empty values silently
            // (see DtoJsonRoundTripTests). Newtonsoft is required here.
            var auth = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthResponseDto>(json);
            client.SetTokens(auth);
            Debug.Log($"[SteamAuthService] Authenticated as {auth.SteamId}");
        }
    }
}
#endif
