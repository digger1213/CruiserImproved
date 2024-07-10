using GameNetcodeStuff;
using HarmonyLib;
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

            var extraData = VehicleControllerPatches.vehicleData[__instance.mainScript];

            PlayerControllerB player;
            //Patch hitting players standing on/in the cruiser
            if (other.CompareTag("Player") && (player = other.GetComponentInParent<PlayerControllerB>()))
            {
                if(__instance.mainScript.physicsRegion.physicsTransform == player.physicsParent)
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

                if (UserConfig.EntitiesAvoidCruiser.Value)
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
                return true;
            }
            return true;
        }
    }
}
