using SlamDisarm.Logic;
using Xunit;

namespace SlamDisarm.Tests
{
    public class GrabRulesTests
    {
        private static GrabInput HandGrab()
        {
            return new GrabInput
            {
                ModEnabled = true,
                Thing = GrabbedThing.BodyPart,
                OwnerIsNpc = true,
                OwnerAlive = true,
                Part = GrabbedPart.Hand,
                NpcHandHoldsItem = true,
                HeldItemIsShield = false,
                ForearmGrabCounts = true,
                WeaponGrabCounts = true,
                ShieldsCanBeSlammed = false,
            };
        }

        private static GrabInput WeaponGrab()
        {
            var input = HandGrab();
            input.Thing = GrabbedThing.Item;
            input.Part = GrabbedPart.Other;
            return input;
        }

        [Fact]
        public void Npc_weapon_hand_counts()
        {
            Assert.Equal(GrabVerdict.Counts, GrabRules.Decide(HandGrab()));
        }

        [Fact]
        public void Mod_off_refuses_first()
        {
            var input = HandGrab();
            input.ModEnabled = false;
            input.OwnerIsNpc = false;

            Assert.Equal(GrabVerdict.ModOff, GrabRules.Decide(input));
        }

        [Fact]
        public void Empty_player_hand_is_not_a_grab()
        {
            var input = HandGrab();
            input.Thing = GrabbedThing.Nothing;

            Assert.Equal(GrabVerdict.NotGrabbing, GrabRules.Decide(input));
        }

        [Fact]
        public void Body_part_of_the_player_or_nobody_is_refused()
        {
            var input = HandGrab();
            input.OwnerIsNpc = false;

            Assert.Equal(GrabVerdict.NotNpc, GrabRules.Decide(input));
        }

        [Fact]
        public void Dead_npc_is_refused()
        {
            var input = HandGrab();
            input.OwnerAlive = false;

            Assert.Equal(GrabVerdict.NpcDead, GrabRules.Decide(input));
        }

        [Fact]
        public void Other_body_parts_are_refused()
        {
            var input = HandGrab();
            input.Part = GrabbedPart.Other;

            Assert.Equal(GrabVerdict.NotHandOrForearm, GrabRules.Decide(input));
        }

        [Fact]
        public void Forearm_counts_when_allowed()
        {
            var input = HandGrab();
            input.Part = GrabbedPart.Forearm;

            Assert.Equal(GrabVerdict.Counts, GrabRules.Decide(input));
        }

        [Fact]
        public void Forearm_refused_when_turned_off()
        {
            var input = HandGrab();
            input.Part = GrabbedPart.Forearm;
            input.ForearmGrabCounts = false;

            Assert.Equal(GrabVerdict.ForearmGrabOff, GrabRules.Decide(input));
        }

        [Fact]
        public void Forearm_off_does_not_touch_hand_grabs()
        {
            var input = HandGrab();
            input.ForearmGrabCounts = false;

            Assert.Equal(GrabVerdict.Counts, GrabRules.Decide(input));
        }

        [Fact]
        public void Empty_npc_hand_is_refused()
        {
            var input = HandGrab();
            input.NpcHandHoldsItem = false;

            Assert.Equal(GrabVerdict.HandEmpty, GrabRules.Decide(input));
        }

        [Fact]
        public void Shield_hand_is_refused_by_default()
        {
            var input = HandGrab();
            input.HeldItemIsShield = true;

            Assert.Equal(GrabVerdict.Shield, GrabRules.Decide(input));
        }

        [Fact]
        public void Shield_hand_counts_when_allowed()
        {
            var input = HandGrab();
            input.HeldItemIsShield = true;
            input.ShieldsCanBeSlammed = true;

            Assert.Equal(GrabVerdict.Counts, GrabRules.Decide(input));
        }

        [Fact]
        public void Weapon_held_by_an_npc_counts()
        {
            Assert.Equal(GrabVerdict.Counts, GrabRules.Decide(WeaponGrab()));
        }

        [Fact]
        public void Weapon_grab_refused_when_turned_off()
        {
            var input = WeaponGrab();
            input.WeaponGrabCounts = false;

            Assert.Equal(GrabVerdict.WeaponGrabOff, GrabRules.Decide(input));
        }

        [Fact]
        public void Item_no_npc_holds_is_refused()
        {
            var input = WeaponGrab();
            input.OwnerIsNpc = false;
            input.NpcHandHoldsItem = false;

            Assert.Equal(GrabVerdict.NotHeldByNpc, GrabRules.Decide(input));
        }

        [Fact]
        public void Weapon_of_a_dead_npc_is_refused()
        {
            var input = WeaponGrab();
            input.OwnerAlive = false;

            Assert.Equal(GrabVerdict.NpcDead, GrabRules.Decide(input));
        }

        [Fact]
        public void Grabbed_shield_is_refused_by_default()
        {
            var input = WeaponGrab();
            input.HeldItemIsShield = true;

            Assert.Equal(GrabVerdict.Shield, GrabRules.Decide(input));
        }

        [Fact]
        public void Grabbed_shield_counts_when_allowed()
        {
            var input = WeaponGrab();
            input.HeldItemIsShield = true;
            input.ShieldsCanBeSlammed = true;

            Assert.Equal(GrabVerdict.Counts, GrabRules.Decide(input));
        }

        [Fact]
        public void Weapon_grab_off_does_not_touch_hand_grabs()
        {
            var input = HandGrab();
            input.WeaponGrabCounts = false;

            Assert.Equal(GrabVerdict.Counts, GrabRules.Decide(input));
        }
    }
}
