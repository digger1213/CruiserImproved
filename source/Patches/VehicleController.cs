using CruiserImproved.Network;
using CruiserImproved.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CruiserImproved.Patches;

[HarmonyPatch(typeof(VehicleController))]
internal class VehicleControllerPatches
{
    public class VehicleControllerData
    {
        public int lastDamageReceived;
        public float timeLastDamaged;
        public float timeLastCriticalDamage;
        public int hitsBlockedThisCrit;
        public Coroutine destroyCoroutine;
        public NavMeshObstacle navObstacle;
        public float lastSteeringAngle;
        public float timeLastSyncedRadio;

        public ScanNodeProperties scanNode;
        public int lastScanHP = -1;
        public int lastScanTurbo = -1;

        public bool usingColoredExhaust = false;
        public ParticleSystem particleSystemSwap;
    }

    static readonly int CriticalThreshold = 2;

    static readonly int ScanDamagedThreshold = 15;
    static readonly int ScanCriticalThreshold = 5;

    static readonly string DefaultScanText = "Company Cruiser";
    static readonly string DefaultScanSubtext = "You've got work to do.";

    static readonly string ScanDamagedText = "Damaged Cruiser";
    static readonly string ScanCriticalText = "Critical Cruiser";
    static readonly string ScanDestroyedText = "Destroyed Cruiser";
    static readonly string ScanDetailedHealthText = "Company Cruiser ({0}%)";

    static readonly string ScanTurboSubtext = "Turbocharged";
    static readonly string ScanDetailedTurboSubtext = "{0}% Turbocharged";

    static readonly string[] DestroyDisableList =
    {
        "HPMeter",
        "TurboMeter",
        "Triggers/ButtonAnimContainer",
        "Meshes/FrontLight",
        "Meshes/GearStickContainer",
        "Meshes/SteeringWheelContainer",
        "Meshes/DriverSeatContainer",
        "Meshes/DoorLeftContainer",
        "Meshes/DoorRightContainer",
        "Meshes/FrontCabinLight",
        "Meshes/CabinWindowContainer",
    };

    public static Dictionary<VehicleController, VehicleControllerData> vehicleData = new();

    private static void RemoveStaleVehicleData()
    {
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
    }

    static void SetupSyncedVehicleFeatures(VehicleController vehicle)
    {
        VehicleControllerData thisData = vehicleData[vehicle];

        //Allow player to turn further backward for the lean mechanic
        if (NetworkSync.Config.AllowLean)
        {
            vehicle.driverSeatTrigger.horizontalClamp = 163f;
            vehicle.passengerSeatTrigger.horizontalClamp = 163f;
        }

        //Set up the car's NavMeshObstacle if host has it enabled (it's mostly meaningless otherwise)
        if (NetworkSync.SyncedWithHost && NetworkSync.Config.EntitiesAvoidCruiser)
        {
            GameObject cruiserNavBlockerObject = new("CruiserObstacle");
            cruiserNavBlockerObject.transform.localPosition = new Vector3(0, -2, 0);
            cruiserNavBlockerObject.transform.localScale = new Vector3(4, -2, 9);

            var navmeshObstacle = cruiserNavBlockerObject.AddComponent<NavMeshObstacle>();
            navmeshObstacle.carveOnlyStationary = true;
            navmeshObstacle.carving = true;
            navmeshObstacle.shape = NavMeshObstacleShape.Box;

            thisData.navObstacle = navmeshObstacle;

            cruiserNavBlockerObject.transform.parent = vehicle.transform;
        }

        if (NetworkSync.Config.HandsfreeDoors)
        {
            vehicle.driverSideDoorTrigger.twoHandedItemAllowed = true;
            vehicle.passengerSideDoorTrigger.twoHandedItemAllowed = true;

            vehicle.backDoorContainer.GetComponentsInChildren<InteractTrigger>().Do((trigger) => { trigger.twoHandedItemAllowed = true; });
        }

        //Set up scan node
        if (NetworkSync.Config.CruiserScanNode.HasFlag(ScanNodeOptions.Enabled))
        {
            GameObject cruiserScanNode = new("CruiserScanNode");
            cruiserScanNode.layer = LayerMask.NameToLayer("ScanNode");

            //thanks unity - need a rigidbody on the scan node so it can be detected by the spherecast without being overriden by the Cruiser's rigidbody
            var rigidbody = cruiserScanNode.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;

            var collider = cruiserScanNode.AddComponent<BoxCollider>();
            var scanProperties = cruiserScanNode.AddComponent<ScanNodeProperties>();

            scanProperties.requiresLineOfSight = !NetworkSync.Config.CruiserScanNode.HasFlag(ScanNodeOptions.VisibleThroughWalls);
            scanProperties.maxRange = 100;
            scanProperties.minRange = 6;
            scanProperties.headerText = DefaultScanText;
            scanProperties.subText = DefaultScanSubtext;
            thisData.scanNode = scanProperties;

            cruiserScanNode.transform.parent = vehicle.transform;
            cruiserScanNode.transform.localPosition = Vector3.zero;

            UpdateCruiserScanText(vehicle);
        }


        if (NetworkSync.Config.TurboExhaust)
        {
            thisData.particleSystemSwap = UnityEngine.Object.Instantiate(vehicle.carExhaustParticle, vehicle.transform);
            thisData.particleSystemSwap.name = "Turbo Exhaust";

            var colorOverLifetime = thisData.particleSystemSwap.colorOverLifetime;

            var colorKeys = colorOverLifetime.color.gradient.colorKeys;
            colorKeys[0].color = new(0.17f, 0.17f, 0.3f);
            colorKeys[0].time = 0.3f;
            colorKeys[1].time = 0.7f;

            var mmGradient = colorOverLifetime.color;
            mmGradient.gradient.SetKeys(colorKeys, mmGradient.gradient.alphaKeys);

            colorOverLifetime.color = mmGradient;
        }
    }

