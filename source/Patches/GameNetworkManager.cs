using System;
using CruiserImproved.Utils;
using HarmonyLib;

namespace CruiserImproved.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
internal class GameNetworkManagerPatches
{
    [HarmonyPatch("SaveItemsInShip")]
    [HarmonyPostfix]
    static void SaveItemsInShip_Postfix(GameNetworkManager __instance)
    {
        //save cruiser data if we have one
        try
        {
            if (UserConfig.SaveCruiserValues.Value && StartOfRound.Instance.attachedVehicle)
            {
                VehicleController vehicle = StartOfRound.Instance.attachedVehicle;
                SaveManager.Save("AttachedVehicleRotation", vehicle.magnetTargetRotation.eulerAngles);
                SaveManager.Save("AttachedVehiclePosition", vehicle.magnetTargetPosition);
                SaveManager.Save("AttachedVehicleTurbo", vehicle.turboBoosts);
                SaveManager.Save("AttachedVehicleIgnition", vehicle.ignitionStarted);
                CruiserImproved.LogMessage("Successfully saved cruiser data.");
            }
            else
            {
                SaveManager.Delete("AttachedVehicleRotation");
                SaveManager.Delete("AttachedVehiclePosition");
                SaveManager.Delete("AttachedVehicleTurbo");
                SaveManager.Delete("AttachedVehicleIgnition");
            }
        }
        catch(Exception e)
        {
            CruiserImproved.LogError("Exception caught saving Cruiser data:\n" + e);
        }
    }
}
