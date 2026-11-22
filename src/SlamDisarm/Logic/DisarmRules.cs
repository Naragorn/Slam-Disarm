namespace SlamDisarm.Logic
{
    public struct HandsToRelease
    {
        /// <summary>The hand whose grip was slammed loose.</summary>
        public bool Slammed;

        /// <summary>The NPC's other hand.</summary>
        public bool Other;
    }

    /// <summary>Which NPC hands let go on a disarm.</summary>
    public static class DisarmRules
    {
        public static HandsToRelease HandsToRelease(bool dropBothHands, bool otherHandHolds, bool otherHandIsShield, bool shieldsCanBeSlammed)
        {
            return new HandsToRelease
            {
                Slammed = true,
                Other = dropBothHands && otherHandHolds && (!otherHandIsShield || shieldsCanBeSlammed),
            };
        }
    }

    /// <summary>After a disarm the NPC cannot pick up or draw a weapon for No Regrab Time.</summary>
    public static class NoRegrabRules
    {
        /// <summary>The lock's end; No Regrab Time 0 gives no lock (it ends now).</summary>
        public static float LockUntil(float now, float noRegrabTime)
        {
            return noRegrabTime > 0f ? now + noRegrabTime : now;
        }

        public static bool IsLocked(float now, float until)
        {
            return now < until;
        }

        /// <summary>A locked hand that holds something again lets go (only while the mod is on).</summary>
        public static bool ShouldRelease(bool modEnabled, bool locked, bool handHoldsItem)
        {
            return modEnabled && locked && handHoldsItem;
        }
    }
}
