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
            harmony.PatchAll();
        }

        public void BindConfig<T>(ref ConfigEntry<T> config, string section, string key, T defaultValue, string description = "")
        {
            config = Config.Bind(section, key, defaultValue, description);
        }
    }

    internal class UserConfig
    {
    }
}