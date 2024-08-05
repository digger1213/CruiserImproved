using BepInEx;
using HarmonyLib;
using BepInEx.Logging;
using System;
using CruiserImproved.Utils;
using System.Diagnostics;

namespace CruiserImproved;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("io.daxcess.lcvr", BepInDependency.DependencyFlags.SoftDependency)]
internal class CruiserImproved : BaseUnityPlugin
{
    static public Version Version = new(MyPluginInfo.PLUGIN_VERSION);

    static public CruiserImproved Instance;

    private Harmony harmony;
    static private ManualLogSource Log;

    public static void LogError(object data) => Log.LogError(data);

    public static void LogWarning(object data) => Log.LogWarning(data);

    public static void LogMessage(object data) => Log.LogMessage(data);

    public static void LogInfo(object data) => Log.LogInfo(data);

    [Conditional("DEBUG")]
    public static void LogDebug(object data) => Log.LogDebug(data);

    public void Awake()
    {
        Instance = this;
        Log = Logger;
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        UserConfig.InitConfig();
        harmony.PatchAll();
    }
}