using HarmonyLib;
using System.Diagnostics;
using Unity.Netcode;

namespace CruiserImproved.Network.Patches;

[HarmonyPatch(typeof(RoundManager))]
internal static class RoundManagerPatches
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    static void Awake_Postfix()
    {
        NetworkSync.Init();
    }

    [HarmonyPatch("OnDestroy")]
    [HarmonyPostfix]
    static void Destroy_Postfix()
    {
        NetworkSync.Cleanup();
    }
}
