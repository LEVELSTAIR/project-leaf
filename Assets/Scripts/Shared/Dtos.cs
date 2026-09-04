using System;

namespace Arborvale.Shared
{
    // Auth
    public record AuthResponseDto(string Jwt, string RefreshToken, DateTime ExpiresAt, string SteamId);

    // Wallet
    public record WalletDto(int Premium, int BloomShards);

    // Gacha
    public record BannerDto(string BannerId, string Name, string Description, DateTime EndsAtUtc, string[] FeaturedItemIds, string DisplayedOddsText);
    public record BannerListDto(BannerDto[] Banners);
    public record GachaPullResultItem(string GrantId, string ItemId, string Tier);
    public record PityDto(int Soft, int Hard);
    public record GachaPullResultDto(GachaPullResultItem[] Results, WalletDto Wallet, PityDto Pity);

    // Grants
    public record GrantDto(string GrantId, string ItemId, string Tier, string Source, DateTime AcquiredAt);
    public record GrantListDto(GrantDto[] Grants);

    // Grafting
    public record GraftCostDto(string SpeciesA, string SpeciesB, string FertilizerItemName, int FertilizerAmount, float GraftTimeSeconds, int CurrencyCost);
    public record GraftConfigDto(GraftCostDto[] Costs, int Version);
    public record GraftAttemptResultDto(bool Success, string HybridId, string GrantId, WalletDto Wallet);

    // Trade
    public record TradeSessionDto(string SessionId, string State, string[] OfferA, string[] OfferB, DateTime? CompletedAt);

    // MTX
    public record MtxInitResponseDto(string OrderId);

    // Telemetry
    public record TelemetryEventDto(string EventType, string Payload, long TimestampMs);
    public record TelemetryBatchDto(TelemetryEventDto[] Events);

    // Marketplace listings
    public record ListingDto(string ListingId, string GrantId, string ItemId, string SellerSteamId, int PriceBloomShards, DateTime ExpiresAtUtc);
    public record ListingListDto(ListingDto[] Listings);
    public record CreateListingRequestDto(string GrantId, int PriceBloomShards);
}
