using GameNetcodeStuff;
using HarmonyLib;
using System.ComponentModel;
using UnityEngine;

namespace CruiserImproved.Patches
{
    [HarmonyPatch(typeof(VehicleCollisionTrigger))]
    internal class VehicleCollisionTriggerPatches
    {
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPrefix]
        static bool OnTriggerEnter_Prefix(VehicleCollisionTrigger __instance, Collider other)
        {
            if (!__instance.mainScript.hasBeenSpawned)
            {
                return true;
            }
            if (__instance.mainScript.magnetedToShip && __instance.mainScript.magnetTime > 0.8f)
            {
                return true;
            }

            PlayerControllerB player;
            //Patch hitting players standing on/in the cruiser
            if (player = other.GetComponentInParent<PlayerControllerB>())
            {
                if(__instance.mainScript.physicsRegion.physicsTransform == player.physicsParent)
                {
                    return false;
                }
                return true;
            }
            EnemyAICollisionDetect enemyAI;
            if(enemyAI = other.GetComponentInParent<EnemyAICollisionDetect>())
            {
                if(!enemyAI.mainScript || !enemyAI.mainScript.agent || !enemyAI.mainScript.agent.navMeshOwner)
                {
                    return true;
                }
                //Prevent hitting entities inside the truck
                Behaviour navmeshOn = (Behaviour)enemyAI.mainScript.agent.navMeshOwner;
                Vector3 destination = enemyAI.mainScript.agent.destination;
                if (navmeshOn.transform.IsChildOf(__instance.mainScript.transform))
                {
                    return false;
                }
                return true;
            }
            return true;
        }
    }
}
