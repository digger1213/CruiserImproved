using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System;

namespace CruiserImproved; 

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class CruiserImproved : BaseUnityPlugin
{
    static public Version Version = new(MyPluginInfo.PLUGIN_VERSION);

    private Harmony harmony;

    static public CruiserImproved Instance;
    static public ManualLogSource Log;

    public void Awake()
    {
        Instance = this;
        Log = Logger;
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        UserConfig.InitConfig();
        harmony.PatchAll();
    }
}