    public static void UpdateCruiserScanText(VehicleController vehicle, bool forceUpdate = false)
    {
        var extraData = vehicleData[vehicle];
        if (extraData == null || !extraData.scanNode) return;

        if (extraData.lastScanTurbo == vehicle.turboBoosts && extraData.lastScanHP == vehicle.carHP && !forceUpdate) return;

        extraData.lastScanTurbo = vehicle.turboBoosts;
        extraData.lastScanHP = vehicle.carHP;

        var scanProperties = extraData.scanNode;

        ScanNodeOptions flags = NetworkSync.Config.CruiserScanNode;

        bool displayTurbo = (flags & (ScanNodeOptions.TurboEstimate | ScanNodeOptions.TurboPercentage)) != 0;
        bool displayHealth = (flags & (ScanNodeOptions.HealthEstimate | ScanNodeOptions.HealthPercentage)) != 0;

        bool turboDetailed = flags.HasFlag(ScanNodeOptions.TurboPercentage);
        bool healthDetailed = flags.HasFlag(ScanNodeOptions.HealthPercentage);

        if (!displayTurbo || vehicle.turboBoosts == 0)
        {
            scanProperties.subText = DefaultScanSubtext;
        }
        else if (!turboDetailed)
        {
            scanProperties.subText = ScanTurboSubtext;
        }
        else
        {
            int percent = vehicle.turboBoosts * 100 / 5;
            scanProperties.subText = string.Format(ScanDetailedTurboSubtext, percent);
        }

        if (displayHealth)
        {
            if (vehicle.carDestroyed || vehicle.carHP <= 0)
            {
                scanProperties.headerText = ScanDestroyedText;
                scanProperties.subText = "";
            }
            else if (healthDetailed)
            {
                int percent = vehicle.carHP * 100 / vehicle.baseCarHP;
                scanProperties.headerText = string.Format(ScanDetailedHealthText, percent);
            }
            else if (vehicle.carHP < ScanCriticalThreshold) scanProperties.headerText = ScanCriticalText;
            else if (vehicle.carHP < ScanDamagedThreshold) scanProperties.headerText = ScanDamagedText;
            else scanProperties.headerText = DefaultScanText;
        }
    }

    static void UpdateExhaustColor(VehicleController vehicle)
    {
        var extraData = vehicleData[vehicle];

        bool hasTurbos = vehicle.turboBoosts > 0;
        
        //swap to the turbo exhaust particles (or back)
        if(hasTurbos != extraData.usingColoredExhaust && extraData.particleSystemSwap)
        {
            extraData.usingColoredExhaust = hasTurbos;
            bool on = vehicle.carExhaustParticle.isPlaying;

            vehicle.carExhaustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            //swap the VehicleController.carExhaustParticle
            (extraData.particleSystemSwap, vehicle.carExhaustParticle) = (vehicle.carExhaustParticle, extraData.particleSystemSwap);
            if(on) vehicle.carExhaustParticle.Play();
        }
    }

    public static void OnSync()
    {
        RemoveStaleVehicleData();

        foreach (var elem in vehicleData)
        {
            try
            {
                SetupSyncedVehicleFeatures(elem.Key);
            }
            catch (Exception e)
            {
                CruiserImproved.LogError("Exception caught setting up synced vehicle features:\n" + e);
            }
        }
    }

