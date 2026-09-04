using Arborvale.Shared;
using NUnit.Framework;

namespace Arborvale.Tests
{
    public class HybridIdTests
    {
        [Test]
        public void Encode_ProducesExpectedFormat()
        {
            string id = HybridId.Encode("TR_Oak", "FO_Maple", "BL_Rose");
            Assert.AreEqual("hyb1:TR_Oak.FO_Maple.BL_Rose", id);
        }

        [Test]
        public void TryDecode_RoundTrip()
        {
            string id = HybridId.Encode("TR_Pine", "FO_Fern", "BL_Lily");
            bool ok = HybridId.TryDecode(id, out string trunk, out string foliage, out string bloom);
            Assert.IsTrue(ok);
            Assert.AreEqual("TR_Pine", trunk);
            Assert.AreEqual("FO_Fern", foliage);
            Assert.AreEqual("BL_Lily", bloom);
        }

        [Test]
        public void TryDecode_NullInput_ReturnsFalse()
        {
            Assert.IsFalse(HybridId.TryDecode(null, out _, out _, out _));
        }

        [Test]
        public void TryDecode_WrongPrefix_ReturnsFalse()
        {
            Assert.IsFalse(HybridId.TryDecode("v2:TR_Oak.FO_Maple.BL_Rose", out _, out _, out _));
        }

        [Test]
        public void TryDecode_TooFewParts_ReturnsFalse()
        {
            Assert.IsFalse(HybridId.TryDecode("hyb1:TR_Oak.FO_Maple", out _, out _, out _));
        }

        [Test]
        public void TryDecode_TooManyParts_ReturnsFalse()
        {
            Assert.IsFalse(HybridId.TryDecode("hyb1:TR_Oak.FO_Maple.BL_Rose.Extra", out _, out _, out _));
        }

        [Test]
        public void ToHash_IsStable()
        {
            string id = HybridId.Encode("TR_Oak", "FO_Maple", "BL_Rose");
            uint h1 = HybridId.ToHash(id);
            uint h2 = HybridId.ToHash(id);
            Assert.AreEqual(h1, h2);
        }

        [Test]
        public void ToHash_DifferentIds_DifferentHashes()
        {
            uint h1 = HybridId.ToHash(HybridId.Encode("TR_Oak", "FO_Maple", "BL_Rose"));
            uint h2 = HybridId.ToHash(HybridId.Encode("TR_Pine", "FO_Maple", "BL_Rose"));
            Assert.AreNotEqual(h1, h2);
        }
    }
}
