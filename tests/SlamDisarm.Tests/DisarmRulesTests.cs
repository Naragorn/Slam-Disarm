using SlamDisarm.Logic;
using Xunit;

namespace SlamDisarm.Tests
{
    public class DisarmRulesTests
    {
        [Fact]
        public void Only_the_slammed_hand_lets_go_by_default()
        {
            var hands = DisarmRules.HandsToRelease(dropBothHands: false, otherHandHolds: true, otherHandIsShield: false, shieldsCanBeSlammed: false);

            Assert.True(hands.Slammed);
            Assert.False(hands.Other);
        }

        [Fact]
        public void Drop_both_hands_releases_the_other_weapon()
        {
            var hands = DisarmRules.HandsToRelease(dropBothHands: true, otherHandHolds: true, otherHandIsShield: false, shieldsCanBeSlammed: false);

            Assert.True(hands.Other);
        }

        [Fact]
        public void Drop_both_hands_skips_an_empty_other_hand()
        {
            var hands = DisarmRules.HandsToRelease(dropBothHands: true, otherHandHolds: false, otherHandIsShield: false, shieldsCanBeSlammed: false);

            Assert.False(hands.Other);
        }

        [Fact]
        public void Drop_both_hands_keeps_a_shield_by_default()
        {
            var hands = DisarmRules.HandsToRelease(dropBothHands: true, otherHandHolds: true, otherHandIsShield: true, shieldsCanBeSlammed: false);

            Assert.False(hands.Other);
        }

        [Fact]
        public void Drop_both_hands_drops_a_shield_when_shields_count()
        {
            var hands = DisarmRules.HandsToRelease(dropBothHands: true, otherHandHolds: true, otherHandIsShield: true, shieldsCanBeSlammed: true);

            Assert.True(hands.Other);
        }

        [Fact]
        public void No_regrab_lock_lasts_the_set_time()
        {
            var until = NoRegrabRules.LockUntil(now: 10f, noRegrabTime: 5f);

            Assert.True(NoRegrabRules.IsLocked(14.9f, until));
            Assert.False(NoRegrabRules.IsLocked(15f, until));
        }

        [Fact]
        public void No_regrab_time_zero_gives_no_lock()
        {
            var until = NoRegrabRules.LockUntil(now: 10f, noRegrabTime: 0f);

            Assert.False(NoRegrabRules.IsLocked(10f, until));
        }

        [Theory]
        [InlineData(true, true, true, true)]
        [InlineData(false, true, true, false)]
        [InlineData(true, false, true, false)]
        [InlineData(true, true, false, false)]
        public void Locked_hand_that_picked_something_up_lets_go(bool modEnabled, bool locked, bool handHolds, bool expected)
        {
            Assert.Equal(expected, NoRegrabRules.ShouldRelease(modEnabled, locked, handHolds));
        }
    }
}
