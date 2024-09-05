using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CruiserImproved.Network;
using CruiserImproved.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace CruiserImproved.Patches;


[HarmonyPatch(typeof(Landmine))]
internal class LandminePatches
{
    //Injected method, return true if a hit should not deal knockback
    static bool ShouldNotDealKnockback(PlayerControllerB instance)
    {
        return NetworkSync.Config.PreventMissileKnockback && instance.inVehicleAnimation;
    }

    static MethodInfo get_magnitude = PatchUtils.Method(typeof(Vector3), "get_magnitude");
    [HarmonyPatch("SpawnExplosion")]
    [HarmonyTranspiler]

    static IEnumerable<CodeInstruction> SpawnExplosion_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        int findIndex = PatchUtils.LocateCodeSegment(0, codes, [ //locate the if statement checking for knockback sufficient to push player
            new(OpCodes.Ldloca_S, 16),
            new(OpCodes.Call, get_magnitude),
            new(OpCodes.Ldc_R4, 2),
            new(OpCodes.Ble_Un),
            new(OpCodes.Ldloca_S, 16),
            new(OpCodes.Call, get_magnitude),
            new(OpCodes.Ldc_R4, 10)
            ]);

        if(findIndex == -1)
        {
            CruiserImproved.LogError("Could not patch landmine knockback vehicle check!");
            return codes;
        }

        var jumpOperand = codes[findIndex + 3].operand;

        codes.InsertRange(findIndex + 4, [
            new CodeInstruction(OpCodes.Ldloc_S, 4),
            new CodeInstruction(OpCodes.Call, PatchUtils.Method(typeof(LandminePatches), "ShouldNotDealKnockback")), //add custom condition checking if player is seated
            new CodeInstruction(OpCodes.Brtrue_S, jumpOperand)
            ]);
        return codes;
    }
}
