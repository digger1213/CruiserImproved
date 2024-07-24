using System;
using System.Collections.Generic;
using System.Text;
using CruiserImproved.Patches;
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
