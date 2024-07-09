using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace CruiserImproved.Patches
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
            public int hitsBlockedThisCrit;
            public Coroutine destroyCoroutine;
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

        static System.Collections.IEnumerator DestroyAfterSeconds(VehicleController __instance, float seconds)
        {
            VehicleControllerData extraData = vehicleData[__instance];
            yield return new WaitForSeconds(seconds);

            __instance.DestroyCarServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
            __instance.DestroyCar();
            extraData.destroyCoroutine = null;
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
                    extraData.hitsBlockedThisCrit = 0;
                    timeSinceCriticallyDamaged = 0.0f;
                }

                //Prevent car from dropping below 1HP if we have crit I-frames
                if (timeSinceCriticallyDamaged < critInvulnDuration)
                {
                    damageAmount = Mathf.Min(damageAmount, __instance.carHP - 1);
                    if (__instance.carHP - damageAmount == 1) //blocked a hit at 1hp, count as a block
                    {
                        extraData.hitsBlockedThisCrit++;
                        if (extraData.hitsBlockedThisCrit > UserConfig.MaxCriticalHitCount.Value && extraData.destroyCoroutine == null)
                        {
                            float timeUntilExplosion = UserConfig.CruiserCriticalInvulnerabilityDuration.Value - (Time.realtimeSinceStartup - extraData.timeLastCriticalDamage);
                            extraData.destroyCoroutine = __instance.StartCoroutine(DestroyAfterSeconds(__instance, timeUntilExplosion));
                        }
                    }
                    string blockedCounterStr = $"({extraData.hitsBlockedThisCrit}/{UserConfig.MaxCriticalHitCount.Value})";
                    if (activatedCritThisDamage)
                    {
                        CruiserImproved.Log.LogMessage($"{blockedCounterStr} Critical protection triggered for {critInvulnDuration}s due to {damageAmount} vehicle damage");

                    }
                    else
                    {
                        CruiserImproved.Log.LogMessage($"{blockedCounterStr} Critical protection reduced vehicle damage from {beforeCritDamage} to {damageAmount}");
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

            //if knocked into critical, reset critical related variables
            if (__instance.carHP <= CriticalThreshold && __instance.carHP + amount > CriticalThreshold)
            {
                extraData.timeLastCriticalDamage = Time.realtimeSinceStartup;
                extraData.hitsBlockedThisCrit = 0;
            }

            float timeSinceDamage = Time.realtimeSinceStartup - extraData.timeLastDamaged;
            float timeSinceCriticallyDamaged = Time.realtimeSinceStartup - extraData.timeLastCriticalDamage;

            bool isInvulnerable = timeSinceDamage < UserConfig.CruiserInvulnerabilityDuration.Value;
            bool isCritInvulnerable = timeSinceCriticallyDamaged < UserConfig.CruiserCriticalInvulnerabilityDuration.Value;

            //if receiving damage that will knock the car to 1hp, increase the hitsBlocked
            if (!isInvulnerable && isCritInvulnerable && (__instance.carHP - amount == 1))
            {
                CruiserImproved.Log.LogMessage("Received critical damage blocked from network");
                extraData.hitsBlockedThisCrit++;
            }

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
        }

        [HarmonyPatch("FixedUpdate")]
        [HarmonyPostfix]
        static void FixedUpdate_Postfix(VehicleController __instance)
        {
            //Anti-hill sideslip
            if (!UserConfig.AntiSideslip.Value) return;
            List<WheelCollider> wheels = [__instance.FrontLeftWheel, __instance.FrontRightWheel, __instance.BackLeftWheel, __instance.BackRightWheel];

            //If at least 3 wheels are on the ground, apply a sideways force to the Cruiser, directed up the hill slope, to counter gravity pulling it down the slope.
            Vector3 groundNormal = Vector3.zero;
            int groundedWheelCount = 0;
            foreach(WheelCollider wheel in wheels)
            {
                if(wheel.GetGroundHit(out var hit))
                {
                    groundNormal += hit.normal;
                    groundedWheelCount++;
                }
            }
            groundNormal = groundNormal.normalized;
            if (groundedWheelCount < 3 || Vector3.Angle(-groundNormal, Physics.gravity) > 30f) return;

            Vector3 carFrontHillDirection = Vector3.ProjectOnPlane(__instance.transform.forward, groundNormal).normalized;
            Vector3 hillGravity = -groundNormal * Physics.gravity.magnitude;

            Vector3 force = Vector3.ProjectOnPlane(hillGravity - Physics.gravity, carFrontHillDirection);

            //CruiserImproved.Log.LogMessage("Anti-slip force magnitude " + force.magnitude);

            __instance.mainRigidbody.AddForce(force, ForceMode.Acceleration);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Update_Postfix(VehicleController __instance)
        {
            VehicleControllerData extraData = vehicleData[__instance];

            bool networkDestroyImminent = extraData.hitsBlockedThisCrit > UserConfig.MaxCriticalHitCount.Value && __instance.carHP == 1;
            if (networkDestroyImminent || extraData.destroyCoroutine != null)
            {
                __instance.underExtremeStress = true;

                //ownership got transferred mid destruction from a client, let's start the coroutine here too
                if(__instance.IsOwner && extraData.destroyCoroutine == null && !__instance.carDestroyed)
                {
                    float timeUntilExplosion = UserConfig.CruiserCriticalInvulnerabilityDuration.Value - (Time.realtimeSinceStartup - extraData.timeLastCriticalDamage);
                    CruiserImproved.Log.LogMessage("Destruction coroutine transferred due to ownership switch");
                    extraData.destroyCoroutine = __instance.StartCoroutine(DestroyAfterSeconds(__instance, timeUntilExplosion));
                }
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

        //Allow player to push a destroyed vehicle
        [HarmonyPatch("DestroyCar")]
        [HarmonyPostfix]
        static void DestroyCar_Postfix(VehicleController __instance)
        {
            if (!UserConfig.AllowPushDestroyedCar.Value) return;

            foreach(Transform child in __instance.transform)
            {
                if(child.name == "PushTrigger")
                {
                    child.GetComponent<InteractTrigger>().interactable = true;
                    CruiserImproved.Log.LogMessage("Made car pushable");
                    break;
                }
            }
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

        //Fix inputs still working while chat or pause menu is open        
        [HarmonyPatch("GetVehicleInput")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> GetVehicleInput_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = instructions.ToList();

            var currentDriver = typeof(VehicleController).GetField("currentDriver");
            var isTypingChat = typeof(PlayerControllerB).GetField("isTypingChat");
            var quickMenuManager = typeof(PlayerControllerB).GetField("quickMenuManager");
            var isMenuOpen = typeof(QuickMenuManager).GetField("isMenuOpen");
            var moveInputVector = typeof(VehicleController).GetField("moveInputVector");
            var steeringWheelTurnSpeed = typeof(VehicleController).GetField("steeringWheelTurnSpeed");

            if (currentDriver == null || isTypingChat == null || quickMenuManager == null || isMenuOpen == null || moveInputVector == null || steeringWheelTurnSpeed == null)
            {
                CruiserImproved.Log.LogError("Could not find fields for VehicleInput transpiler!");
                return codes;
            }

            var get_zero = typeof(Vector2).GetMethod("get_zero", BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.Public);

            if(get_zero == null)
            {
                CruiserImproved.Log.LogError("Could not find vector method required for VehicleInput transpiler!");
                return codes;
            }

            int insertIndex = PatchUtils.LocateCodeSegment(0, codes, [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, steeringWheelTurnSpeed),
                new(OpCodes.Stloc_0)
                ]);

            if(insertIndex == -1)
            {
                CruiserImproved.Log.LogError("Could not find insertion point for VehicleInput transpiler!");
            }

            var labelMove = codes[insertIndex].labels;
            var afterIfJump = il.DefineLabel();
            var inIfJump = il.DefineLabel();
            codes[insertIndex].labels = [afterIfJump];

            CodeInstruction inIfBegin = new(OpCodes.Ldarg_0);
            inIfBegin.labels.Add(inIfJump);

            /*
             * IL Code:
             * 
             *  if (this.currentDriver.isTypingChat || this.currentDriver.quickMenuManager.isMenuOpen)
		     *  {
			 *      this.moveInputVector = Vector2.zero;
		     *  }
             * 
             */

            codes.InsertRange(insertIndex, [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, currentDriver),
                new(OpCodes.Ldfld, isTypingChat),
                new(OpCodes.Brtrue_S, inIfJump),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, currentDriver),
                new(OpCodes.Ldfld, quickMenuManager),
                new(OpCodes.Ldfld, isMenuOpen),
                new(OpCodes.Brfalse_S, afterIfJump),
                inIfBegin,
                new(OpCodes.Call, get_zero),
                new(OpCodes.Stfld, moveInputVector)
                ]);

            codes[insertIndex].labels.AddRange(labelMove);

            return codes;
        }

        [HarmonyPatch("DoTurboBoost")]
        [HarmonyPrefix]
        static bool DoTurboBoost_Prefix(VehicleController __instance)
        {
            //Prevent turbo or car jumping if chat is open or player is paused
            if (__instance.localPlayerInControl && __instance.currentDriver)
            {
                if (__instance.currentDriver.isTypingChat || __instance.currentDriver.quickMenuManager.isMenuOpen) return false;
            }
            return true;
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

        //Fix slow impacts not actually damaging entities
        static void PatchLocalEntityDamage(List<CodeInstruction> codes) 
        {
            //Replace all instances of KillEnemy with KillEnemyOnOwnerClient
            MethodInfo hitEnemy = typeof(EnemyAI).GetMethod("HitEnemy", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo hitEnemyOnLocalClient = typeof(EnemyAI).GetMethod("HitEnemyOnLocalClient", BindingFlags.Instance | BindingFlags.Public);

            var get_zero = typeof(Vector2).GetMethod("get_zero", BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.Public);

            int insertBefore = PatchUtils.LocateCodeSegment(0, codes, [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ldc_I4),
                new(OpCodes.Callvirt, hitEnemy)
                ]);

            if(insertBefore == -1)
            {
                CruiserImproved.Log.LogError("PatchLocalEntityDamage: Failed to find HitEnemy call!");
                return;
            }

            codes[insertBefore + 4].operand = hitEnemyOnLocalClient;
            codes.Insert(insertBefore, new(OpCodes.Call, get_zero));
        }

        [HarmonyPatch("CarReactToObstacle")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CarReactToObstacle_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            PatchSmallEntityCarKill(codes);
            PatchLocalEntityDamage(codes);

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
            
            //Replace the 'less than 3 health, destroy car' code with 'deal carHP-1 damage or 2 damage, whichever is larger'. This has identical outcome to vanilla but allows DealPermanentDamage prefix to run first for I-frames
            codes.RemoveRange(targetIndex, removeEndIndex - targetIndex);
            codes.InsertRange(targetIndex, [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, carHP),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Sub),
                new(OpCodes.Ldc_I4_2),
                new(OpCodes.Call, typeof(Math).GetMethod("Max", [typeof(int), typeof(int)])),
                ]);

            return codes;
        }

        [HarmonyPatch("SetCarEffects")]
        [HarmonyPrefix]
        static void SetCarEffects_Prefix(VehicleController __instance, ref float setSteering)
        {
            //Fix the steering wheel desync bug
            if (__instance.localPlayerInControl)
            {
                setSteering = 0f;
                __instance.steeringWheelAnimFloat = __instance.steeringInput / 6f;
            }
        }

        //Patch non-drivers ejecting drivers in the Cruiser
        //Since we need to know who sent the rpc and SpringDriverSeatServerRpc doesn't take rpcParams we need to patch the rpc handler directly for access to this data
        //The ulong handler id appears to be identical between versions so this patch *shouldn't* break
        [HarmonyPatch("__rpc_handler_46143233")]
        [HarmonyPrefix]
        static bool SpringDriverSeatServerRpc_Handler_Prefix(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (!UserConfig.PreventPassengersEjectingDriver.Value) return true;
            NetworkManager networkManager = target.NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return true;
            }
            var targetVehicle = (VehicleController)target;

            //don't process the rpc if the sender isn't the driver
            if (rpcParams.Server.Receive.SenderClientId != targetVehicle.currentDriver.actualClientId) return false;
            return true;
        }

        //dropin for Physics.Linecast in the CanExitCar method. Return true if cannot use exitPoint, return false if can
        static bool CheckExitPointInvalid(Vector3 playerPos, Vector3 exitPoint, int layerMask, QueryTriggerInteraction interaction)
        {
            //The vanilla linecast check to the exitPoint
            if (Physics.Linecast(playerPos, exitPoint, layerMask, interaction)) return true;

            //Added check: Make sure nothing is around the exit point
            if (Physics.CheckCapsule(exitPoint, exitPoint+Vector3.up, 0.5f, layerMask, interaction)) return true;

            return false;
        }

        [HarmonyPatch("CanExitCar")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CanExitCar_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //Replace all Physics.Linecast with CheckExitPointInvalid so we can run more tests on the exit point
            MethodInfo linecast = typeof(Physics).GetMethod("Linecast", BindingFlags.Static | BindingFlags.Public, null, [typeof(Vector3), typeof(Vector3), typeof(int), typeof(QueryTriggerInteraction)], null);
            MethodInfo exitPointInvalid = typeof(VehicleControllerPatches).GetMethod("CheckExitPointInvalid", BindingFlags.Static | BindingFlags.NonPublic, null, [typeof(Vector3), typeof(Vector3), typeof(int), typeof(QueryTriggerInteraction)], null);

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == linecast)
                {
                    instruction.operand = exitPointInvalid;
                }
            }
            return instructions;
        }

        [HarmonyPatch("ExitPassengerSideSeat")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ExitPassengerSideSeat_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            MethodInfo canExitCar = typeof(VehicleController).GetMethod("CanExitCar");

            //replace CanExitCar(true) with CanExxitCar(false) so it properly checks the passenger side
            int index = PatchUtils.LocateCodeSegment(0, codes, [
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Call, canExitCar)
                ]);

            if(index == -1)
            {
                CruiserImproved.Log.LogError("Could not patch ExitPassengerSideSeat!");
                return codes;
            }
            codes[index].opcode = OpCodes.Ldc_I4_0;
            return codes;
        }
    }
}
