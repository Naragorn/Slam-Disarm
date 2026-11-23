using System.Collections.Generic;
using System.Globalization;
using SlamDisarm.Logic;
using ThunderRoad;
using UnityEngine;

namespace SlamDisarm
{
    /// <summary>
    /// Polls the player's two hands each frame (<see cref="GrabRules"/>), keeps one <see cref="SlamWatch"/> per
    /// hand, decides every contact with <see cref="SlamRules"/> and applies the outcome: feedback, hand damage,
    /// the disarm (RagdollHand.TryRelease + EventManager.InvokeDisarm, as DMCS and the game's hand-hit disarm do)
    /// and the No Regrab Time lock.
    /// </summary>
    internal sealed class SlamController
    {
        // Strength of the controller tick on a counted slam (RagdollHand.HapticTick takes 0..1).
        private const float HapticIntensity = 1f;

        // The voice line every human voice has for taking a hit (Voices/*.json in the 1.3.2 catalog).
        private const string PainDialog = "hit";

        private readonly SlamWatch left;
        private readonly SlamWatch right;
        private readonly Dictionary<int, SlamSeries> series = new Dictionary<int, SlamSeries>();
        private readonly RegrabLocks locks = new RegrabLocks();

        public SlamController()
        {
            left = new SlamWatch(Side.Left, OnContact);
            right = new SlamWatch(Side.Right, OnContact);
        }

        public void Update(float now)
        {
            var player = Player.currentCreature;
            if (!ModOptions.Enabled || player == null)
            {
                StopWatches();
            }
            else
            {
                Track(left, player.handLeft, right);
                Track(right, player.handRight, left);
            }
            locks.Update(now);
        }

        /// <summary>Mod off, unloaded or level unloading: stop listening, lift every lock, forget every series.</summary>
        public void Clear()
        {
            StopWatches();
            locks.Clear();
            series.Clear();
        }

        public void Forget(Creature creature)
        {
            if (creature == null)
            {
                return;
            }
            series.Remove(creature.GetInstanceID());
            locks.Forget(creature);
            if (left.Npc == creature)
            {
                left.Stop();
            }
            if (right.Npc == creature)
            {
                right.Stop();
            }
        }

        private void StopWatches()
        {
            left.Stop();
            right.Stop();
        }

        /// <summary>
        /// Keeps a hand's watch in line with what the hand holds. When the other hand already watches the same NPC
        /// hand and weapon (one hand on the wrist, the other on the blade), this one stays idle so a contact is not
        /// decided twice.
        /// </summary>
        private static void Track(SlamWatch watch, RagdollHand playerHand, SlamWatch other)
        {
            var reading = GameAdapters.ReadGrab(playerHand);
            if (GrabRules.Decide(reading.Input) != GrabVerdict.Counts)
            {
                watch.Stop();
                return;
            }
            var weapon = GameAdapters.HeldItem(reading.NpcHand);
            if (other.Watches(reading.NpcHand, weapon))
            {
                watch.Stop();
                return;
            }
            if (!watch.Watches(reading.NpcHand, weapon))
            {
                watch.Start(playerHand, reading.Npc, reading.NpcHand, weapon);
            }
        }

        private void OnContact(SlamWatch watch, CollisionInstance hit)
        {
            var npc = watch.Npc;
            if (!ModOptions.Enabled || npc == null || npc.isKilled)
            {
                return;
            }
            var target = GameAdapters.Target(hit);
            var speed = hit.impactVelocity.magnitude;
            var decision = SlamRules.Decide(SeriesOf(npc), target, speed, Time.time, Random.value, ModOptions.SlamSettings());
            if (ModOptions.DebugLogging)
            {
                LogDecision(npc, target, speed, decision, hit);
            }
            switch (decision.Outcome)
            {
                case SlamOutcome.Ignored:
                    return;
                case SlamOutcome.Counted:
                case SlamOutcome.Resisted:
                    Hurt(npc);
                    Feedback(npc, watch.PlayerHand);
                    return;
                default:
                    Hurt(npc);
                    Disarm(npc, watch.NpcHand, hit);
                    return;
            }
        }

