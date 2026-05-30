using CruiserImproved.Network;
using HarmonyLib;

namespace CruiserImproved.Patches;

[HarmonyPatch(typeof(HUDManager))]
internal class HUDManagerPatches
{
    [HarmonyPatch("CanPlayerScan")]
    [HarmonyPostfix]
    static void CanPlayerScan_Postfix(ref bool __result)
    {
        if (!NetworkSync.Config.ScanWhileSeated || __result) return;

        //override to allow scan while seated
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        bool validCruiser = localPlayer.inVehicleAnimation && localPlayer.currentTriggerInAnimationWith && localPlayer.currentTriggerInAnimationWith.overridePlayerParent;
        if (validCruiser && !localPlayer.isPlayerDead && localPlayer.currentTriggerInAnimationWith.overridePlayerParent.TryGetComponent<VehicleController>(out var controller) && controller.vehicleID == 0)
            __result = true;
    }
}
