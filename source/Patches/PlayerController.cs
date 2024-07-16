using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace CruiserImproved.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
internal class PlayerControllerPatches
{
    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    public static void Update_Postfix(PlayerControllerB __instance)
    {
        Vector3 cameraOffset = Vector3.zero;
        if (__instance.inVehicleAnimation)
        {
            //If we're in a car, boost the camera upward slightly for better visibility
            cameraOffset = new Vector3(0f, 0.25f, -0.05f) * UserConfig.SeatBoostScale.Value;
            Vector3 lookFlat = __instance.gameplayCamera.transform.localRotation * Vector3.forward;
            lookFlat.y = 0;
            float angleToBack = Vector3.Angle(lookFlat, Vector3.back);
            if(angleToBack < 70 && UserConfig.AllowLean.Value)
            {
                //If we're looking backwards, offset the camera to the side ('leaning')
                cameraOffset.x = Mathf.Sign(lookFlat.x) * ((70f - angleToBack)/70f);
            }
        }

        __instance.gameplayCamera.transform.localPosition = cameraOffset;
    }
}
