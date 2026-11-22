using SlamDisarm.Logic;
using Xunit;

namespace SlamDisarm.Tests
{
    public class SlamRulesTests
    {
        private static SlamSettings Defaults()
        {
            return new SlamSettings
            {
                ObjectsCount = true,
                MinSlamSpeed = 2.5f,
                SlamCooldown = 0.3f,
                SlamsToDisarm = 2,
                SlamWindow = 3f,
                DisarmChance = 1f,
            };
        }

        private static SlamDecision Slam(SlamSeries series, float now, SlamTarget target = SlamTarget.World, float speed = 4f, float roll = 0.5f, SlamSettings? settings = null)
        {
            return SlamRules.Decide(series, target, speed, now, roll, settings ?? Defaults());
        }

        [Fact]
        public void First_slam_is_counted_not_disarming()
        {
            var series = new SlamSeries();

            var d = Slam(series, 10f);

            Assert.Equal(SlamOutcome.Counted, d.Outcome);
            Assert.Equal(1, d.Count);
            Assert.Equal(2, d.Needed);
        }

        [Fact]
        public void Second_slam_in_the_window_disarms_and_resets()
        {
            var series = new SlamSeries();
            Slam(series, 10f);

            var d = Slam(series, 11f);

            Assert.Equal(SlamOutcome.Disarm, d.Outcome);
            Assert.Equal(2, d.Count);
            Assert.Equal(0, series.Count);
        }

        [Fact]
        public void One_slam_needed_disarms_at_once()
        {
            var settings = Defaults();
            settings.SlamsToDisarm = 1;

            var d = Slam(new SlamSeries(), 10f, settings: settings);

            Assert.Equal(SlamOutcome.Disarm, d.Outcome);
            Assert.Equal(1, d.Needed);
        }

        [Fact]
        public void Zero_slams_needed_is_treated_as_one()
        {
            var settings = Defaults();
            settings.SlamsToDisarm = 0;

            var d = Slam(new SlamSeries(), 10f, settings: settings);

            Assert.Equal(SlamOutcome.Disarm, d.Outcome);
            Assert.Equal(1, d.Needed);
        }

        [Fact]
        public void Slam_after_the_window_starts_over()
        {
            var series = new SlamSeries();
            Slam(series, 10f);

            var d = Slam(series, 13.5f);

            Assert.Equal(SlamOutcome.Counted, d.Outcome);
            Assert.Equal(1, d.Count);
        }

        [Fact]
        public void Slam_exactly_at_the_window_end_still_counts_on()
        {
            var series = new SlamSeries();
            Slam(series, 10f);

            var d = Slam(series, 13f);

            Assert.Equal(SlamOutcome.Disarm, d.Outcome);
        }

        [Fact]
        public void Contact_within_the_cooldown_is_ignored_and_changes_nothing()
        {
            var series = new SlamSeries();
            Slam(series, 10f);

            var d = Slam(series, 10.2f);

            Assert.Equal(SlamOutcome.Ignored, d.Outcome);
            Assert.Equal(SlamIgnoreReason.Cooldown, d.Reason);
            Assert.Equal(1, series.Count);
            Assert.Equal(10f, series.LastCountedTime);
        }

        [Fact]
        public void Contact_right_after_the_cooldown_counts()
        {
            var series = new SlamSeries();
            Slam(series, 10f);

            var d = Slam(series, 10.3f);

            Assert.Equal(SlamOutcome.Disarm, d.Outcome);
        }

        [Fact]
        public void Too_slow_is_ignored()
        {
            var series = new SlamSeries();

            var d = Slam(series, 10f, speed: 2.4f);

            Assert.Equal(SlamOutcome.Ignored, d.Outcome);
            Assert.Equal(SlamIgnoreReason.TooSlow, d.Reason);
            Assert.Equal(0, series.Count);
        }

        [Fact]
        public void Exactly_the_min_speed_counts()
        {
            var d = Slam(new SlamSeries(), 10f, speed: 2.5f);

            Assert.Equal(SlamOutcome.Counted, d.Outcome);
        }

