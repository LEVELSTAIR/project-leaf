namespace Arborvale.Shared
{
    /// <summary>
    /// Deterministic, order-independent-within-slots hybrid identifier.
    /// Format: hyb1:{trunkPartId}.{foliagePartId}.{bloomPartId}
    /// Both client and server compute the same ID from the same part IDs.
    /// </summary>
    public static class HybridId
    {
        public const string SchemePrefix = "hyb1:";

        public static string Encode(string trunkPartId, string foliagePartId, string bloomPartId)
        {
            return $"{SchemePrefix}{trunkPartId}.{foliagePartId}.{bloomPartId}";
        }

        public static bool TryDecode(string hybridId, out string trunk, out string foliage, out string bloom)
        {
            trunk = foliage = bloom = null;
            if (hybridId == null || !hybridId.StartsWith(SchemePrefix))
                return false;

            var parts = hybridId.Substring(SchemePrefix.Length).Split('.');
            if (parts.Length != 3)
                return false;

            trunk = parts[0];
            foliage = parts[1];
            bloom = parts[2];
            return true;
        }

        public static uint ToHash(string hybridId)
        {
            // FNV-1a 32-bit
            uint hash = 2166136261u;
            foreach (char c in hybridId)
            {
                hash ^= (byte)c;
                hash *= 16777619u;
            }
            return hash;
        }
    }
}
