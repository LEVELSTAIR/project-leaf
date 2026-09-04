using Arborvale.Shared;
using Newtonsoft.Json;
using NUnit.Framework;
using System;

namespace Arborvale.Tests
{
    /// <summary>
    /// Guards the P0 fix: Newtonsoft.Json must correctly round-trip all DTO records.
    /// If JsonUtility were used instead, these would return default/empty values silently.
    /// </summary>
    public class DtoJsonRoundTripTests
    {
        [Test]
        public void WalletDto_RoundTrips()
        {
            var original = new WalletDto(100, 250);
            string json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<WalletDto>(json);
            Assert.AreEqual(100, restored.Premium);
            Assert.AreEqual(250, restored.BloomShards);
        }

        [Test]
        public void GachaPullResultDto_RoundTrips()
        {
            var original = new GachaPullResultDto(
                new[] { new GachaPullResultItem("grant-1", "item-flower-a", "Common") },
                new WalletDto(90, 248),
                new PityDto(3, 0)
            );
            string json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<GachaPullResultDto>(json);
            Assert.AreEqual(1, restored.Results.Length);
            Assert.AreEqual("grant-1", restored.Results[0].GrantId);
            Assert.AreEqual("Common", restored.Results[0].Tier);
            Assert.AreEqual(90, restored.Wallet.Premium);
            Assert.AreEqual(3, restored.Pity.Soft);
        }

        [Test]
        public void AuthResponseDto_RoundTrips()
        {
            var expiry = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            var original = new AuthResponseDto("jwt-token", "refresh-token", expiry, "76561198000000000");
            string json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<AuthResponseDto>(json);
            Assert.AreEqual("jwt-token", restored.Jwt);
            Assert.AreEqual("76561198000000000", restored.SteamId);
            Assert.AreEqual(expiry, restored.ExpiresAt.ToUniversalTime());
        }

        [Test]
        public void GraftConfigDto_RoundTrips()
        {
            var original = new GraftConfigDto(
                new[]
                {
                    new GraftCostDto("Oak", "Maple", "fertilizer", 3, 30f, 50)
                },
                Version: 2
            );
            string json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<GraftConfigDto>(json);
            Assert.AreEqual(1, restored.Costs.Length);
            Assert.AreEqual("Oak", restored.Costs[0].SpeciesA);
            Assert.AreEqual(50, restored.Costs[0].CurrencyCost);
            Assert.AreEqual(2, restored.Version);
        }

        [Test]
        public void ListingDto_RoundTrips()
        {
            var expiry = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
            var original = new ListingDto("listing-1", "grant-abc", "item-hybrid-oak", "76561198000000001", 120, expiry);
            string json = JsonConvert.SerializeObject(original);
            var restored = JsonConvert.DeserializeObject<ListingDto>(json);
            Assert.AreEqual("listing-1", restored.ListingId);
            Assert.AreEqual(120, restored.PriceBloomShards);
            Assert.AreEqual(expiry, restored.ExpiresAtUtc.ToUniversalTime());
        }
    }
}
