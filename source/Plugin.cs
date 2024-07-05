using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib.Tools;

namespace DiggCruiserImproved 
{
    [BepInPlugin(modGUID, "CompanyCruiserImproved", modVersion)]
    internal class CruiserImproved : BaseUnityPlugin
    {
        internal const string modGUID = "DiggC.CompanyCruiserImproved";
        internal const string modVersion = "1.0.0";

        private Harmony harmony;

        static public CruiserImproved Instance;
        static public ManualLogSource Log;

        public void Awake()
        {
            Instance = this;
            Log = Logger;
            harmony = new Harmony(modGUID);
            UserConfig.InitConfig();
            harmony.PatchAll();
        }

        public void BindConfig<T>(ref ConfigEntry<T> config, string section, string key, T defaultValue, string description = "")
        {
            config = Config.Bind(section, key, defaultValue, description);
        }
    }

    internal class UserConfig
    {
        internal static ConfigEntry<bool> AllowLean;
        internal static ConfigEntry<bool> PreventMissileKnockback;
        internal static ConfigEntry<float> SeatBoostScale;
        internal static ConfigEntry<float> CruiserInvulnerabilityDuration;
        internal static ConfigEntry<float> CruiserCriticalInvulnerabilityDuration;
        internal static void InitConfig()
        {
            ConfigFile config = CruiserImproved.Instance.Config;
            AllowLean = config.Bind("Settings", "Allow Leaning", true, "If true, allow the player to look backward out the window or through the cabin window.");
            PreventMissileKnockback = config.Bind("Settings", "Prevent Missile Knockback", true, "If true, prevent the player being ejected from seats by Old Bird missile knockback.");

            AcceptableValueRange<float> seatScale = new(0f, 1f);
            SeatBoostScale = config.Bind("Settings", "Seat Boost Scale", 1.0f, new ConfigDescription("How much to boost the seat up? Set 0 to disable.", seatScale));

            AcceptableValueRange<float> invulnerableDuration = new(0f, 2f);
            CruiserInvulnerabilityDuration = config.Bind("Settings", "Cruiser Invulnerability Duration", 1.0f, new ConfigDescription("How long after taking damage is the Cruiser invulnerable for? Set 0 to disable.", invulnerableDuration));

            AcceptableValueRange<float> criticalInvulnerableDuration = new(0f, 4f);
            CruiserCriticalInvulnerabilityDuration = config.Bind("Settings", "Cruiser Critical Invulnerability Duration", 3.0f, new ConfigDescription("How long after critical damage (engine on fire) is the Cruiser invulnerable for? Set 0 to disable.", criticalInvulnerableDuration));
        }
    }
}