        [Fact]
        public void Too_slow_does_not_start_the_cooldown()
        {
            var series = new SlamSeries();
            Slam(series, 10f, speed: 1f);

            var d = Slam(series, 10.1f);

            Assert.Equal(SlamOutcome.Counted, d.Outcome);
        }

        [Fact]
        public void Creatures_never_count()
        {
            var d = Slam(new SlamSeries(), 10f, SlamTarget.Creature, speed: 99f);

            Assert.Equal(SlamIgnoreReason.Creature, d.Reason);
        }

        [Fact]
        public void Held_objects_never_count()
        {
            var d = Slam(new SlamSeries(), 10f, SlamTarget.HeldObject, speed: 99f);

            Assert.Equal(SlamIgnoreReason.HeldObject, d.Reason);
        }

        [Fact]
        public void Loose_objects_count_by_default()
        {
            var d = Slam(new SlamSeries(), 10f, SlamTarget.LooseObject);

            Assert.Equal(SlamOutcome.Counted, d.Outcome);
        }

        [Fact]
        public void Loose_objects_refused_when_turned_off()
        {
            var settings = Defaults();
            settings.ObjectsCount = false;

            var d = Slam(new SlamSeries(), 10f, SlamTarget.LooseObject, settings: settings);

            Assert.Equal(SlamIgnoreReason.ObjectsOff, d.Reason);
        }

        [Fact]
        public void Objects_off_keeps_the_world()
        {
            var settings = Defaults();
            settings.ObjectsCount = false;

            var d = Slam(new SlamSeries(), 10f, SlamTarget.World, settings: settings);

            Assert.Equal(SlamOutcome.Counted, d.Outcome);
        }

        [Fact]
        public void Target_is_checked_before_speed()
        {
            var d = Slam(new SlamSeries(), 10f, SlamTarget.Creature, speed: 0f);

            Assert.Equal(SlamIgnoreReason.Creature, d.Reason);
        }

        [Fact]
        public void Failed_chance_resists_and_keeps_the_count_full()
        {
            var settings = Defaults();
            settings.DisarmChance = 0.5f;
            var series = new SlamSeries();
            Slam(series, 10f, settings: settings);

            var d = Slam(series, 11f, roll: 0.9f, settings: settings);

            Assert.Equal(SlamOutcome.Resisted, d.Outcome);
            Assert.Equal(2, series.Count);
        }

        [Fact]
        public void After_a_resist_the_next_slam_rolls_again()
        {
            var settings = Defaults();
            settings.DisarmChance = 0.5f;
            var series = new SlamSeries();
            Slam(series, 10f, settings: settings);
            Slam(series, 11f, roll: 0.9f, settings: settings);

            var d = Slam(series, 12f, roll: 0.1f, settings: settings);

            Assert.Equal(SlamOutcome.Disarm, d.Outcome);
            Assert.Equal(2, d.Count);
        }

        [Fact]
        public void Chance_is_not_rolled_before_the_last_slam()
        {
            var settings = Defaults();
            settings.DisarmChance = 0f;

            var d = Slam(new SlamSeries(), 10f, roll: 0f, settings: settings);

            Assert.Equal(SlamOutcome.Counted, d.Outcome);
        }

        [Fact]
        public void Zero_chance_never_disarms()
        {
            var settings = Defaults();
            settings.DisarmChance = 0f;
            settings.SlamsToDisarm = 1;

            var d = Slam(new SlamSeries(), 10f, roll: 0f, settings: settings);

            Assert.Equal(SlamOutcome.Resisted, d.Outcome);
        }

        [Fact]
        public void Lowered_slams_needed_applies_to_a_running_series()
        {
            var settings = Defaults();
            settings.SlamsToDisarm = 4;
            var series = new SlamSeries();
            Slam(series, 10f, settings: settings);
            Slam(series, 11f, settings: settings);
            settings.SlamsToDisarm = 2;

            var d = Slam(series, 12f, settings: settings);

            Assert.Equal(SlamOutcome.Disarm, d.Outcome);
            Assert.Equal(2, d.Count);
        }
    }
}
