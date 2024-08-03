using CruiserImproved.Network;
using CruiserImproved.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;

namespace CruiserImproved.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
internal class PlayerControllerPatches
{
    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    public static void Update_Postfix(PlayerControllerB __instance)
    {
        if (LCVRCompatibility.inVrSession) return;

        bool cameraSettingsEnabled = NetworkSync.Config.AllowLean || NetworkSync.Config.SeatBoostScale > 0f;
        if (!cameraSettingsEnabled) return;

        Vector3 cameraOffset = Vector3.zero;
        if (__instance.inVehicleAnimation)
        {
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
        }

        __instance.gameplayCamera.transform.localPosition = cameraOffset;
    }

    public static PlayerPhysicsRegion FindPhysicsRegionOnTransform(ref Transform transform)
    {
        CruiserImproved.LogMessage("Transform name: " + transform.name);

        //vanilla try get region in children first
        PlayerPhysicsRegion region = transform.GetComponentInChildren<PlayerPhysicsRegion>();
        if (region) return region;

        //try find a vehicle in parents, return that vehicle's physics region
        VehicleController parentVehicle = transform.GetComponentInParent<VehicleController>();
        if (parentVehicle)
        {
            transform = parentVehicle.transform;
            return parentVehicle.physicsRegion;
        }

        return null;
    }

    [HarmonyPatch("DiscardHeldObject")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> DiscardHeldObject_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        int index = PatchUtils.LocateCodeSegment(0, codes, [
            //locate GetComponentInChildren<PlayerPhysicsRegion> call
            new(OpCodes.Ldloc_0),
            new(OpCodes.Callvirt),
            new(OpCodes.Stloc_2),
            new(OpCodes.Ldloc_2),
            ]);

        if(index == -1)
        {
            CruiserImproved.LogError("Could not patch DiscardHeldObject!");
            return codes;
        }

        //replace with custom method to find a physics region
        codes[index] = new(OpCodes.Ldloca, 0);
        codes[index + 1] = new(OpCodes.Call, typeof(PlayerControllerPatches).GetMethod("FindPhysicsRegionOnTransform", BindingFlags.Public | BindingFlags.Static));

        return codes;
    }
}
