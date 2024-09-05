using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CruiserImproved.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace CruiserImproved.Patches.Targeted;

[HarmonyPatch]
internal class ItemFallthroughPatch
{
    public static MethodBase TargetMethod()
    {
        if (PatchUtils.TryMethod(typeof(GrabbableObject), "GetPhysicsRegionOfDroppedObject", [typeof(PlayerControllerB), typeof(Vector3).MakeByRefType()], out var info))
        {
            //v64 patch
            return info;
        }
        else if (PatchUtils.TryMethod(typeof(PlayerControllerB), "DiscardHeldObject", out info))
        {
            //v62 and earlier patch
            return info;
        }
        CruiserImproved.LogWarning("No valid method for ItemFallthroughPatch found.");
        return null;
    }

    public static PlayerPhysicsRegion FindPhysicsRegionOnTransform(ref Transform transform)
    {
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

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        int index = PatchUtils.LocateCodeSegment(0, codes, [
            new(OpCodes.Callvirt, PatchUtils.Method(typeof(Component), "GetComponentInChildren", null, [typeof(PlayerPhysicsRegion)])),
            ]);

        if (index != -1)
        {
            var loadInstruction = codes[index - 1];

            //this opcode changed variable number between versions, check both
            if (loadInstruction.opcode == OpCodes.Ldloc_0) codes[index - 1] = new(OpCodes.Ldloca, 0);
            if (loadInstruction.opcode == OpCodes.Ldloc_1) codes[index - 1] = new(OpCodes.Ldloca, 1);
            codes[index] = new(OpCodes.Call, PatchUtils.Method(typeof(ItemFallthroughPatch), "FindPhysicsRegionOnTransform"));

            return codes;
        }

        CruiserImproved.LogWarning("Could not patch ItemFallthroughPatch!");
        return instructions;
    }
}
