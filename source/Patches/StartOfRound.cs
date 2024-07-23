﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

namespace CruiserImproved.Patches;

[HarmonyPatch(typeof(StartOfRound))]
internal class StartOfRoundPatches
{
    //injected sorting method
    static void SetItemPosition(StartOfRound instance, int index, Vector3[] positionArray, int[] itemArray)
    {
        if (!UserConfig.SortEquipmentOnLoad.Value) return;

        //try catch block here in case code shuffles variables and this throws (if it throws it'll break the whole loading sequence)
        try
        {
            CruiserImproved.Log.LogMessage("SetItemPosition called with instance: " + (instance != null) + " index: " + index + " pos array: " + (positionArray != null) + " item array: " + (itemArray != null));
            Item thisItem = instance.allItemsList.itemsList[itemArray[index]];
            //move non-scrap and weapons toward the center of the ship slightly from the rest of the pile
            if (!thisItem.isScrap || thisItem.isDefensiveWeapon)
            {
                positionArray[index].z += Random.Range(-2.5f, -1.5f);
            }
        }
        catch(System.Exception e)
        {
            CruiserImproved.Log.LogError("Error while placing Cruiser items in ship:\n" + e);
        }
    }

    [HarmonyPatch("LoadShipGrabbableItems")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> LoadShipGrabbableItems_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        int index = PatchUtils.LocateCodeSegment(0, codes, [
            new(OpCodes.Ldfld, typeof(StartOfRound).GetField("shipBounds"))
            ]);

        if(index != -1)
        {
            //fix items floating by using the inner room bounds instead (shipBounds is too large and keeps items floating near ship walls)
            codes[index].operand = typeof(StartOfRound).GetField("shipInnerRoomBounds");
        }
        else
        {
            index = 0;
            CruiserImproved.Log.LogError("Could not patch LoadShipGrabbableItems bounds!");
        }

        index = PatchUtils.LocateCodeSegment(index, codes, [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, typeof(StartOfRound).GetField("allItemsList")),
            new(OpCodes.Ldfld, typeof(AllItemsList).GetField("itemsList"))
            ]);

        if(index != -1)
        {
            //Insert method call to SetItemPosition to apply sorting
            codes.InsertRange(index, [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldloc_S, 9),
                new(OpCodes.Ldloc_2),
                new(OpCodes.Ldloc_1),
                new(OpCodes.Call, typeof(StartOfRoundPatches).GetMethod("SetItemPosition", BindingFlags.NonPublic | BindingFlags.Static))
                ]);
        }
        else
        {
            CruiserImproved.Log.LogError("Could not patch LoadShipGrabbableItems sorting!");
        }

        return codes;
    }
}
