using System;
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
            if (StartOfRound.Instance && StartOfRound.Instance.attachedVehicle)
            {
                VehicleController vehicle = StartOfRound.Instance.attachedVehicle;
                SaveManager.Save("AttachedVehicleRotation", vehicle.magnetTargetRotation.eulerAngles);
                SaveManager.Save("AttachedVehiclePosition", vehicle.magnetTargetPosition);
                SaveManager.Save("AttachedVehicleTurbo", vehicle.turboBoosts);
                SaveManager.Save("AttachedVehicleIgnition", vehicle.ignitionStarted);
                CruiserImproved.Log.LogMessage("Successfully saved cruiser data.");
            }
        }
        catch(Exception e)
        {
            CruiserImproved.Log.LogError("Caught error saving Cruiser data:\n" + e);
        }
    }
}
