using SlamDisarm.Logic;
using Xunit;

namespace SlamDisarm.Tests
{
    public class SlamTargetTests
    {
        [Fact]
        public void No_collider_group_is_the_world()
        {
            Assert.Equal(SlamTarget.World, SlamRules.Classify(hasGroup: false, isCreaturePart: false, isItem: false, itemHeld: false));
        }

        [Fact]
        public void No_group_wins_over_stale_flags()
        {
            Assert.Equal(SlamTarget.World, SlamRules.Classify(hasGroup: false, isCreaturePart: true, isItem: true, itemHeld: true));
        }

        [Fact]
        public void Body_part_is_a_creature()
        {
            Assert.Equal(SlamTarget.Creature, SlamRules.Classify(hasGroup: true, isCreaturePart: true, isItem: false, itemHeld: false));
        }

        [Fact]
        public void Held_item_is_a_held_object()
        {
            Assert.Equal(SlamTarget.HeldObject, SlamRules.Classify(hasGroup: true, isCreaturePart: false, isItem: true, itemHeld: true));
        }

        [Fact]
        public void Free_item_is_a_loose_object()
        {
            Assert.Equal(SlamTarget.LooseObject, SlamRules.Classify(hasGroup: true, isCreaturePart: false, isItem: true, itemHeld: false));
        }

        [Fact]
        public void Group_that_is_neither_item_nor_body_is_a_loose_object()
        {
            Assert.Equal(SlamTarget.LooseObject, SlamRules.Classify(hasGroup: true, isCreaturePart: false, isItem: false, itemHeld: false));
        }
    }
}
