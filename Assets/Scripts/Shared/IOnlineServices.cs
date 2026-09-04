using System.Threading.Tasks;

namespace Arborvale.Shared
{
    /// <summary>
    /// Null when compiled without STEAM_BUILD or when auth hasn't completed.
    /// Check OnlineServices.Instance != null before calling any method.
    /// </summary>
    public interface IOnlineServices
    {
        bool IsAvailable { get; }

        // Wallet
        Task<WalletDto> GetWalletAsync();

        // Gacha
        Task<BannerListDto> GetBannersAsync();
        Task<GachaPullResultDto> PullAsync(string bannerId, int count, CurrencyType currency, string idempotencyKey);
        Task<GrantListDto> GetGrantsAsync();

        // Grafting
        Task<GraftConfigDto> GetGraftConfigAsync();
        Task<GraftAttemptResultDto> AttemptGraftAsync(string trunkPartId, string foliagePartId, string bloomPartId, string idempotencyKey);

        // Trade
        Task<TradeSessionDto> CreateTradeSessionAsync(string partnerSteamId);
        Task<TradeSessionDto> SetTradeOfferAsync(string sessionId, string[] grantIds);
        Task<TradeSessionDto> AcceptTradeAsync(string sessionId);
        Task<TradeSessionDto> GetTradeSessionAsync(string sessionId);

        // MTX
        Task<MtxInitResponseDto> InitMtxAsync(string bundleId, string language);
        Task<WalletDto> FinalizeMtxAsync(string orderId);

        // Telemetry
        Task PostTelemetryAsync(TelemetryBatchDto batch);

        // Marketplace listings
        Task<ListingListDto> GetListingsAsync(string itemId = null);
        Task<ListingDto> CreateListingAsync(string grantId, int priceBloomShards);
        Task<WalletDto> BuyListingAsync(string listingId, string idempotencyKey);
        Task CancelListingAsync(string listingId);
    }

    public enum CurrencyType { Premium, BloomShards }
}
