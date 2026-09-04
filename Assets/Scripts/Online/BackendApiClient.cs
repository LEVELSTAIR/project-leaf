#if STEAM_BUILD
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arborvale.Shared;
using Newtonsoft.Json;
using UnityEngine;

namespace Arborvale.Online
{
    /// <summary>
    /// Thin HTTP client for the closed-repo backend REST API.
    /// Auth is handled by SteamAuthService; this class only manages the JWT lifecycle.
    /// All methods are fire-and-forget-safe via async/await in coroutine adapters.
    /// </summary>
    public class BackendApiClient : IDisposable
    {
        // Configured at startup; no secrets in this file.
        private readonly string baseUrl;
        private readonly HttpClient http;
        private string jwt;
        private string refreshToken;
        private DateTime jwtExpiresAt;

        public BackendApiClient(string apiBaseUrl)
        {
            baseUrl = apiBaseUrl.TrimEnd('/');
            http = new HttpClient();
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(jwt) && DateTime.UtcNow < jwtExpiresAt;

        public void SetTokens(AuthResponseDto auth)
        {
            jwt = auth.Jwt;
            refreshToken = auth.RefreshToken;
            jwtExpiresAt = auth.ExpiresAt;
        }

        // --- Wallet ---
        public Task<WalletDto> GetWalletAsync(CancellationToken ct = default)
            => GetAsync<WalletDto>("/v1/wallet", ct);

        public Task<WalletDto> ExchangeCurrencyAsync(int premiumSpend, CancellationToken ct = default)
            => PostAsync<WalletDto>("/v1/wallet/exchange", $"{{\"premiumSpend\":{premiumSpend}}}", ct);

        // --- Gacha ---
        public Task<BannerListDto> GetBannersAsync(CancellationToken ct = default)
            => GetAsync<BannerListDto>("/v1/gacha/banners", ct);

        public Task<GachaPullResultDto> PullAsync(string bannerId, int count, CurrencyType currency, string idempotencyKey, CancellationToken ct = default)
        {
            string body = $"{{\"bannerId\":\"{bannerId}\",\"count\":{count},\"currency\":\"{currency}\",\"idempotencyKey\":\"{idempotencyKey}\"}}";
            return PostAsync<GachaPullResultDto>("/v1/gacha/pull", body, ct);
        }

        public Task<GrantListDto> GetGrantsAsync(CancellationToken ct = default)
            => GetAsync<GrantListDto>("/v1/grants", ct);

        // --- Grafting ---
        public Task<GraftConfigDto> GetGraftConfigAsync(CancellationToken ct = default)
            => GetAsync<GraftConfigDto>("/v1/config/graft", ct);

        public Task<GraftAttemptResultDto> AttemptGraftAsync(string trunkPartId, string foliagePartId, string bloomPartId, string idempotencyKey, CancellationToken ct = default)
        {
            string body = $"{{\"partTrunk\":\"{trunkPartId}\",\"partFoliage\":\"{foliagePartId}\",\"partBloom\":\"{bloomPartId}\",\"idempotencyKey\":\"{idempotencyKey}\"}}";
            return PostAsync<GraftAttemptResultDto>("/v1/graft/attempt", body, ct);
        }

        // --- Trade ---
        public Task<TradeSessionDto> CreateTradeSessionAsync(string partnerSteamId, CancellationToken ct = default)
        {
            string body = $"{{\"partnerSteamId\":\"{partnerSteamId}\"}}";
            return PostAsync<TradeSessionDto>("/v1/trade/session", body, ct);
        }

        public Task<TradeSessionDto> SetTradeOfferAsync(string sessionId, string[] grantIds, CancellationToken ct = default)
        {
            string ids = "[\"" + string.Join("\",\"", grantIds) + "\"]";
            return PutAsync<TradeSessionDto>($"/v1/trade/session/{sessionId}/offer", $"{{\"grantIds\":{ids}}}", ct);
        }

        public Task<TradeSessionDto> AcceptTradeAsync(string sessionId, CancellationToken ct = default)
            => PostAsync<TradeSessionDto>($"/v1/trade/session/{sessionId}/accept", "{}", ct);

        public Task<TradeSessionDto> GetTradeSessionAsync(string sessionId, CancellationToken ct = default)
            => GetAsync<TradeSessionDto>($"/v1/trade/session/{sessionId}", ct);

        // --- MTX ---
        public Task<MtxInitResponseDto> InitMtxAsync(string bundleId, string language = "en", CancellationToken ct = default)
        {
            string body = $"{{\"bundleId\":\"{bundleId}\",\"language\":\"{language}\"}}";
            return PostAsync<MtxInitResponseDto>("/v1/mtx/init", body, ct);
        }

        public Task<WalletDto> FinalizeMtxAsync(string orderId, CancellationToken ct = default)
        {
            string body = $"{{\"orderId\":\"{orderId}\"}}";
            return PostAsync<WalletDto>("/v1/mtx/finalize", body, ct);
        }

        // --- Telemetry ---
        public async Task PostTelemetryAsync(TelemetryBatchDto batch, CancellationToken ct = default)
        {
            await EnsureAuthAsync(ct);
            string body = JsonConvert.SerializeObject(batch);
            var req = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/v1/telemetry/events")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            ApplyJwt(req);
            using var resp = await http.SendAsync(req, ct);
            // 202 Accepted — no body expected
        }

        // --- Listings ---
        public Task<ListingListDto> GetListingsAsync(string itemId = null, CancellationToken ct = default)
        {
            string path = itemId != null ? $"/v1/listings?itemId={Uri.EscapeDataString(itemId)}" : "/v1/listings";
            return GetAsync<ListingListDto>(path, ct);
        }

        public Task<ListingDto> CreateListingAsync(string grantId, int priceBloomShards, CancellationToken ct = default)
        {
            string body = JsonConvert.SerializeObject(new { grantId, priceBloomShards });
            return PostAsync<ListingDto>("/v1/listings", body, ct);
        }

        public Task<WalletDto> BuyListingAsync(string listingId, string idempotencyKey, CancellationToken ct = default)
        {
            string body = JsonConvert.SerializeObject(new { idempotencyKey });
            return PostAsync<WalletDto>($"/v1/listings/{Uri.EscapeDataString(listingId)}/buy", body, ct);
        }

        public async Task CancelListingAsync(string listingId, CancellationToken ct = default)
        {
            await EnsureAuthAsync(ct);
            var req = new HttpRequestMessage(HttpMethod.Delete, baseUrl + $"/v1/listings/{Uri.EscapeDataString(listingId)}");
            ApplyJwt(req);
            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"Backend error {resp.StatusCode}");
        }

