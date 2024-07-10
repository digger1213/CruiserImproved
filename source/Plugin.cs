using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace CruiserImproved 
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
        //General
        internal static ConfigEntry<bool> AllowLean;
        internal static ConfigEntry<bool> PreventMissileKnockback;
        internal static ConfigEntry<bool> AllowPushDestroyedCar;
        internal static ConfigEntry<bool> PreventPassengersEjectingDriver;
        internal static ConfigEntry<bool> EntitiesAvoidCruiser;
        internal static ConfigEntry<float> SeatBoostScale;

        //Cruiser Health
        internal static ConfigEntry<float> CruiserInvulnerabilityDuration;
        internal static ConfigEntry<float> CruiserCriticalInvulnerabilityDuration;
        internal static ConfigEntry<int> MaxCriticalHitCount;

        //Physics
        internal static ConfigEntry<bool> AntiSideslip;
        internal static void InitConfig()
        {
            ConfigFile config = CruiserImproved.Instance.Config;

            AllowLean = config.Bind("General", "Allow Leaning", true, "If true, allow the player to look backward out the window or through the cabin window.");
            PreventMissileKnockback = config.Bind("General", "Prevent Missile Knockback", true, "If true, prevent the player being ejected from seats by Old Bird missile knockback.");
            AllowPushDestroyedCar = config.Bind("General", "Allow Pushing Destroyed Cruisers", true, "If true, allow players to push destroyed cruisers.");
            EntitiesAvoidCruiser = config.Bind("General", "Entities Avoid Cruiser", true, "If true, entities will pathfind around stationary cruisers with no driver.\nEyeless dogs will still attack it if they hear noise!");
            PreventPassengersEjectingDriver = config.Bind("General", "Prevent Passengers Eject Driver", false, "If true, prevent anyone except the driver of the cruiser from using the eject button.");

            AcceptableValueRange<float> seatScale = new(0f, 1f);
            SeatBoostScale = config.Bind("General", "Seat Boost Scale", 1.0f, new ConfigDescription("How much to boost the seat up? Set 0 to disable.", seatScale));

            AcceptableValueRange<float> invulnerableDuration = new(0f, 2f);
            CruiserInvulnerabilityDuration = config.Bind("Cruiser Health", "Cruiser Invulnerability Duration", 0.5f, new ConfigDescription("How long after taking damage is the Cruiser invulnerable for? Set 0 to disable.", invulnerableDuration));

            AcceptableValueRange<float> criticalInvulnerableDuration = new(0f, 6f);
            CruiserCriticalInvulnerabilityDuration = config.Bind("Cruiser Health", "Cruiser Critical Invulnerability Duration", 4.0f, new ConfigDescription("How long after critical damage (engine on fire) is the Cruiser invulnerable for? Set 0 to disable.", criticalInvulnerableDuration));


            AcceptableValueRange<int> criticalHitCount = new(0, 100);
            MaxCriticalHitCount = config.Bind("Cruiser Health", "Critical Protection Hit Count", 1, new ConfigDescription("Number of hits the Cruiser can block during the Critical Invulnerability Duration. \nIf the Cruiser receives this many hits while critical, it will emit a sound cue before exploding once the duration is up.\nIf 0, any hit that triggers the critical state will also trigger this delayed explosion.", criticalHitCount));

            AntiSideslip = config.Bind("Physics", "Anti-Sideslip", true, "If true, prevent the Cruiser from sliding sideways when on slopes.");
        }
    }
}