using SlamDisarm.Logic;
using Xunit;

namespace SlamDisarm.Tests
{
    public class HandEventsTests
    {
        [Fact]
        public void Off_hand_is_turned_on()
        {
            Assert.True(HandEvents.TurnOn(isOn: false));
        }

        [Fact]
        public void On_hand_is_left_alone()
        {
            Assert.False(HandEvents.TurnOn(isOn: true));
        }

        [Fact]
        public void Restore_turns_off_what_the_mod_turned_on()
        {
            Assert.Equal(false, HandEvents.Restore(turnedOnByMod: true, isOn: true, gameKeepsOn: false));
        }

        [Fact]
        public void Restore_leaves_a_hand_the_mod_never_touched()
        {
            Assert.Null(HandEvents.Restore(turnedOnByMod: false, isOn: true, gameKeepsOn: false));
        }

        [Fact]
        public void Restore_leaves_a_hand_the_game_turned_off_already()
        {
            Assert.Null(HandEvents.Restore(turnedOnByMod: true, isOn: false, gameKeepsOn: false));
        }

        [Fact]
        public void Restore_leaves_a_hand_the_game_keeps_on_now()
        {
            // The NPC went down (ragdoll Destabilized) while held: the game wants the events on itself.
            Assert.Null(HandEvents.Restore(turnedOnByMod: true, isOn: true, gameKeepsOn: true));
        }
    }
}