        // --- Internal HTTP helpers ---

        private async Task<T> GetAsync<T>(string path, CancellationToken ct) where T : class
        {
            await EnsureAuthAsync(ct);
            var req = new HttpRequestMessage(HttpMethod.Get, baseUrl + path);
            ApplyJwt(req);
            return await SendAsync<T>(req, ct);
        }

        private async Task<T> PostAsync<T>(string path, string jsonBody, CancellationToken ct) where T : class
        {
            await EnsureAuthAsync(ct);
            var req = new HttpRequestMessage(HttpMethod.Post, baseUrl + path)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            ApplyJwt(req);
            return await SendAsync<T>(req, ct);
        }

        private async Task<T> PutAsync<T>(string path, string jsonBody, CancellationToken ct) where T : class
        {
            await EnsureAuthAsync(ct);
            var req = new HttpRequestMessage(HttpMethod.Put, baseUrl + path)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            ApplyJwt(req);
            return await SendAsync<T>(req, ct);
        }

        private async Task<T> SendAsync<T>(HttpRequestMessage req, CancellationToken ct) where T : class
        {
            var resp = await http.SendAsync(req, ct);
            string json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"Backend error {resp.StatusCode}: {json}");
            return JsonConvert.DeserializeObject<T>(json);
        }

        private void ApplyJwt(HttpRequestMessage req)
        {
            if (!string.IsNullOrEmpty(jwt))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }

        private async Task EnsureAuthAsync(CancellationToken ct)
        {
            if (IsAuthenticated) return;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                try { await RefreshAsync(ct); return; }
                catch { Debug.LogWarning("[BackendApiClient] Token refresh failed."); }
            }
            throw new InvalidOperationException("Not authenticated. Call SteamAuthService.AuthenticateAsync first.");
        }

        private async Task RefreshAsync(CancellationToken ct)
        {
            string body = $"{{\"refreshToken\":\"{refreshToken}\"}}";
            var req = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/v1/auth/refresh")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            var auth = await SendAsync<AuthResponseDto>(req, ct);
            SetTokens(auth);
        }

        public void Dispose() => http?.Dispose();
    }
}
#endif
