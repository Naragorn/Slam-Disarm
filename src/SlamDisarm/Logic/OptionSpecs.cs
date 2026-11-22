using System;
using System.Collections.Generic;
using System.Globalization;

namespace SlamDisarm.Logic
{
    public enum OptionUnit
    {
        Seconds,

        /// <summary>Stored as 0..1, shown in percent.</summary>
        Percent,

        MetersPerSecond,

        /// <summary>A whole number of things (slams).</summary>
        Count,

        /// <summary>A bare number (damage points).</summary>
        Plain,
    }

    /// <summary>
    /// Range, step and default of a numeric Mod Menu option. Values are generated
    /// with decimal arithmetic so the default is bit-identical to one entry: the
    /// game's mod menu looks the default up with object.Equals on the boxed float
    /// (same builder as AchillesTendon's OptionSpecs).
    /// </summary>
    public sealed class FloatOptionSpec
    {
        public FloatOptionSpec(string name, float min, float max, float step, float defaultValue, OptionUnit unit)
        {
            Name = name;
            Min = min;
            Max = max;
            Step = step;
            Default = defaultValue;
            Unit = unit;
        }

        public string Name { get; }
        public float Min { get; }
        public float Max { get; }
        public float Step { get; }
        public float Default { get; }
        public OptionUnit Unit { get; }

        public float[] Values()
        {
            var min = (decimal)Min;
            var step = (decimal)Step;
            var count = (int)Math.Round(((decimal)Max - min) / step) + 1;
            var values = new float[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = (float)(min + i * step);
            }
            return values;
        }

        public string Label(float value)
        {
            var culture = CultureInfo.InvariantCulture;
            var number = (decimal)value;
            switch (Unit)
            {
                case OptionUnit.Percent:
                    return (number * 100m).ToString("0.#", culture) + " %";
                case OptionUnit.MetersPerSecond:
                    return number.ToString("0.#", culture) + " m/s";
                case OptionUnit.Count:
                    return number.ToString("0", culture);
                case OptionUnit.Plain:
                    return number.ToString("0.#", culture);
                default:
                    return number.ToString("0.##", culture) + " s";
            }
        }
    }

    /// <summary>Every numeric Mod Menu option of the mod. No magic numbers elsewhere.</summary>
    public static class OptionSpecs
    {
        // How hard the grabbed hand, forearm or weapon must hit: CollisionInstance.impactVelocity (m/s).
        // Default not measured yet; the game's own collision start needs the ragdoll's collisionMinVelocity.
        public static readonly FloatOptionSpec MinSlamSpeed =
            new FloatOptionSpec("Min Slam Speed", 0.5f, 8f, 0.5f, 2.5f, OptionUnit.MetersPerSecond);

        public static readonly FloatOptionSpec SlamsToDisarm =
            new FloatOptionSpec("Slams To Disarm", 1f, 5f, 1f, 2f, OptionUnit.Count);

        // Time allowed from one counted slam to the next before the count starts over.
        public static readonly FloatOptionSpec SlamWindow =
            new FloatOptionSpec("Slam Window", 1f, 10f, 0.5f, 3f, OptionUnit.Seconds);

        // One slam touches with several colliders; contacts closer than this to the last counted slam are the same slam.
        public static readonly FloatOptionSpec SlamCooldown =
            new FloatOptionSpec("Slam Cooldown", 0.1f, 1f, 0.05f, 0.3f, OptionUnit.Seconds);

        public static readonly FloatOptionSpec DisarmChance =
            new FloatOptionSpec("Disarm Chance", 0f, 1f, 0.05f, 1f, OptionUnit.Percent);

        public static readonly FloatOptionSpec NoRegrabTime =
            new FloatOptionSpec("No Regrab Time", 0f, 30f, 1f, 5f, OptionUnit.Seconds);

        // Health points per counted slam (Creature.Damage, blunt, no body part, push level 0).
        public static readonly FloatOptionSpec HandDamage =
            new FloatOptionSpec("Hand Damage", 0f, 20f, 1f, 0f, OptionUnit.Plain);

        public static readonly IReadOnlyList<FloatOptionSpec> All = new[]
        {
            MinSlamSpeed,
            SlamsToDisarm,
            SlamWindow,
            SlamCooldown,
            DisarmChance,
            NoRegrabTime,
            HandDamage,
        };
    }
}
