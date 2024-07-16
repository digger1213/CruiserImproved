using BepInEx;
using HarmonyLib;
using BepInEx.Logging;

namespace CruiserImproved; 

[BepInPlugin(modGUID, modName, modVersion)]
internal class CruiserImproved : BaseUnityPlugin
{
    internal const string modGUID = MyPluginInfo.PLUGIN_GUID;
    internal const string modName = MyPluginInfo.PLUGIN_NAME;
    internal const string modVersion = MyPluginInfo.PLUGIN_VERSION;

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
}