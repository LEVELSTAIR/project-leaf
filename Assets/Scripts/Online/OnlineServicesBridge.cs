#if STEAM_BUILD
using System;
using System.Threading.Tasks;
using Arborvale.Shared;
using UnityEngine;

namespace Arborvale.Online
{
    /// <summary>
    /// Implements IOnlineServices using BackendApiClient.
    /// Created at runtime by NetworkGameBootstrap after Steam auth completes.
    /// Registers itself into OnlineServices.Instance so gameplay code can
    /// call backend features without a reference to this assembly.
    /// </summary>
    public class OnlineServicesBridge : IOnlineServices
    {
        private readonly BackendApiClient client;

        public bool IsAvailable => client != null && client.IsAuthenticated;

        public OnlineServicesBridge(BackendApiClient apiClient)
        {
            client = apiClient;
        }

        public Task<WalletDto> GetWalletAsync() => client.GetWalletAsync();
        public Task<BannerListDto> GetBannersAsync() => client.GetBannersAsync();

        public Task<GachaPullResultDto> PullAsync(string bannerId, int count, CurrencyType currency, string idempotencyKey)
            => client.PullAsync(bannerId, count, currency, idempotencyKey);

        public Task<GrantListDto> GetGrantsAsync() => client.GetGrantsAsync();
        public Task<GraftConfigDto> GetGraftConfigAsync() => client.GetGraftConfigAsync();

        public Task<GraftAttemptResultDto> AttemptGraftAsync(string trunkPartId, string foliagePartId, string bloomPartId, string idempotencyKey)
            => client.AttemptGraftAsync(trunkPartId, foliagePartId, bloomPartId, idempotencyKey);

        public Task<TradeSessionDto> CreateTradeSessionAsync(string partnerSteamId)
            => client.CreateTradeSessionAsync(partnerSteamId);

        public Task<TradeSessionDto> SetTradeOfferAsync(string sessionId, string[] grantIds)
            => client.SetTradeOfferAsync(sessionId, grantIds);

        public Task<TradeSessionDto> AcceptTradeAsync(string sessionId)
            => client.AcceptTradeAsync(sessionId);

        public Task<TradeSessionDto> GetTradeSessionAsync(string sessionId)
            => client.GetTradeSessionAsync(sessionId);

        public Task<MtxInitResponseDto> InitMtxAsync(string bundleId, string language)
            => client.InitMtxAsync(bundleId, language);

        public Task<WalletDto> FinalizeMtxAsync(string orderId)
            => client.FinalizeMtxAsync(orderId);

        public Task PostTelemetryAsync(TelemetryBatchDto batch)
            => client.PostTelemetryAsync(batch);

        public Task<ListingListDto> GetListingsAsync(string itemId = null)
            => client.GetListingsAsync(itemId);

        public Task<ListingDto> CreateListingAsync(string grantId, int priceBloomShards)
            => client.CreateListingAsync(grantId, priceBloomShards);

        public Task<WalletDto> BuyListingAsync(string listingId, string idempotencyKey)
            => client.BuyListingAsync(listingId, idempotencyKey);

        public Task CancelListingAsync(string listingId)
            => client.CancelListingAsync(listingId);
    }
}
#endif
