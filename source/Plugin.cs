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

    public static void LogError(object data) => Instance.Logger.LogError(data);

    public static void LogWarning(object data) => Instance.Logger.LogWarning(data);

    public static void LogMessage(object data) => Instance.Logger.LogMessage(data);

    public static void LogInfo(object data) => Instance.Logger.LogInfo(data);

    [Conditional("DEBUG")]
    public static void LogDebug(object data) => Instance.Logger.LogDebug(data);

    public void Awake()
    {
        Instance = this;
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        UserConfig.InitConfig();
        harmony.PatchAll();
    }
}