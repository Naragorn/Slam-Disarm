namespace SlamDisarm.Logic
{
    /// <summary>What a player hand holds.</summary>
    public enum GrabbedThing
    {
        Nothing,

        /// <summary>A body part of a creature (a ragdoll handle).</summary>
        BodyPart,

        /// <summary>An item (weapon, shield, object).</summary>
        Item,
    }

    /// <summary>Which body part a body-part grab holds.</summary>
    public enum GrabbedPart
    {
        Other,
        Hand,

        /// <summary>The lower arm.</summary>
        Forearm,
    }

    /// <summary>Why a grab does or does not let slams count. The first failing check wins.</summary>
    public enum GrabVerdict
    {
        Counts,
        ModOff,
        NotGrabbing,
        NotNpc,
        NotHeldByNpc,
        NpcDead,
        NotHandOrForearm,
        ForearmGrabOff,
        WeaponGrabOff,
        HandEmpty,
        Shield,
    }

    /// <summary>Plain facts about one player hand's grab, filled by the game glue.</summary>
    public struct GrabInput
    {
        public bool ModEnabled;
        public GrabbedThing Thing;

        /// <summary>BodyPart: the part belongs to a creature that is not the player. Item: an NPC hand holds it.</summary>
        public bool OwnerIsNpc;

        public bool OwnerAlive;

        /// <summary>BodyPart only.</summary>
        public GrabbedPart Part;

        /// <summary>The NPC hand on the grabbed side (BodyPart), or the NPC hand holding the grabbed item (Item), holds an item.</summary>
        public bool NpcHandHoldsItem;

        /// <summary>That held item is a shield.</summary>
        public bool HeldItemIsShield;

        public bool ForearmGrabCounts;
        public bool WeaponGrabCounts;
        public bool ShieldsCanBeSlammed;
    }

    /// <summary>Does a player's grab let slams count (player vs NPC only)?</summary>
    public static class GrabRules
    {
        public static GrabVerdict Decide(GrabInput input)
        {
            if (!input.ModEnabled)
            {
                return GrabVerdict.ModOff;
            }
            switch (input.Thing)
            {
                case GrabbedThing.BodyPart:
                    return DecideBodyPart(input);
                case GrabbedThing.Item:
                    return DecideItem(input);
                default:
                    return GrabVerdict.NotGrabbing;
            }
        }

        private static GrabVerdict DecideBodyPart(GrabInput input)
        {
            if (!input.OwnerIsNpc)
            {
                return GrabVerdict.NotNpc;
            }
            if (!input.OwnerAlive)
            {
                return GrabVerdict.NpcDead;
            }
            if (input.Part == GrabbedPart.Other)
            {
                return GrabVerdict.NotHandOrForearm;
            }
            if (input.Part == GrabbedPart.Forearm && !input.ForearmGrabCounts)
            {
                return GrabVerdict.ForearmGrabOff;
            }
            return DecideHeldItem(input);
        }

        private static GrabVerdict DecideItem(GrabInput input)
        {
            if (!input.WeaponGrabCounts)
            {
                return GrabVerdict.WeaponGrabOff;
            }
            if (!input.OwnerIsNpc)
            {
                return GrabVerdict.NotHeldByNpc;
            }
            if (!input.OwnerAlive)
            {
                return GrabVerdict.NpcDead;
            }
            return DecideHeldItem(input);
        }

        private static GrabVerdict DecideHeldItem(GrabInput input)
        {
            if (!input.NpcHandHoldsItem)
            {
                return GrabVerdict.HandEmpty;
            }
            if (input.HeldItemIsShield && !input.ShieldsCanBeSlammed)
            {
                return GrabVerdict.Shield;
            }
            return GrabVerdict.Counts;
        }
    }
}
