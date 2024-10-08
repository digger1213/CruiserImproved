﻿using GameNetcodeStuff;
using HarmonyLib;

namespace CruiserImproved.Patches;

[HarmonyPatch(typeof(ElevatorAnimationEvents))]
internal class ElevatorAnimationEventsPatches
{
    [HarmonyPatch("ElevatorFullyRunning")]
    [HarmonyPrefix]
    static void ElevatorFullyRunning_Prefix()
    {
        //Save players who are on the magneted cruiser from being abandoned
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;

        if (localPlayer.physicsParent == null) return;

        VehicleController vehicle = localPlayer.physicsParent.GetComponentInParent<VehicleController>();
        if (vehicle && vehicle.magnetedToShip)
        {
            localPlayer.isInElevator = true;
        }
    }
}
