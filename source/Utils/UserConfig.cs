using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace CruiserImproved.Utils;

internal class UserConfig
{
    //General
    internal static ConfigEntry<bool> AllowLean;
    internal static ConfigEntry<bool> PreventMissileKnockback;
    internal static ConfigEntry<bool> AllowPushDestroyedCar;
    internal static ConfigEntry<bool> SilentCollisions;
    internal static ConfigEntry<float> SeatBoostScale;
    internal static ConfigEntry<bool> DisableRadioStatic;

    //Cruiser Health
    internal static ConfigEntry<float> CruiserInvulnerabilityDuration;
    internal static ConfigEntry<float> CruiserCriticalInvulnerabilityDuration;
    internal static ConfigEntry<int> MaxCriticalHitCount;

    //Physics
    internal static ConfigEntry<bool> AntiSideslip;

    //Host-side
    internal static ConfigEntry<bool> SyncSeat;
    internal static ConfigEntry<bool> PreventPassengersEjectingDriver;
    internal static ConfigEntry<bool> EntitiesAvoidCruiser;
    internal static ConfigEntry<bool> SortEquipmentOnLoad;
    internal static ConfigEntry<bool> SaveCruiserValues;

    internal static Dictionary<ConfigDefinition, ConfigEntryBase> ConfigMigrations;

    internal static void InitConfig()
    {
        ConfigFile config = CruiserImproved.Instance.Config;
        RetrieveOldConfigFile(config);

        config.SaveOnConfigSet = false;

        //General
        AllowLean = config.Bind("General", "Allow Leaning", true, "If true, allow the player to look backward out the window or through the cabin window.");
        PreventMissileKnockback = config.Bind("General", "Prevent Missile Knockback", true, "If true, prevent the player being ejected from seats by Old Bird missile knockback.");
        AllowPushDestroyedCar = config.Bind("General", "Allow Pushing Destroyed Cruisers", true, "If true, allow players to push destroyed cruisers.");
        SilentCollisions = config.Bind("General", "Silent Collisions", true, "If true, entities hitting the Cruiser when it's engine is off will not make noise.\nThis means Eyeless Dogs will not get stuck in a loop attacking it, triggering noise, and attacking it again while the engine is off.");
        DisableRadioStatic = config.Bind("General", "Disable Radio Static", false, "If true, disable the radio interference static sound on the radio.");


        AcceptableValueRange<float> seatScale = new(0f, 1f);
        SeatBoostScale = config.Bind("General", "Seat Boost Scale", 1.0f, new ConfigDescription("How much to boost the seat up? Set 0 to disable.", seatScale));

        //Cruiser Health
        AcceptableValueRange<float> invulnerableDuration = new(0f, 2f);
        CruiserInvulnerabilityDuration = config.Bind("Cruiser Health", "Cruiser Invulnerability Duration", 0.5f, new ConfigDescription("How long after taking damage is the Cruiser invulnerable for? Set 0 to disable.", invulnerableDuration));

        AcceptableValueRange<float> criticalInvulnerableDuration = new(0f, 6f);
        CruiserCriticalInvulnerabilityDuration = config.Bind("Cruiser Health", "Cruiser Critical Invulnerability Duration", 4.0f, new ConfigDescription("How long after critical damage (engine on fire) is the Cruiser invulnerable for? Set 0 to disable.", criticalInvulnerableDuration));


        AcceptableValueRange<int> criticalHitCount = new(0, 100);
        MaxCriticalHitCount = config.Bind("Cruiser Health", "Critical Protection Hit Count", 1, new ConfigDescription("Number of hits the Cruiser can block during the Critical Invulnerability Duration. \nIf the Cruiser receives this many hits while critical, it will emit a sound cue before exploding once the duration is up.\nIf 0, any hit that triggers the critical state will also trigger this delayed explosion.", criticalHitCount));

        //Physics
        AntiSideslip = config.Bind("Physics", "Anti-Sideslip", true, "If true, prevent the Cruiser from sliding sideways when on slopes.");

        //Host-side
        SyncSeat = config.Bind("Host-side", "Synchronise Seat Boost", false, "If true, set all other players using CruiserImproved in your lobbies to have the same Seat Boost Scale setting as you.\nAll other settings are always synchronised.");
        EntitiesAvoidCruiser = config.Bind("Host-side", "Entities Avoid Cruiser", true, "If true, entities will pathfind around stationary cruisers with no driver.\nEyeless Dogs will still attack it if they hear noise!");
        PreventPassengersEjectingDriver = config.Bind("Host-side", "Prevent Passengers Eject Driver", false, "If true, prevent anyone except the driver of the cruiser from using the eject button in your lobbies.");
        SortEquipmentOnLoad = config.Bind("Host-side", "Sort Equipment On Load", true, "If true, equipment and weapons will be separated from other scrap when items are moved out of the Cruiser on save load.\nThese items will be placed in a second pile in the center of the ship.");
        SaveCruiserValues = config.Bind("Host-side", "Save Cruiser Values", true, "If true, the Cruiser's turbo count, ignition state, and magnet position will be saved to/loaded from the save file.");

        MigrateOldConfigs(config);
        config.Save();
        config.SaveOnConfigSet = true;
    }

    //If the old config file still exists, rename and move to the new DiggC.CruiserImproved.cfg
    static void RetrieveOldConfigFile(ConfigFile config)
    {
        string oldConfigPath = Path.Combine(BepInEx.Paths.ConfigPath, "DiggC.CompanyCruiserImproved.cfg");
        if (File.Exists(oldConfigPath))
        {
            File.Copy(oldConfigPath, config.ConfigFilePath, true);
            File.Delete(oldConfigPath);
            config.Reload();
            CruiserImproved.LogMessage("Successfuly renamed old config file.");
        }
    }

    static void MigrateOldConfigs(ConfigFile config)
    {
        ConfigMigrations = new()
        {
            { new("General", "Prevent Passengers Eject Driver"), PreventPassengersEjectingDriver },
            { new("General", "Entities Avoid Cruiser"), EntitiesAvoidCruiser },
        };

        PropertyInfo orphanedEntriesProperty = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProperty.GetValue(config);
        foreach (var entry in orphanedEntries)
        {
            if (ConfigMigrations.TryGetValue(entry.Key, out var newConfig))
            {
                CruiserImproved.LogMessage("Migrated old config " + entry.Key + " : " + entry.Value);
                newConfig.SetSerializedValue(entry.Value);
            }
        }
        orphanedEntries.Clear();
    }
}