        private static void LogDecision(Creature npc, SlamTarget target, float speed, SlamDecision decision, CollisionInstance hit)
        {
            var what = GameAdapters.Label(npc) + " | " + target + " at " + F(speed) + " m/s (" + GameAdapters.Contact(hit) + ") | ";
            var count = decision.Count + "/" + decision.Needed;
            switch (decision.Outcome)
            {
                case SlamOutcome.Ignored:
                    ModLog.Info("SKIP | " + what + decision.Reason + " (count " + count + ")");
                    return;
                case SlamOutcome.Counted:
                    ModLog.Info("SLAM | " + what + count);
                    return;
                case SlamOutcome.Resisted:
                    ModLog.Info("SLAM | " + what + count + " | grip held (Disarm Chance " + Percent(ModOptions.DisarmChance) + ")");
                    return;
                default:
                    ModLog.Info("SLAM | " + what + count + " | disarm");
                    return;
            }
        }

        private SlamSeries SeriesOf(Creature npc)
        {
            var id = npc.GetInstanceID();
            if (!series.TryGetValue(id, out var s))
            {
                s = new SlamSeries();
                series[id] = s;
            }
            return s;
        }

        private static void Feedback(Creature npc, RagdollHand playerHand)
        {
            if (ModOptions.PainSound)
            {
                var speak = npc.brain != null && npc.brain.instance != null
                    ? npc.brain.instance.GetModule<BrainModuleSpeak>(autoAddModuleIfMissing: false)
                    : null;
                if (speak != null)
                {
                    speak.Play(PainDialog, true);
                }
            }
            if (ModOptions.HapticPulse && playerHand != null)
            {
                playerHand.HapticTick(HapticIntensity);
            }
        }

        /// <summary>
        /// Hand Damage through the game's own Creature.Damage(amount, type): no body part and push level 0, so the
        /// game's grab and hand-hit disarms (BrainModuleEquipment.OnDamage, push level 2) do not fire, and this mod
        /// does not listen to hits, so it cannot loop.
        /// </summary>
        private static void Hurt(Creature npc)
        {
            if (ModOptions.HandDamage > 0f && !npc.isKilled)
            {
                npc.Damage(ModOptions.HandDamage, DamageType.Blunt);
            }
        }

        private void Disarm(Creature npc, RagdollHand hand, CollisionInstance hit)
        {
            var other = hand.otherHand;
            var otherItem = GameAdapters.HeldItem(other);
            var hands = DisarmRules.HandsToRelease(ModOptions.DropBothHands, otherItem != null, GameAdapters.IsShield(otherItem), ModOptions.ShieldsCanBeSlammed);
            var slammedItem = Release(npc, hand, hit);
            var otherDropped = hands.Other ? Release(npc, other, hit) : null;
            if (ModOptions.DebugLogging)
            {
                ModLog.Info("DISARM | " + GameAdapters.Label(npc) + " | " + hand.side + " hand dropped '" + GameAdapters.Label(slammedItem) + "'"
                    + (otherDropped != null ? ", " + other.side + " hand dropped '" + GameAdapters.Label(otherDropped) + "'" : "")
                    + " | no regrab for " + F(ModOptions.NoRegrabTime) + " s");
            }
        }

        /// <summary>Lets the hand go and raises the game's disarm event; returns the dropped item or null when the hand was empty.</summary>
        private Item Release(Creature npc, RagdollHand hand, CollisionInstance hit)
        {
            var handle = hand.grabbedHandle;
            var item = GameAdapters.HeldItem(hand);
            if (handle == null || item == null)
            {
                return null;
            }
            hand.TryRelease();
            EventManager.InvokeDisarm(npc, hand, handle, hit);
            locks.Lock(npc, hand.side, Time.time);
            return item;
        }

        private static string F(float value) => value.ToString("0.0#", CultureInfo.InvariantCulture);

        private static string Percent(float value) => (value * 100f).ToString("0", CultureInfo.InvariantCulture) + " %";
    }
}
