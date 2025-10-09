using CruiserImproved.Network;
using CruiserImproved.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace CruiserImproved.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
internal class PlayerControllerPatches
{
    private static bool usingSeatCam = false;

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    public static void Update_Postfix(PlayerControllerB __instance)
    {
        if (LCVRCompatibility.inVrSession) return;

        if (__instance != GameNetworkManager.Instance.localPlayerController) return;

        bool cameraSettingsEnabled = NetworkSync.Config.AllowLean || NetworkSync.Config.SeatBoostScale > 0f;
        if (!cameraSettingsEnabled) return;

        Vector3 cameraOffset = Vector3.zero;

        //check we're in the vanilla Cruiser before modifying camera
        bool validCruiser = __instance.inVehicleAnimation && __instance.currentTriggerInAnimationWith && __instance.currentTriggerInAnimationWith.overridePlayerParent;
        if (validCruiser && __instance.currentTriggerInAnimationWith.overridePlayerParent.TryGetComponent<VehicleController>(out var controller) && PublicVehicleData.VehicleID == 0)
        {
            usingSeatCam = true;
            //If we're in a car, boost the camera upward slightly for better visibility
            cameraOffset = new Vector3(0f, 0.25f, -0.05f) * NetworkSync.Config.SeatBoostScale;
            Vector3 lookFlat = __instance.gameplayCamera.transform.localRotation * Vector3.forward;
            lookFlat.y = 0;
            float angleToBack = Vector3.Angle(lookFlat, Vector3.back);
            if(angleToBack < 70 && NetworkSync.Config.AllowLean)
            {
                //If we're looking backwards, offset the camera to the side ('leaning')
                cameraOffset.x = Mathf.Sign(lookFlat.x) * ((70f - angleToBack)/70f);
            }
            __instance.gameplayCamera.transform.localPosition = cameraOffset;
        }
        else if (!__instance.inVehicleAnimation && usingSeatCam == true)
        {
            //If player is not in the cruiser, reset the camera once
            usingSeatCam = false;
            __instance.gameplayCamera.transform.localPosition = Vector3.zero;
        }
    }

    [HarmonyPatch("PlaceGrabbableObject")]
    [HarmonyPostfix]
    static void PlaceGrabbableObject_Postfix(GrabbableObject placeObject)
    {
        ScanNodeProperties scanNode = placeObject.GetComponentInChildren<ScanNodeProperties>();

        //add rigidbody to the scanNode so it'll be scannable when attached to the cruiser
        if (scanNode && !scanNode.GetComponent<Rigidbody>())
        {
            var rb = scanNode.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
    }

    [HarmonyPatch("SetHoverTipAndCurrentInteractTrigger")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> SetHoverTipAndCurrentInteractTrigger_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        //Fix untagged interactables from carrying the last valid interactable the player looked at (caused the Cabin Light interact prompt to carry over the whole WindshieldInteractBlocker)
        var codes = new List<CodeInstruction>(instructions);

        var get_layer = PatchUtils.Method(typeof(GameObject), "get_layer");

        int insertIndex = PatchUtils.LocateCodeSegment(0, codes, [
            new(OpCodes.Callvirt, get_layer),
            new(OpCodes.Ldc_I4_S, 0x1E),
            new(OpCodes.Beq),
            ]);

        if(insertIndex == -1)
        {
            CruiserImproved.LogWarning("Could not transpile SetHoverTipAndCurrentInteractTrigger!");
            return codes;
        }

        var jumpDestination = codes[insertIndex + 2].operand;

        insertIndex += 3; //after the searched sequence

        var insertMethod = PatchUtils.Method(typeof(PlayerControllerPatches), nameof(ValidRayHit));

        /*
         *  IL Code (adding new condition to the end of the existing if statement beginning with Physics.Raycast):
         *  
         *  && PlayerControllerPatches.ValidRayHit(this)
         */

        codes.InsertRange(insertIndex, [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Call, insertMethod),
            new(OpCodes.Brfalse, jumpDestination)
            ]);

        return codes;
    }

    static bool ValidRayHit(PlayerControllerB player)
    {
        //Return true if the looked at object is a valid interactable
        string tag = player.hit.collider.tag;
        return tag == "PhysicsProp" || tag == "InteractTrigger";
    }
}
