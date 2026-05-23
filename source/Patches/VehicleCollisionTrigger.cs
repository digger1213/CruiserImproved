using CruiserImproved.Network;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CruiserImproved.Patches;

[HarmonyPatch(typeof(VehicleCollisionTrigger))]
internal class VehicleCollisionTriggerPatches
{
    [HarmonyPatch("OnTriggerEnter")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> VehicleCollisionTrigger_Trans_OnTriggerEnter(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();

        MethodInfo log = AccessTools.Method(typeof(Debug), nameof(Debug.Log), [typeof(object)]);
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldstr && (string)codes[i].operand == "Truck collision: {0} || {1} || {2}")
            {
                for (int j = i; j < codes.Count; j++)
                {
                    if (codes[j].opcode == OpCodes.Call && codes[j].operand as MethodInfo == log)
                    {
                        codes[j].opcode = OpCodes.Nop;
                        break;
                    }

                    codes[j].opcode = OpCodes.Nop;
                }
                CruiserImproved.LogDebug($"Transpiler (Cruiser collision): Resolve NRE by removing unnecessary log");
                return codes;
            }
        }

        CruiserImproved.LogWarning($"Cruiser collision transpiler failed");
        return codes;
    }

    [HarmonyPatch("OnTriggerEnter")]
    [HarmonyPrefix]
    static bool OnTriggerEnter_Prefix(VehicleCollisionTrigger __instance, Collider other)
    {
        if (__instance.mainScript.vehicleID != 0)
        {
            return true;
        }
        if (!__instance.mainScript.hasBeenSpawned)
        {
            return true;
        }
        if (__instance.mainScript.magnetedToShip && __instance.mainScript.magnetTime > 0.8f)
        {
            return true;
        }

        var extraData = VehicleControllerPatches.vehicleData[__instance.mainScript];

        PlayerControllerB player;
        //Patch hitting players standing on/in the cruiser
        if (other.CompareTag("Player") && (player = other.GetComponentInParent<PlayerControllerB>()))
        {
            Transform physicsTransform = __instance.mainScript.physicsRegion.physicsTransform;
            if(player.physicsParent == physicsTransform || player.overridePhysicsParent == physicsTransform)
            {
                return false;
            }
            return true;
        }
        EnemyAICollisionDetect enemyAI;
        if(other.CompareTag("Enemy") && (enemyAI = other.GetComponentInParent<EnemyAICollisionDetect>()))
        {
            if(!enemyAI.mainScript || !enemyAI.mainScript.agent || !enemyAI.mainScript.agent.navMeshOwner)
            {
                return true;
            }

            if (NetworkSync.Config.EntitiesAvoidCruiser)
            {
                MouthDogAI dog = enemyAI.mainScript as MouthDogAI;
                bool isAngryDog = dog && dog.suspicionLevel > 8;
                //prevent hits if the cruiser is blocking entity navigation and it's not an angry dog
                if(!isAngryDog && extraData.navObstacle.gameObject.activeSelf)
                {
                    return false;
                }
            }

            //Prevent hitting entities inside the truck
            Behaviour navmeshOn = (Behaviour)enemyAI.mainScript.agent.navMeshOwner;
            if (navmeshOn.transform.IsChildOf(__instance.mainScript.transform))
            {
                return false;
            }

            //Prevent hitting and bouncing off unkillable small entities (bees, ghost girl, earth leviathan). This matches vanilla behaviour with those entities and makes more sense
            if (!enemyAI.mainScript.enemyType.canDie && enemyAI.mainScript.enemyType.SizeLimit == NavSizeLimit.NoLimit) return false;

            return true;
        }
        return true;
    }
}
