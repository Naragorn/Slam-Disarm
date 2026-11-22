using System.Collections.Generic;
using System.Linq;
using SlamDisarm.Logic;
using Xunit;

namespace SlamDisarm.Tests
{
    public class OptionSpecTests
    {
        public static IEnumerable<object[]> AllSpecs()
        {
            return OptionSpecs.All.Select(spec => new object[] { spec.Name, spec });
        }

        [Theory]
        [MemberData(nameof(AllSpecs))]
        public void Default_is_one_of_the_values_exactly(string name, FloatOptionSpec spec)
        {
            // The game's mod menu finds the default with object.Equals on the
            // boxed float, so the default must be bit-identical to one entry.
            Assert.Contains(spec.Values(), v => v.Equals(spec.Default));
            Assert.False(string.IsNullOrEmpty(name));
        }

        [Theory]
        [MemberData(nameof(AllSpecs))]
        public void Values_run_from_min_to_max_in_steps(string name, FloatOptionSpec spec)
        {
            var values = spec.Values();

            Assert.Equal(spec.Min, values.First());
            Assert.Equal(spec.Max, values.Last(), 4);
            for (var i = 1; i < values.Length; i++)
            {
                Assert.Equal(spec.Step, values[i] - values[i - 1], 4);
            }
            Assert.False(string.IsNullOrEmpty(name));
        }

        [Theory]
        [InlineData(OptionUnit.Seconds, 3f, "3 s")]
        [InlineData(OptionUnit.Seconds, 0.3f, "0.3 s")]
        [InlineData(OptionUnit.Seconds, 0.25f, "0.25 s")]
        [InlineData(OptionUnit.Percent, 1f, "100 %")]
        [InlineData(OptionUnit.Percent, 0.05f, "5 %")]
        [InlineData(OptionUnit.MetersPerSecond, 2.5f, "2.5 m/s")]
        [InlineData(OptionUnit.MetersPerSecond, 3f, "3 m/s")]
        [InlineData(OptionUnit.Count, 2f, "2")]
        [InlineData(OptionUnit.Plain, 0f, "0")]
        [InlineData(OptionUnit.Plain, 5f, "5")]
        public void Label(OptionUnit unit, float value, string expected)
        {
            var spec = new FloatOptionSpec("Test", 0f, 1f, 0.1f, 0f, unit);

            Assert.Equal(expected, spec.Label(value));
        }

        [Fact]
        public void Specs_have_unique_names()
        {
            var names = OptionSpecs.All.Select(s => s.Name).ToList();

            Assert.Equal(names.Count, names.Distinct().Count());
        }

        [Fact]
        public void Decided_defaults()
        {
            Assert.Equal(2.5f, OptionSpecs.MinSlamSpeed.Default);
            Assert.Equal(2f, OptionSpecs.SlamsToDisarm.Default);
            Assert.Equal(3f, OptionSpecs.SlamWindow.Default);
            Assert.Equal(0.3f, OptionSpecs.SlamCooldown.Default);
            Assert.Equal(1f, OptionSpecs.DisarmChance.Default);
            Assert.Equal(5f, OptionSpecs.NoRegrabTime.Default);
            Assert.Equal(0f, OptionSpecs.HandDamage.Default);
        }

        [Fact]
        public void Slams_to_disarm_are_whole_numbers()
        {
            Assert.All(OptionSpecs.SlamsToDisarm.Values(), v => Assert.Equal(v, (float)System.Math.Round(v)));
        }
    }
}
