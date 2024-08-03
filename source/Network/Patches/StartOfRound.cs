using HarmonyLib;

namespace CruiserImproved.Network.Patches;

[HarmonyPatch(typeof(StartOfRound))]
internal class StartOfRoundPatches
{
    [HarmonyPatch("SyncAlreadyHeldObjectsServerRpc")]
    [HarmonyPostfix]
    static void SyncAlreadyHeldObjectsServerRpc(int joiningClientId)
    {
        NetworkSync.SendClientSyncRpcs((ulong)joiningClientId);
    }
}
