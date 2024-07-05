using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace DiggCruiserImproved.Patches
{
    [HarmonyPatch(typeof(VehicleController))]
    internal class VehicleControllerPatches
    {
        static int CriticalThreshold = 2;

        class VehicleControllerData
        {
            public int lastDamageReceived;
            public float timeLastDamaged;
            public float timeLastCriticalDamage;
        }

        static Dictionary<VehicleController, VehicleControllerData> vehicleData = new();

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void Awake_Postfix(VehicleController __instance)
        {
            vehicleData.Add(__instance, new());

            List<VehicleController> vehiclesToRemove = new();
            foreach (VehicleController vehicle in vehicleData.Keys)
            {
                if (!vehicle)
                {
                    vehiclesToRemove.Add(vehicle);
                }
            }

            foreach (VehicleController vehicle in vehiclesToRemove)
            {
                vehicleData.Remove(vehicle);
            }

            //Allow player to turn further backward for the lean mechanic
            if (UserConfig.AllowLean.Value)
            {
                __instance.driverSeatTrigger.horizontalClamp = 163f;
                __instance.passengerSeatTrigger.horizontalClamp = 163f;
            }
        }

        [HarmonyPatch("DealPermanentDamage")]
        [HarmonyPrefix]
        private static void DealPermanentDamage_Prefix(VehicleController __instance, ref int damageAmount, Vector3 damagePosition)
        {
            if (StartOfRound.Instance.testRoom == null && StartOfRound.Instance.inShipPhase)
            {
                return;
            }
            if (__instance.carDestroyed)
            {
                return;
            }
            if (!__instance.IsOwner)
            {
                return;
            }

            int originalDamage = damageAmount;
            VehicleControllerData extraData = vehicleData[__instance];

            float timeSinceDamage = Time.realtimeSinceStartup - extraData.timeLastDamaged;
            float timeSinceCriticallyDamaged = Time.realtimeSinceStartup - extraData.timeLastCriticalDamage;

            float invulnDuration = UserConfig.CruiserInvulnerabilityDuration.Value;
            float critInvulnDuration = UserConfig.CruiserCriticalInvulnerabilityDuration.Value;

            bool isInvulnerable = timeSinceDamage < invulnDuration;
            bool isCritInvulnerable = timeSinceCriticallyDamaged < critInvulnDuration;

            //Prevent damage less than what we last received if within I-frame duration
            if (isInvulnerable && extraData.lastDamageReceived >= damageAmount)
            {
                CruiserImproved.Log.LogMessage($"Vehicle ignored {damageAmount} damage due to I-frames from previous damage {extraData.lastDamageReceived} ({Math.Round(timeSinceDamage, 2)}s)");
                damageAmount = 0;
                return;
            }

            //If this damage is higher than the last damage we received and we're still in I-frames, allow the extra damage through (so a small hit can't block for a bigger one)
            if (isInvulnerable && extraData.lastDamageReceived > 0)
            {
                damageAmount -= extraData.lastDamageReceived;
                CruiserImproved.Log.LogMessage($"Vehicle reduced {originalDamage} to {damageAmount} due to I-frames from previous damage {extraData.lastDamageReceived} ({Math.Round(timeSinceDamage, 2)}s)");
            }

            //Don't grant extra I-frames if we're still invulnerable from crit
            if (!isCritInvulnerable)
            {
                extraData.lastDamageReceived = originalDamage;
                extraData.timeLastDamaged = Time.realtimeSinceStartup;
            }

            if (__instance.carHP - damageAmount <= CriticalThreshold)
            {
                float beforeCritDamage = damageAmount;
                bool activatedCritThisDamage = false;

                //Only grant crit I-frames if we were not crit before this damage
                if (__instance.carHP > CriticalThreshold)
                {
                    activatedCritThisDamage = true;
                    extraData.timeLastCriticalDamage = Time.realtimeSinceStartup;
                    timeSinceCriticallyDamaged = 0.0f;
                }

                //Prevent car from dropping below 1HP if we have crit I-frames
                if (timeSinceCriticallyDamaged < critInvulnDuration)
                {
                    damageAmount = Mathf.Min(damageAmount, __instance.carHP - 1);
                    if(activatedCritThisDamage)
                    {
                        CruiserImproved.Log.LogMessage($"Critical protection triggered for {critInvulnDuration}s due to {damageAmount} vehicle damage");

                    }
                    else
                    {
                        CruiserImproved.Log.LogMessage($"Critical protection reduced vehicle damage from {beforeCritDamage} to {damageAmount}");
                    }
                }
            }
        }

        [HarmonyPatch("DealDamageClientRpc")]
        [HarmonyPostfix]
        static void DealDamageClientRpc_Postfix(VehicleController __instance, int amount, int sentByClient)
        {
            //Keep track of damage sent by other clients and update local invincibility stats in case of ownership switch
            if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId == sentByClient)
            {
                return;
            }

            if (amount <= 0) return;

            VehicleControllerData extraData = vehicleData[__instance];

            float timeSinceDamage = Time.realtimeSinceStartup - extraData.timeLastDamaged;
            float timeSinceCriticallyDamaged = Time.realtimeSinceStartup - extraData.timeLastCriticalDamage;

            bool isInvulnerable = timeSinceDamage < UserConfig.CruiserInvulnerabilityDuration.Value;
            bool isCritInvulnerable = timeSinceCriticallyDamaged < UserConfig.CruiserCriticalInvulnerabilityDuration.Value;

            if (isCritInvulnerable) return;

            extraData.timeLastDamaged = Time.realtimeSinceStartup;
            if (isInvulnerable)
            {
                extraData.lastDamageReceived += amount;
            }
            else
            {
                extraData.lastDamageReceived = amount;
            }

            if (__instance.carHP <= CriticalThreshold && __instance.carHP + amount > CriticalThreshold)
            {
                CruiserImproved.Log.LogMessage("Received critical car damage from network");
                extraData.timeLastCriticalDamage = Time.realtimeSinceStartup;
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //Remove the code section that stops the update function from running if we're not in control of the truck, so the truck can still update the wheels
            var codes = new List<CodeInstruction>(instructions);

            int searchTargetIndex = PatchUtils.LocateCodeSegment(0, codes, [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(VehicleController).GetField("localPlayerInControl", BindingFlags.Instance | BindingFlags.Public)),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(NetworkBehaviour).GetMethod("get_IsOwner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty)),
                new(OpCodes.Brtrue)
                ]);

            if(searchTargetIndex == -1)
            {
                CruiserImproved.Log.LogError("Could not transpile VehicleController.Update");
                return codes;
            }

            codes[searchTargetIndex + 3].labels.AddRange(codes[searchTargetIndex].labels); //move any labels to the instruction after the range we're removing

            codes.RemoveRange(searchTargetIndex, 3);

            return codes;
        }


        //Fix drive and brake getting stuck down if exiting the car while holding them
        [HarmonyPatch("GetVehicleInput")]
        [HarmonyPrefix]
        static void GetVehicleInput_Prefix(VehicleController __instance)
        {
            if (__instance.localPlayerInControl) return;
            __instance.drivePedalPressed = false;
            __instance.brakePedalPressed = false;
            __instance.moveInputVector = Vector2.zero;
        }

        //Fix small entities (ie everything except Eyeless Dogs, Kidnapper Foxes, Forest Giants and Radmechs) not taking dying when run over
        static void PatchSmallEntityCarKill(List<CodeInstruction> codes)
        {
            //locate the if statement responsible for returning when hitting small entities
            int indexFind = PatchUtils.LocateCodeSegment(0, codes, [
                new(OpCodes.Ldarg_S, 5),
                new(OpCodes.Ldc_R4, 1),
                new(OpCodes.Bgt_Un),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ret)]) + 3;

            if (indexFind == -1) { CruiserImproved.Log.LogError("PatchSmallEntityCarKill: Failed to find ret code!"); return; }

            int branchCopy = PatchUtils.LocateCodeSegment(indexFind, codes, [new(OpCodes.Br)]); //copy the destination label of the next branch

            if (branchCopy == -1) { CruiserImproved.Log.LogError("PatchSmallEntityCarKill: Failed to find branch instruction!"); return; }

            object afterCheckJumpOperand = codes[branchCopy].operand;

            //Modify the if statement to set required velocity to 1.0 (like in V55) instead of returning
            codes.RemoveRange(indexFind, 2);
            codes.InsertRange(indexFind, [
            new(OpCodes.Ldc_R4, 1.0f),
            new(OpCodes.Stloc_0),
            new(OpCodes.Br, afterCheckJumpOperand)]);
        }

        //Fix entities only dying on clientside when run over
        static void PatchLocalEntityKill(List<CodeInstruction> codes) 
        {
            //Replace all instances of KillEnemy with KillEnemyOnOwnerClient
            MethodInfo methodFind = typeof(EnemyAI).GetMethod("KillEnemy", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo methodReplace = typeof(EnemyAI).GetMethod("KillEnemyServerRpc", BindingFlags.Instance | BindingFlags.Public);

            foreach (CodeInstruction code in codes)
            {
                if (code.opcode == OpCodes.Callvirt && (MethodInfo)code.operand == methodFind)
                {
                    code.operand = methodReplace;
                }
            }
        }

        [HarmonyPatch(typeof(VehicleController), "CarReactToObstacle")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CarReactToObstacle_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            PatchSmallEntityCarKill(codes);
            PatchLocalEntityKill(codes);

            return codes;
        }

        [HarmonyPatch("OnCollisionEnter")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OnCollisionEnter_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            FieldInfo carHP = typeof(VehicleController).GetField("carHP");

            int targetIndex = PatchUtils.LocateCodeSegment(0, codes, [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, carHP),
                new(OpCodes.Ldc_I4_3),
                new(OpCodes.Bge)
                ]);

            if (targetIndex == -1)
            {
                CruiserImproved.Log.LogError("Could not patch VehicleController.OnCollisionEnter instakill!");
                return codes;
            }

            int removeEndIndex = PatchUtils.LocateCodeSegment(targetIndex, codes, [
                new(OpCodes.Ldloca_S, 4)
                ]);

            if(removeEndIndex == -1)
            {
                CruiserImproved.Log.LogError("Could not locate VehicleController.OnCollisionEnter instakill patch end point!");
                return codes;
            }
            
            //Replace the 'less than 3 health, destroy car' code with 'deal carHP-2 damage or 2 damage, whichever is larger'. This has identical outcome to vanilla but allows DealPermanentDamage prefix to run first for I-frames
            codes.RemoveRange(targetIndex, removeEndIndex - targetIndex);
            codes.InsertRange(targetIndex, [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, carHP),
                new(OpCodes.Ldc_I4_2),
                new(OpCodes.Sub),
                new(OpCodes.Ldc_I4_2),
                new(OpCodes.Call, typeof(Math).GetMethod("Max", [typeof(int), typeof(int)])),
                ]);

            return codes;
        }

        [HarmonyPatch("SetCarEffects")]
        [HarmonyPrefix]
        static void SetCarEffectsPrefix(VehicleController __instance, ref float setSteering)
        {
            //Fix the steering wheel desync bug
            if (__instance.localPlayerInControl)
            {
                setSteering = 0f;
                __instance.steeringWheelAnimFloat = __instance.steeringInput / 6f;
            }
        }
    }
}
