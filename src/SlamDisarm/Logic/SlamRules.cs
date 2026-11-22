namespace SlamDisarm.Logic
{
    /// <summary>What the slammed hand, forearm or weapon hit.</summary>
    public enum SlamTarget
    {
        /// <summary>Level geometry: a collider with no collider group (walls, floor, rocks).</summary>
        World,

        /// <summary>An item nobody holds, or another collider group that is neither an item nor a body.</summary>
        LooseObject,

        /// <summary>An item someone holds (a parry, not a slam).</summary>
        HeldObject,

        /// <summary>A body part of any creature (the NPC itself, the player, other NPCs).</summary>
        Creature,
    }

    public enum SlamOutcome
    {
        Ignored,

        /// <summary>Counted towards the disarm, not enough slams yet.</summary>
        Counted,

        /// <summary>Last needed slam, but the Disarm Chance roll failed; the next slam rolls again.</summary>
        Resisted,

        Disarm,
    }

    public enum SlamIgnoreReason
    {
        None,
        Creature,
        HeldObject,
        ObjectsOff,
        TooSlow,
        Cooldown,
    }

    public struct SlamSettings
    {
        public bool ObjectsCount;
        public float MinSlamSpeed;
        public float SlamCooldown;
        public int SlamsToDisarm;
        public float SlamWindow;
        public float DisarmChance;
    }

    public struct SlamDecision
    {
        public SlamOutcome Outcome;
        public SlamIgnoreReason Reason;

        /// <summary>Slams counted in the current series, this one included.</summary>
        public int Count;

        public int Needed;
    }

    /// <summary>One NPC's running series of slams.</summary>
    public sealed class SlamSeries
    {
        public int Count;
        public float LastCountedTime = float.NegativeInfinity;
    }

    /// <summary>Whether a contact of the grabbed hand, forearm or weapon is a slam, and what it does.</summary>
    public static class SlamRules
    {
        public static SlamTarget Classify(bool hasGroup, bool isCreaturePart, bool isItem, bool itemHeld)
        {
            if (!hasGroup)
            {
                return SlamTarget.World;
            }
            if (isCreaturePart)
            {
                return SlamTarget.Creature;
            }
            return isItem && itemHeld ? SlamTarget.HeldObject : SlamTarget.LooseObject;
        }

        /// <summary>
        /// Decides one contact and updates the series. Checks in this order: target, speed, cooldown; then the
        /// window (a slam later than Slam Window after the last counted one starts a new series), the count, and
        /// only on the last needed slam the Disarm Chance roll. A disarm resets the series; a failed roll keeps it full.
        /// </summary>
        public static SlamDecision Decide(SlamSeries series, SlamTarget target, float speed, float now, float roll, SlamSettings settings)
        {
            var needed = settings.SlamsToDisarm < 1 ? 1 : settings.SlamsToDisarm;
            var reason = IgnoreReason(series, target, speed, now, settings);
            if (reason != SlamIgnoreReason.None)
            {
                return new SlamDecision { Outcome = SlamOutcome.Ignored, Reason = reason, Count = series.Count, Needed = needed };
            }
            if (now - series.LastCountedTime > settings.SlamWindow)
            {
                series.Count = 0;
            }
            series.Count = series.Count + 1 > needed ? needed : series.Count + 1;
            series.LastCountedTime = now;
            var decision = new SlamDecision { Outcome = SlamOutcome.Counted, Count = series.Count, Needed = needed };
            if (series.Count < needed)
            {
                return decision;
            }
            if (!Chance.Hits(settings.DisarmChance, roll))
            {
                decision.Outcome = SlamOutcome.Resisted;
                return decision;
            }
            decision.Outcome = SlamOutcome.Disarm;
            series.Count = 0;
            return decision;
        }

        private static SlamIgnoreReason IgnoreReason(SlamSeries series, SlamTarget target, float speed, float now, SlamSettings settings)
        {
            switch (target)
            {
                case SlamTarget.Creature:
                    return SlamIgnoreReason.Creature;
                case SlamTarget.HeldObject:
                    return SlamIgnoreReason.HeldObject;
                case SlamTarget.LooseObject when !settings.ObjectsCount:
                    return SlamIgnoreReason.ObjectsOff;
            }
            if (speed < settings.MinSlamSpeed)
            {
                return SlamIgnoreReason.TooSlow;
            }
            if (now - series.LastCountedTime < settings.SlamCooldown)
            {
                return SlamIgnoreReason.Cooldown;
            }
            return SlamIgnoreReason.None;
        }
    }
}
