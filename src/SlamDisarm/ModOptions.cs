using SlamDisarm.Logic;
using ThunderRoad;

namespace SlamDisarm
{
    /// <summary>
    /// Every setting of the mod, shown in the game's Mod Menu.
    ///
    /// How the game handles these (read in ThunderRoad.dll 1.3.2, same as AchillesTendon):
    /// - ModManager scans all static members with [ModOption] in this assembly.
    /// - The initial field value is the default; for floats the value list comes
    ///   from <see cref="OptionSpecs"/> so the default matches exactly.
    /// - A change in the menu writes the static field immediately (live), and the
    ///   chosen index is saved by the game per mod folder (persistent).
    /// - Option names must be unique per mod (saved by name); the game's mod page
    ///   has its own reset button, so the mod adds no reset option.
    /// </summary>
    public static class ModOptions
    {
        private const string SlamCategory = "Slam Disarm";
        private const string DebugCategory = "Debug";

        [ModOption("Slam Disarm Enabled", "Turns the mod on or off. Off: slams do nothing and disarmed enemies may pick their weapons up again.")]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(0)]
        public static bool Enabled = true;

        [ModOption("Min Slam Speed", "How hard you must slam their hand or weapon into something for it to count.", nameof(MinSlamSpeedValues))]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        public static float MinSlamSpeed = OptionSpecs.MinSlamSpeed.Default;

        [ModOption("Slams To Disarm", "How many slams it takes before they let go.", nameof(SlamsToDisarmValues))]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        public static float SlamsToDisarm = OptionSpecs.SlamsToDisarm.Default;

        [ModOption("Slam Window", "Time you have for the next slam. Wait longer and the count starts over.", nameof(SlamWindowValues))]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        public static float SlamWindow = OptionSpecs.SlamWindow.Default;

        [ModOption("Slam Cooldown", "Bumps closer together than this count as one slam.", nameof(SlamCooldownValues))]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        public static float SlamCooldown = OptionSpecs.SlamCooldown.Default;

        [ModOption("Disarm Chance", "Chance that the last slam makes them let go. If it fails, the next slam tries again.", nameof(DisarmChanceValues))]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        public static float DisarmChance = OptionSpecs.DisarmChance.Default;

        [ModOption("Objects Count", "Slams against loose objects count too, not only walls and the floor.")]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(6)]
        public static bool ObjectsCount = true;

        [ModOption("Forearm Grab Counts", "Holding their forearm works too, not only their hand.")]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(7)]
        public static bool ForearmGrabCounts = true;

        [ModOption("Weapon Grab Counts", "Holding their weapon while they still hold it works too.")]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(8)]
        public static bool WeaponGrabCounts = true;

        [ModOption("Shields Can Be Slammed", "Shields can be slammed out of their hand too.")]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(9)]
        public static bool ShieldsCanBeSlammed = false;

        [ModOption("Drop Both Hands", "They drop what is in their other hand as well.")]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(10)]
        public static bool DropBothHands = false;

        [ModOption("No Regrab Time", "How long a disarmed enemy cannot pick up or draw a weapon. 0 = no wait.", nameof(NoRegrabTimeValues))]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(11)]
        [ModOptionSlider]
        public static float NoRegrabTime = OptionSpecs.NoRegrabTime.Default;

        [ModOption("Pain Sound", "They cry out when a slam loosens their grip.")]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(12)]
        public static bool PainSound = true;

        [ModOption("Haptic Pulse", "Your hand buzzes when a slam loosens their grip.")]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(13)]
        public static bool HapticPulse = true;

        [ModOption("Hand Damage", "Damage each slam deals to them. 0 = none.", nameof(HandDamageValues))]
        [ModOptionCategory(SlamCategory, 0)]
        [ModOptionOrder(14)]
        [ModOptionSlider]
        public static float HandDamage = OptionSpecs.HandDamage.Default;

        [ModOption("Debug Logging", "Writes every slam and disarm to the game log, for bug reports.")]
        [ModOptionCategory(DebugCategory, 1)]
        [ModOptionOrder(0)]
        public static bool DebugLogging = false;

        public static ModOptionParameter[] MinSlamSpeedValues() => ToParameters(OptionSpecs.MinSlamSpeed);

        public static ModOptionParameter[] SlamsToDisarmValues() => ToParameters(OptionSpecs.SlamsToDisarm);

        public static ModOptionParameter[] SlamWindowValues() => ToParameters(OptionSpecs.SlamWindow);

        public static ModOptionParameter[] SlamCooldownValues() => ToParameters(OptionSpecs.SlamCooldown);

        public static ModOptionParameter[] DisarmChanceValues() => ToParameters(OptionSpecs.DisarmChance);

        public static ModOptionParameter[] NoRegrabTimeValues() => ToParameters(OptionSpecs.NoRegrabTime);

        public static ModOptionParameter[] HandDamageValues() => ToParameters(OptionSpecs.HandDamage);

        /// <summary>The menu values as the pure logic reads them.</summary>
        public static SlamSettings SlamSettings()
        {
            return new SlamSettings
            {
                ObjectsCount = ObjectsCount,
                MinSlamSpeed = MinSlamSpeed,
                SlamCooldown = SlamCooldown,
                SlamsToDisarm = (int)System.Math.Round(SlamsToDisarm),
                SlamWindow = SlamWindow,
                DisarmChance = DisarmChance,
            };
        }

        private static ModOptionParameter[] ToParameters(FloatOptionSpec spec)
        {
            var values = spec.Values();
            var parameters = new ModOptionParameter[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                parameters[i] = new ModOptionFloat(spec.Label(values[i]), values[i]);
            }
            return parameters;
        }
    }
}
