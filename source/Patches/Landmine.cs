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

    static MethodInfo get_magnitude = typeof(Vector3).GetMethod("get_magnitude", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
    [HarmonyPatch("SpawnExplosion")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> SpawnExplosion_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        int findIndex = PatchUtils.LocateCodeSegment(0, codes, [
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

        List<CodeInstruction> newCodes = [
            new CodeInstruction(OpCodes.Ldloc_S, 4),
            new CodeInstruction(OpCodes.Call, typeof(LandminePatches).GetMethod("ShouldNotDealKnockback", BindingFlags.Static | BindingFlags.NonPublic)),
            new CodeInstruction(OpCodes.Brtrue_S, jumpOperand)
            ];

        codes.InsertRange(findIndex + 4, newCodes);
        return codes;
    }
}
