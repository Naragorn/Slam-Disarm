namespace SlamDisarm.Logic
{
    /// <summary>
    /// The game raises no collision events for the hand of a standing NPC: Ragdoll.RefreshPartJointAndCollision
    /// turns every part's CollisionHandler off unless the ragdoll is Inert, Destabilized or Frozen, and the wrist and
    /// forearm handles (HandleRagdollData 'HumanWrist') turn them back on for Head, Neck, Torso and the arms only,
    /// not the hands (measured headless 2026-11-23). A hand slammed into a wall would never be seen. While a grab is
    /// watched the mod turns the NPC hand's events on, and afterwards gives back only what it changed.
    /// </summary>
    public static class HandEvents
    {
        /// <summary>Whether the mod turns the hand's events on now (true only when they are off).</summary>
        public static bool TurnOn(bool isOn)
        {
            return !isOn;
        }

        /// <summary>
        /// What to set the hand's events to when the watch ends: false to give back the off state, null to leave them.
        /// Left alone when the mod did not turn them on, when the game turned them off already, or when the game now
        /// keeps them on itself (the NPC is down).
        /// </summary>
        public static bool? Restore(bool turnedOnByMod, bool isOn, bool gameKeepsOn)
        {
            if (!turnedOnByMod || !isOn || gameKeepsOn)
            {
                return null;
            }
            return false;
        }
    }
}
