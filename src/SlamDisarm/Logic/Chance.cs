namespace SlamDisarm.Logic
{
    /// <summary>The menu's percentage options.</summary>
    public static class Chance
    {
        /// <summary>
        /// Whether a roll hits a chance. <paramref name="roll"/> is uniform in [0, 1]
        /// (UnityEngine.Random.value, which can return 1), so 100 % always and 0 % never hits.
        /// </summary>
        public static bool Hits(float chance, float roll)
        {
            if (chance >= 1f)
            {
                return true;
            }
            return chance > 0f && roll < chance;
        }
    }
}
