using System.Collections.Generic;
using SlamDisarm.Logic;
using ThunderRoad;

namespace SlamDisarm
{
    /// <summary>
    /// No Regrab Time: after a disarm, a hand that picks up or draws something lets go of it again until the
    /// time is up (DMCS DisarmStrike's pattern: RagdollHand.TryRelease every frame the hand holds something).
    /// Costs nothing while no NPC is locked.
    /// </summary>
    internal sealed class RegrabLocks
    {
        private sealed class Entry
        {
            public Creature Creature;
            public float LeftUntil = float.NegativeInfinity;
            public float RightUntil = float.NegativeInfinity;
        }

        private readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
        private readonly List<int> expired = new List<int>();

        public void Lock(Creature creature, Side side, float now)
        {
            var until = NoRegrabRules.LockUntil(now, ModOptions.NoRegrabTime);
            if (!NoRegrabRules.IsLocked(now, until))
            {
                return;
            }
            var id = creature.GetInstanceID();
            if (!entries.TryGetValue(id, out var entry))
            {
                entry = new Entry { Creature = creature };
                entries[id] = entry;
            }
            if (side == Side.Left)
            {
                entry.LeftUntil = until;
            }
            else
            {
                entry.RightUntil = until;
            }
        }

        public void Update(float now)
        {
            if (entries.Count == 0)
            {
                return;
            }
            expired.Clear();
            foreach (var pair in entries)
            {
                var entry = pair.Value;
                var leftLocked = NoRegrabRules.IsLocked(now, entry.LeftUntil);
                var rightLocked = NoRegrabRules.IsLocked(now, entry.RightUntil);
                if (entry.Creature == null || entry.Creature.isKilled || (!leftLocked && !rightLocked))
                {
                    expired.Add(pair.Key);
                    continue;
                }
                ReleaseIfLocked(entry.Creature, entry.Creature.handLeft, leftLocked);
                ReleaseIfLocked(entry.Creature, entry.Creature.handRight, rightLocked);
            }
            for (var i = 0; i < expired.Count; i++)
            {
                if (entries.TryGetValue(expired[i], out var entry) && entry.Creature != null && !entry.Creature.isKilled)
                {
                    ModLog.Verbose("REGRAB | " + GameAdapters.Label(entry.Creature) + " | may pick up weapons again");
                }
                entries.Remove(expired[i]);
            }
        }

        public void Forget(Creature creature)
        {
            if (creature != null)
            {
                entries.Remove(creature.GetInstanceID());
            }
        }

        /// <summary>Mod off or level unloading: every lock is lifted at once.</summary>
        public void Clear()
        {
            if (entries.Count > 0)
            {
                ModLog.Verbose("REGRAB | " + entries.Count + " lock(s) lifted");
            }
            entries.Clear();
        }

        private static void ReleaseIfLocked(Creature creature, RagdollHand hand, bool locked)
        {
            var item = GameAdapters.HeldItem(hand);
            if (!NoRegrabRules.ShouldRelease(ModOptions.Enabled, locked, item != null))
            {
                return;
            }
            hand.TryRelease();
            ModLog.Verbose("REGRAB | " + GameAdapters.Label(creature) + " | picked up or drew '" + GameAdapters.Label(item)
                + "' while disarmed: released (" + hand.side + " hand)");
        }
    }
}
