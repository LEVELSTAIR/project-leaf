using NUnit.Framework;
using UnityEngine;

namespace Arborvale.Tests
{
    /// <summary>
    /// EditMode tests for GrantStore FIFO grant-ID tracking.
    /// Uses new GameObject().AddComponent to avoid singleton state bleed.
    /// </summary>
    public class GrantStoreTests
    {
        private GameObject go;
        private GrantStore store;

        [SetUp]
        public void SetUp()
        {
            // Bypass the singleton guard — only one GrantStore per test.
            go = new GameObject("GrantStore_Test");
            store = go.AddComponent<GrantStore>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void AddGrant_ThenHasGrant_ReturnsTrue()
        {
            store.AddGrant("HybridOak", "grant-001");
            Assert.IsTrue(store.HasGrant("HybridOak"));
        }

        [Test]
        public void HasGrant_UnknownItem_ReturnsFalse()
        {
            Assert.IsFalse(store.HasGrant("UnknownItem"));
        }

        [Test]
        public void TryConsumeGrant_FIFO_Order()
        {
            store.AddGrant("HybridOak", "grant-001");
            store.AddGrant("HybridOak", "grant-002");
            store.AddGrant("HybridOak", "grant-003");

            store.TryConsumeGrant("HybridOak", out string first);
            store.TryConsumeGrant("HybridOak", out string second);
            store.TryConsumeGrant("HybridOak", out string third);

            Assert.AreEqual("grant-001", first);
            Assert.AreEqual("grant-002", second);
            Assert.AreEqual("grant-003", third);
        }

        [Test]
        public void TryConsumeGrant_EmptyAfterDepletion()
        {
            store.AddGrant("HybridOak", "grant-001");
            store.TryConsumeGrant("HybridOak", out _);
            Assert.IsFalse(store.HasGrant("HybridOak"));
        }

        [Test]
        public void TryConsumeGrant_NoGrant_ReturnsFalse()
        {
            bool result = store.TryConsumeGrant("HybridOak", out string grantId);
            Assert.IsFalse(result);
            Assert.IsNull(grantId);
        }

        [Test]
        public void GrantCount_ReflectsQueueLength()
        {
            store.AddGrant("HybridPine", "g-1");
            store.AddGrant("HybridPine", "g-2");
            Assert.AreEqual(2, store.GrantCount("HybridPine"));

            store.TryConsumeGrant("HybridPine", out _);
            Assert.AreEqual(1, store.GrantCount("HybridPine"));
        }

        [Test]
        public void AddGrant_NullOrEmptyGrantId_Ignored()
        {
            store.AddGrant("HybridOak", null);
            store.AddGrant("HybridOak", "");
            Assert.IsFalse(store.HasGrant("HybridOak"));
        }

        [Test]
        public void MultipleItems_DoNotInterfere()
        {
            store.AddGrant("ItemA", "ga-1");
            store.AddGrant("ItemB", "gb-1");
            store.AddGrant("ItemB", "gb-2");

            Assert.AreEqual(1, store.GrantCount("ItemA"));
            Assert.AreEqual(2, store.GrantCount("ItemB"));

            store.TryConsumeGrant("ItemA", out string ga);
            Assert.AreEqual("ga-1", ga);
            Assert.IsFalse(store.HasGrant("ItemA"));
            Assert.IsTrue(store.HasGrant("ItemB"));
        }
    }
}