    public static void SendClientSyncData(ulong clientId)
    {
        RemoveStaleVehicleData();

        foreach (var elem in vehicleData)
        {
            VehicleController vehicle = elem.Key;
            if(vehicle.turboBoosts > 0)
            {
                //Call AddTurboBoostClientRpc(0, vehicle.turboBoosts);
                RpcSender.SendClientRpc(vehicle, 4268487771U, [clientId], (ref FastBufferWriter fastBufferWriter) => 
                {
                    BytePacker.WriteValueBitPacked(fastBufferWriter, 0);
                    BytePacker.WriteValueBitPacked(fastBufferWriter, vehicle.turboBoosts);
                });
            }
            if (vehicle.ignitionStarted)
            {
                //Call StartIgnitionClientRpc(0);
                RpcSender.SendClientRpc(vehicle, 3273216474U, [clientId], (ref FastBufferWriter fastBufferWriter) =>
                {
                    BytePacker.WriteValueBitPacked(fastBufferWriter, 0);
                });
            }
            if (vehicle.magnetedToShip)
            {
                //Call MagnetCarClientRpc(vehicle.magnetTargetPosition, vehicle.magnetTargetRotation, 0);
                RpcSender.SendClientRpc(vehicle, 2845017736U, [clientId], (ref FastBufferWriter fastBufferWriter) =>
                {
                    fastBufferWriter.WriteValueSafe(in vehicle.magnetTargetPosition);
                    Vector3 eulerAngles = vehicle.magnetTargetRotation.eulerAngles;
                    fastBufferWriter.WriteValueSafe(in eulerAngles);
                    BytePacker.WriteValueBitPacked(fastBufferWriter, 0);
                });
            }
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

    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    private static void Awake_Postfix(VehicleController __instance)
    {
        RemoveStaleVehicleData();

        VehicleControllerData thisData = new();
        vehicleData.Add(__instance, thisData);

        //Move all cruiser children from MoldSpore to Triggers so weedkiller can't shrink it
        Transform[] allChildObjects = __instance.GetComponentsInChildren<Transform>();
        int moldSporeLayer = LayerMask.NameToLayer("MoldSpore");
        int replaceLayer = LayerMask.NameToLayer("Triggers");

        foreach (Transform transform in allChildObjects)
        {
            if (transform.gameObject.layer == moldSporeLayer)
            {
                transform.gameObject.layer = replaceLayer;
            }
        }

        //Fix items dropping through the back of the cruiser
        Transform itemDropCollider = __instance.physicsRegion.itemDropCollider.transform;
        itemDropCollider.localScale = new Vector3(itemDropCollider.localScale.x, itemDropCollider.localScale.y, 5f);

        if (NetworkSync.FinishedSync)
        {
            SetupSyncedVehicleFeatures(__instance);
        }
    }

    [HarmonyPatch("FixedUpdate")]
    [HarmonyPostfix]
    static void FixedUpdate_Postfix(VehicleController __instance)
    {
        //Anti-hill sideslip
        if (!NetworkSync.Config.AntiSideslip) return;
        List<WheelCollider> wheels = [__instance.FrontLeftWheel, __instance.FrontRightWheel, __instance.BackLeftWheel, __instance.BackRightWheel];

        //If at least 3 wheels are on the ground, apply a force to the Cruiser, directed up the hill slope, to counter gravity pulling it down the slope.
        Vector3 groundNormal = Vector3.zero;
        int groundedWheelCount = 0;
        foreach (WheelCollider wheel in wheels)
        {
            if (wheel.GetGroundHit(out var hit))
            {
                groundNormal += hit.normal;
                groundedWheelCount++;
            }
        }
        groundNormal = groundNormal.normalized;
        if (groundedWheelCount < 3 || Vector3.Angle(-groundNormal, Physics.gravity) > 30f) return;

        Vector3 carFrontHillDirection = Vector3.ProjectOnPlane(__instance.transform.forward, groundNormal).normalized;
        Vector3 hillGravity = -groundNormal * Physics.gravity.magnitude;

        Vector3 force = hillGravity - Physics.gravity; //apply the difference between real gravity and the 'hill' downward gravity

        //if we're not in park, don't apply forces in the forward or backward direction (car should still roll down hills)
        if (__instance.gear != CarGearShift.Park)
        {
            force = Vector3.ProjectOnPlane(force, carFrontHillDirection);
        }

        //CruiserImproved.Log.LogMessage("Anti-slip force magnitude " + force.magnitude);

        __instance.mainRigidbody.AddForce(force, ForceMode.Acceleration);
    }

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    static void Update_Postfix(VehicleController __instance)
    {
        if (!__instance.IsSpawned)
        {
            return;
        }
        VehicleControllerData extraData = vehicleData[__instance];

        UpdateCruiserScanText(__instance);

        UpdateExhaustColor(__instance);

        if (NetworkSync.Config.DisableRadioStatic)
        {
            __instance.radioSignalQuality = 3f;
        }

        //Sync radio time once per second
        if (__instance.IsHost && (Time.realtimeSinceStartup - extraData.timeLastSyncedRadio > 1f))
        {
            extraData.timeLastSyncedRadio = Time.realtimeSinceStartup;
            FastBufferWriter bufferWriter = new(16, Unity.Collections.Allocator.Temp);

            bufferWriter.WriteValue(new NetworkObjectReference(__instance.NetworkObject));
            bufferWriter.WriteValue(__instance.currentSongTime);
            NetworkSync.SendToClients("SyncRadioTimeRpc", ref bufferWriter);
        }

        //Fix sound not playing when magneted if this cruiser was loaded in
        if (__instance.finishedMagneting) __instance.loadedVehicleFromSave = false;

        //Fix cruiser physics region not active when magneted, resulting in player sliding
        if (__instance.magnetedToShip) __instance.physicsRegion.priority = 1;

        bool networkDestroyImminent = extraData.hitsBlockedThisCrit > NetworkSync.Config.MaxCriticalHitCount && __instance.carHP == 1;
        if (networkDestroyImminent || extraData.destroyCoroutine != null)
        {
            __instance.underExtremeStress = true;

            //ownership got transferred mid destruction from a client, let's start the coroutine here too
            if (__instance.IsOwner && extraData.destroyCoroutine == null && !__instance.carDestroyed)
            {
                float timeUntilExplosion = NetworkSync.Config.CruiserCriticalInvulnerabilityDuration - (Time.realtimeSinceStartup - extraData.timeLastCriticalDamage);
                extraData.destroyCoroutine = __instance.StartCoroutine(DestroyAfterSeconds(__instance, timeUntilExplosion));
            }
        }

        //Disable the nav obstacle when the cruiser is moving
        if (NetworkSync.Config.EntitiesAvoidCruiser && extraData.navObstacle)
        {
            bool enableObstacle = __instance.averageVelocity.magnitude < 0.5f && !__instance.currentDriver && !__instance.currentPassenger;

            extraData.navObstacle.gameObject.SetActive(enableObstacle);
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
            new(OpCodes.Ldfld, PatchUtils.Field(typeof(VehicleController), "localPlayerInControl")),
            new(OpCodes.Brfalse),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Call, PatchUtils.Method(typeof(NetworkBehaviour), "get_IsOwner")),
            new(OpCodes.Brtrue)
            ]);

        if (searchTargetIndex == -1)
        {
            CruiserImproved.LogWarning("Could not transpile VehicleController.Update");
            return codes;
        }

        codes[searchTargetIndex + 3].labels.AddRange(codes[searchTargetIndex].labels); //move any labels to the instruction after the range we're removing

        codes.RemoveRange(searchTargetIndex, 3);

        return codes;
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

        float invulnDuration = NetworkSync.Config.CruiserInvulnerabilityDuration;
        float critInvulnDuration = NetworkSync.Config.CruiserCriticalInvulnerabilityDuration;

        bool isInvulnerable = timeSinceDamage < invulnDuration;
        bool isCritInvulnerable = timeSinceCriticallyDamaged < critInvulnDuration;

        //Prevent damage less than what we last received if within I-frame duration
        if (isInvulnerable && extraData.lastDamageReceived >= damageAmount)
        {
            CruiserImproved.LogInfo($"Vehicle ignored {damageAmount} damage due to I-frames from previous damage {extraData.lastDamageReceived} ({Math.Round(timeSinceDamage, 2)}s)");
            damageAmount = 0;
            return;
        }

        //If this damage is higher than the last damage we received and we're still in I-frames, allow the extra damage through (so a small hit can't block for a bigger one)
        if (isInvulnerable && extraData.lastDamageReceived > 0)
        {
            damageAmount -= extraData.lastDamageReceived;
            CruiserImproved.LogInfo($"Vehicle reduced {originalDamage} to {damageAmount} due to I-frames from previous damage {extraData.lastDamageReceived} ({Math.Round(timeSinceDamage, 2)}s)");
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
                    if (extraData.hitsBlockedThisCrit > NetworkSync.Config.MaxCriticalHitCount && extraData.destroyCoroutine == null)
                    {
                        float timeUntilExplosion = NetworkSync.Config.CruiserCriticalInvulnerabilityDuration - (Time.realtimeSinceStartup - extraData.timeLastCriticalDamage);
                        extraData.destroyCoroutine = __instance.StartCoroutine(DestroyAfterSeconds(__instance, timeUntilExplosion));
                    }
                }
                string blockedCounterStr = $"({extraData.hitsBlockedThisCrit}/{NetworkSync.Config.MaxCriticalHitCount})";
                if (activatedCritThisDamage)
                {
                    CruiserImproved.LogInfo($"{blockedCounterStr} Critical protection triggered for {critInvulnDuration}s due to {damageAmount} vehicle damage");

                }
                else
                {
                    CruiserImproved.LogInfo($"{blockedCounterStr} Critical protection reduced vehicle damage from {beforeCritDamage} to {damageAmount}");
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

        bool isInvulnerable = timeSinceDamage < NetworkSync.Config.CruiserInvulnerabilityDuration;
        bool isCritInvulnerable = timeSinceCriticallyDamaged < NetworkSync.Config.CruiserCriticalInvulnerabilityDuration;

        //if receiving damage that will knock the car to 1hp, increase the hitsBlocked
        if (!isInvulnerable && isCritInvulnerable && (__instance.carHP - amount == 1))
        {
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

    [HarmonyPatch("AddEngineOilOnLocalClient")]
    [HarmonyPostfix]
    static void AddEngineOilOnLocalClient_Postfix(VehicleController __instance, int setCarHP)
    {
        if (setCarHP <= 1) return;

        VehicleControllerData extraData = vehicleData[__instance];

        if(extraData.destroyCoroutine != null)
        {
            __instance.StopCoroutine(extraData.destroyCoroutine);
            extraData.destroyCoroutine = null;
            __instance.underExtremeStress = false;
        }
    }

    //Allow player to push a destroyed vehicle
    [HarmonyPatch("DestroyCar")]
    [HarmonyPostfix]
    static void DestroyCar_Postfix(VehicleController __instance)
    {
        UpdateCruiserScanText(__instance, true);

        __instance.carExhaustParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        foreach(string name in DestroyDisableList)
        {
            Transform child = __instance.transform.Find(name);
            if (child)
            {
                child.gameObject.SetActive(false);
            }
        }

        if (!NetworkSync.Config.AllowPushDestroyedCar) return;

        foreach(Transform child in __instance.transform)
        {
            if(child.name == "PushTrigger")
            {
                child.GetComponent<InteractTrigger>().interactable = true;
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

        var currentDriver = PatchUtils.Field(typeof(VehicleController), "currentDriver");
        var isTypingChat = PatchUtils.Field(typeof(PlayerControllerB), "isTypingChat");
        var quickMenuManager = PatchUtils.Field(typeof(PlayerControllerB), "quickMenuManager");
        var isMenuOpen = PatchUtils.Field(typeof(QuickMenuManager), "isMenuOpen");
        var moveInputVector = PatchUtils.Field(typeof(VehicleController), "moveInputVector");
        var steeringWheelTurnSpeed = PatchUtils.Field(typeof(VehicleController), "steeringWheelTurnSpeed");

        if (currentDriver == null || isTypingChat == null || quickMenuManager == null || isMenuOpen == null || moveInputVector == null || steeringWheelTurnSpeed == null)
        {
            CruiserImproved.LogWarning("Could not find fields for VehicleInput transpiler!");
            return codes;
        }

        var get_zero = PatchUtils.Method(typeof(Vector2), "get_zero");

        if(get_zero == null)
        {
            CruiserImproved.LogWarning("Could not find vector method required for VehicleInput transpiler!");
            return codes;
        }

        int insertIndex = PatchUtils.LocateCodeSegment(0, codes, [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, steeringWheelTurnSpeed),
            new(OpCodes.Stloc_0)
            ]);

        if(insertIndex == -1)
        {
            CruiserImproved.LogWarning("Could not find insertion point for VehicleInput transpiler!");
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

        if (indexFind == -1)
        {
            CruiserImproved.LogWarning("PatchSmallEntityCarKill: Failed to find ret code!"); 
            return; 
        }

        int branchCopy = PatchUtils.LocateCodeSegment(indexFind, codes, [new(OpCodes.Br)]); //copy the destination label of the next branch

        if (branchCopy == -1) 
        { 
            CruiserImproved.LogWarning("PatchSmallEntityCarKill: Failed to find branch instruction!"); 
            return; 
        }

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
        MethodInfo hitEnemy = PatchUtils.Method(typeof(EnemyAI), "HitEnemy");
        MethodInfo hitEnemyOnLocalClient = PatchUtils.Method(typeof(EnemyAI), "HitEnemyOnLocalClient");

        var get_zero = PatchUtils.Method(typeof(Vector2), "get_zero");

        int insertBefore = PatchUtils.LocateCodeSegment(0, codes, [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld),
            new(OpCodes.Ldc_I4_1),
            new(OpCodes.Ldc_I4),
            new(OpCodes.Callvirt, hitEnemy)
            ]);

        if(insertBefore == -1)
        {
            CruiserImproved.LogWarning("PatchLocalEntityDamage: Failed to find HitEnemy call!");
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

        FieldInfo carHP = PatchUtils.Field(typeof(VehicleController), "carHP");

        int targetIndex = PatchUtils.LocateCodeSegment(0, codes, [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, carHP),
            new(OpCodes.Ldc_I4_3),
            new(OpCodes.Bge)
            ]);

        if (targetIndex == -1)
        {
            CruiserImproved.LogWarning("Could not patch VehicleController.OnCollisionEnter instakill!");
            return codes;
        }

        int removeEndIndex = PatchUtils.LocateCodeSegment(targetIndex, codes, [
            new(OpCodes.Ldloca_S, 4)
            ]);

        if(removeEndIndex == -1)
        {
            CruiserImproved.LogWarning("Could not locate VehicleController.OnCollisionEnter instakill patch end point!");
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

    //Rpc Args: NetworkObjectReference cruiserRef, float angle
    static public void SyncSteeringRpc(ulong clientId, FastBufferReader reader)
    {
        reader.ReadNetworkSerializable(out NetworkObjectReference cruiserRef);
        reader.ReadValue(out float angle);
        if (!cruiserRef.TryGet(out NetworkObject cruiserNetObj)) return;
        if (!cruiserNetObj.TryGetComponent(out VehicleController vehicle)) return;

        if (NetworkManager.Singleton.IsHost)
        {
            FastBufferWriter bufferWriter = new(16, Unity.Collections.Allocator.Temp);

            bufferWriter.WriteValue(cruiserRef);
            bufferWriter.WriteValue(angle);
            NetworkSync.SendToClients("SyncSteeringRpc", ref bufferWriter);
        }

        vehicleData[vehicle].lastSteeringAngle = angle;
    }

    [HarmonyPatch("SetCarEffects")]
    [HarmonyPrefix]
    static void SetCarEffects_Prefix(VehicleController __instance, ref float setSteering)
    {
        //Fix the steering wheel desync bug
        setSteering = 0f;
        if (__instance.localPlayerInControl)
        {
            __instance.steeringWheelAnimFloat = __instance.steeringInput / 6f;
            if (Mathf.Abs(__instance.steeringInput - vehicleData[__instance].lastSteeringAngle) > 0.02f)
            {
                FastBufferWriter bufferWriter = new(16, Unity.Collections.Allocator.Temp);

                bufferWriter.WriteValue(new NetworkObjectReference(__instance.NetworkObject));
                bufferWriter.WriteValue(__instance.steeringInput);
                NetworkSync.SendToHost("SyncSteeringRpc", bufferWriter);
            }
        }
        else
        {
            __instance.steeringWheelAnimFloat = vehicleData[__instance].lastSteeringAngle / 6f;
            __instance.steeringInput = vehicleData[__instance].lastSteeringAngle;
        }
    }

    [HarmonyPatch("SetCarEffects")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> SetCarEffects_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();

        //Locate front left wheel motorTorque check
        int index = PatchUtils.LocateCodeSegment(0, codes, [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, PatchUtils.Field(typeof(VehicleController), "FrontLeftWheel")),
            new(OpCodes.Callvirt, PatchUtils.Method(typeof(WheelCollider), "get_motorTorque")),
            new(OpCodes.Ldc_R4),
            new(OpCodes.Ble_Un),
            ]);

        if(index == -1)
        {
            CruiserImproved.LogWarning("Could not patch SetCarEffects!");
            return instructions;
        }

        var jumpTo = codes[index + 4].operand; //get jump destination from Ble_un above

        CruiserImproved.LogInfo("Branch destination: " + jumpTo.ToString());

        //at the end of the previous if statement (airborne wheels) jump to this if statement's else block (turns off wheel skidding)
        codes.Insert(index, new(OpCodes.Br, jumpTo));

        CruiserImproved.LogMessage(string.Join("\n", codes.GetRange(index - 10, 50).Select(var => var.ToString())));

        return codes;
    }

    //Patch non-drivers ejecting drivers in the Cruiser
    //Since we need to know who sent the rpc and SpringDriverSeatServerRpc doesn't take rpcParams we need to patch the rpc handler directly for access to this data
    //The ulong handler id appears to be identical between versions so this patch *shouldn't* break
    [HarmonyPatch("__rpc_handler_46143233")]
    [HarmonyPrefix]
    static bool SpringDriverSeatServerRpc_Handler_Prefix(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
    {
        if (!NetworkSync.Config.PreventPassengersEjectingDriver) return true;
        NetworkManager networkManager = target.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
        {
            return true;
        }
        var targetVehicle = (VehicleController)target;

        //don't process the rpc if the sender isn't the driver
        if (targetVehicle.currentDriver == null || rpcParams.Server.Receive.SenderClientId != targetVehicle.currentDriver.actualClientId) return false;
        return true;
    }

    //dropin for Physics.Linecast in the CanExitCar method. Return true if cannot use exitPoint, return false if can
    static bool CheckExitPointInvalid(Vector3 playerPos, Vector3 exitPoint, int layerMask, QueryTriggerInteraction interaction)
    {
        //The vanilla linecast check to the exitPoint
        if (Physics.Linecast(playerPos, exitPoint, layerMask, interaction))
        {
            return true;
        }

        //Added check: Make sure nothing is around the exit point
        if (Physics.CheckCapsule(exitPoint, exitPoint + Vector3.up, 0.5f, layerMask, interaction))
        {
            return true;
        }

        LayerMask maskAndVehicle = layerMask | LayerMask.GetMask("Vehicle");

        //Added check: Check for ground below the exit point
        if (!Physics.Linecast(exitPoint, exitPoint + Vector3.down * 4f, maskAndVehicle, interaction))
        {
            return true;
        }

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

        MethodInfo canExitCar = PatchUtils.Method(typeof(VehicleController), "CanExitCar");

        //replace CanExitCar(true) with CanExitCar(false) so it properly checks the passenger side
        int index = PatchUtils.LocateCodeSegment(0, codes, [
            new(OpCodes.Ldc_I4_1),
            new(OpCodes.Call, canExitCar)
            ]);

        if(index == -1)
        {
            CruiserImproved.LogWarning("Could not patch ExitPassengerSideSeat!");
            return codes;
        }
        codes[index].opcode = OpCodes.Ldc_I4_0;
        return codes;
    }

    //Injected method, return true if impact audio should be detectable by dogs
    static bool ShouldPlayDetectableAudio(VehicleController instance)
    {
        return instance.ignitionStarted || !NetworkSync.Config.SilentCollisions;
    }

    [HarmonyPatch("PlayRandomClipAndPropertiesFromAudio")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> PlayRandomClipAndPropertiesFromAudio_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        //Prevent the cruiser from creating detectable sounds on collision if the engine is off, to prevent dogs from repeatedly attacking cruisers.
        var codes = instructions.ToList();

        var getRoundManagerInstance = PatchUtils.Method(typeof(RoundManager), "get_Instance");
        var ignitionStarted = PatchUtils.Field(typeof(VehicleController), "ignitionStarted");

        int index = PatchUtils.LocateCodeSegment(0, codes, [
            new(OpCodes.Ldarg_S, 4),
            new(OpCodes.Ldc_I4_2),
            new(OpCodes.Blt),
            new(OpCodes.Call, getRoundManagerInstance)
            ]);

        if (index == -1)
        {
            CruiserImproved.LogWarning("Failed to find code segment in PlayRandomClipAndPropertiesFromAudio");
            return codes;
        }

        int jumpIndex = PatchUtils.LocateCodeSegment(index, codes, [
            new(OpCodes.Ldarg_S, 4),
            new(OpCodes.Ldc_I4_M1),
            new(OpCodes.Bne_Un),
            ]);

        if(jumpIndex == -1)
        {
            CruiserImproved.LogWarning("Failed to find end jump segment in PlayRandomClipAndPropertiesFromAudio");
            return codes;
        }

        Label destinationLabel = codes[jumpIndex].labels[0];

        codes.InsertRange(index, [
            new(OpCodes.Ldarg_0),
            new(OpCodes.Call, PatchUtils.Method(typeof(VehicleControllerPatches), "ShouldPlayDetectableAudio")),
            new(OpCodes.Brfalse_S, destinationLabel)
            ]);

        return codes;
    }

    //Method to override StartMagneting's target angle and position. Returns eulerAngles, sets magnetTargetPosition and magnetTargetRotation fields.
    static Vector3 FixMagnet(VehicleController instance)
    {        
        Vector3 eulerAngles = instance.transform.eulerAngles;
        eulerAngles.y = Mathf.Round((eulerAngles.y + 90f) / 180f) * 180f - 90f;
        eulerAngles.z = Mathf.Round(eulerAngles.z / 90f) * 90f;
        float x = Mathf.Repeat(eulerAngles.x + UnityEngine.Random.Range(-5f, 5f) + 180, 360) - 180;
        eulerAngles.x = Mathf.Clamp(x, -20f, 20f);
        instance.magnetTargetRotation = Quaternion.Euler(eulerAngles);

        Vector3 offset = new(0f, -0.5f, -instance.boundsCollider.size.x * 0.5f * instance.boundsCollider.transform.lossyScale.x);
        Vector3 localPos = StartOfRound.Instance.magnetPoint.position + offset;
        instance.magnetTargetPosition = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(localPos);

        return eulerAngles;
    }

    [HarmonyPatch("StartMagneting")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> StartMagneting_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var codes = instructions.ToList();

        var getIsOwner = PatchUtils.Method(typeof(NetworkBehaviour), "get_IsOwner");

        int retIndex = PatchUtils.LocateCodeSegment(0, codes, [
            //locate early return if not owner to remove it
            new(OpCodes.Ldarg_0),
            new(OpCodes.Call, getIsOwner),
            new(OpCodes.Brtrue),
            new(OpCodes.Ret)
            ]);

        if(retIndex == -1)
        {
            CruiserImproved.LogWarning("Failed to remove owner check from StartMagneting!");
        }
        else
        {
            codes.RemoveRange(retIndex, 4);
        }

        var collectItemsInTruck = PatchUtils.Method(typeof(VehicleController), "CollectItemsInTruck");
        var fixMagnet = PatchUtils.Method(typeof(VehicleControllerPatches), "FixMagnet");

        int index = PatchUtils.LocateCodeSegment(0, codes, [
            new(OpCodes.Call, collectItemsInTruck)
            ]);

        if(index == -1)
        {
            CruiserImproved.LogWarning("Failed to patch StartMagneting!");
        }

        var jumpLabel = il.DefineLabel();
        codes[index + 1].labels.Add(jumpLabel);

        codes.InsertRange(index + 1, [
            //call custom fixMagnet method
            new(OpCodes.Ldarg_0),
            new(OpCodes.Call, fixMagnet),
            new(OpCodes.Stloc_1),

            //return early if not owner
            new(OpCodes.Ldarg_0),
            new(OpCodes.Call, getIsOwner),
            new(OpCodes.Brtrue, jumpLabel),
            new(OpCodes.Ret),

            //return early if no localPlayerController yet to prevent a nullref when calling the rpc
            new(OpCodes.Call, PatchUtils.Method(typeof(GameNetworkManager), "get_Instance")),
            new(OpCodes.Ldfld, PatchUtils.Field(typeof(GameNetworkManager), "localPlayerController")),
            new(OpCodes.Call, typeof(UnityEngine.Object).GetMethod("op_Implicit")),
            new(OpCodes.Brtrue, jumpLabel),
            new(OpCodes.Ret)
            ]);

        return codes;
    }

    //fix radio not changing station for clients
    [HarmonyPatch("SetRadioStationClientRpc")]
    [HarmonyPostfix]
    static void SetRadioStationClientRpc_Postfix(VehicleController __instance)
    {
        __instance.SetRadioOnLocalClient(true, true);
    }

    //Set radio time consistently across owner and non-owners
    static void SetRadioTime(VehicleController instance)
    {
        instance.radioAudio.time = Mathf.Clamp(instance.currentSongTime % instance.radioAudio.clip.length, 0.01f, instance.radioAudio.clip.length - 0.1f);
    }

    [HarmonyPatch("SetRadioOnLocalClient")]
    [HarmonyPostfix]
    static void SetRadioOnLocalClient_Postfix(VehicleController __instance, bool on, bool setClip)
    {
        if(on && setClip)
        {
            SetRadioTime(__instance);
        }
    }

    [HarmonyPatch("SwitchRadio")]
    [HarmonyPostfix]
    static void SwitchRadio_Postfix(VehicleController __instance)
    {
        if (__instance.radioOn)
        {
            __instance.SetRadioStationServerRpc(__instance.currentRadioClip, (int)Mathf.Round(__instance.radioSignalQuality));
            SetRadioTime(__instance);
        }
    }

    //Rpc Args: NetworkObjectReference cruiserRef, float radioTime
    static public void SyncRadioTimeRpc(ulong clientId, FastBufferReader reader)
    {
        reader.ReadNetworkSerializable(out NetworkObjectReference cruiserRef);
        reader.ReadValue(out float radioTime);
        if (!cruiserRef.TryGet(out NetworkObject cruiserNetObj)) return;
        if (!cruiserNetObj.TryGetComponent(out VehicleController vehicle)) return;
        if (clientId != NetworkManager.ServerClientId) return;

        vehicle.currentSongTime = radioTime;
    }

    [HarmonyPatch("RemoveKeyFromIgnition")]
    [HarmonyPostfix]
    static public void RemoveKeyFromIgnition_Postfix(VehicleController __instance)
    {
        if (__instance.localPlayerInControl || __instance.currentDriver != null) return;

        //standing key removal if enabled and no one's driving

        if (!NetworkSync.Config.StandingKeyRemoval) return;

        if (__instance.keyIgnitionCoroutine != null)
        {
            __instance.StopCoroutine(__instance.keyIgnitionCoroutine);
        }
        __instance.keyIgnitionCoroutine = __instance.StartCoroutine(__instance.RemoveKey());
        __instance.RemoveKeyFromIgnitionServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
    }
}
