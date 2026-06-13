## 1.6.1

## Fixes
- \[Client] Re-wrote a transpiler to prevent an issue when using mods such as AsyncLoggers, which would effectively brick the vehicles collision trigger.

## 1.6.0

## Changes
- \[Client] The Scan-while-seated is now vehicle-ID checked to allow custom vehicles such as the Company Hauler or the ScanVan to define their own scanning logic.

## 1.5.9

## Hotfix
- \[Client/Host] Fixed some transpiler issues. Additionally fixed ItemFallThroughPatch for real.

## 1.5.8

## Hotfix
- \[Host] Fixed null-ref spam caused by a radio patch on custom vehicles such as the Road-Rage by Boom. Apologies.

## 1.5.7

## Hotfix
- \[Client] Fixed vehicle being catapulted into the atmosphere never to be seen again caused by 1.5.6. Sorry for the mild inconvience.

## 1.5.6

## Bugfixes
- \[Host] Fixed sort-equipment-on-load not working.
- \[Host] Fixed radio not making audible alerts. (thanks to [Buttery Stancakes](https://github.com/ButteryStancakes))
- \[Client] Fixed a null-ref on VehicleCollisionTrigger. (thanks to [Buttery Stancakes](https://github.com/ButteryStancakes))
- \[Client] Added more Vehicle-ID checks to prevent messing with custom vehicles.
- \[Client] Fixed player steering wheel turning animation being reversed. (thanks to [Buttery Stancakes](https://github.com/ButteryStancakes))
- \[Client] Fixed a targeted transpiler error introduced in [v80+].

## 1.5.5

## Bugfixes/Hotfix
- \[Client] Reverted suspension parameters that were introduced in 1.5.3.

## 1.5.4

## Hotfix
- \[Client] Fixed passenger seat collisions.

## 1.5.3

### Compatibility

- CruiserImproved should now be compatible with LCVR (?) (Untested, any issues let me know. - Scandal)

### Bugfixes

- \[Host&Client\] Fixed driven wheel torques not being synced.
- \[Client] Fixed an issue with ignition parameters not being set properly on clients.
- \[Client] Fixed an issue where high speed impacts would potentially desynch enemy health.

## 1.5.2

### Bugfixes

- \[Host&Client\] Fixed tyre skid/tyre spark effects not syncing across clients.
- \[Client] Fixed an issue, again, with wheels not rotating on non-owners.

## 1.5.1

Updated compatibility for v70+

### Bugfixes

- \[Host] Tweaks sort equipment on load to run after a delay, to prevent items from floating out of bounds on reload [v70+] (thanks to [Buttery Stancakes](https://github.com/ButteryStancakes))
- \[Client] Fixed issue with certain materials (broken windshield, headlamp on/off material) not being applied to lower-level-of-detail models.
- \[Client] Fixed issues with wheels not using the rotation of the corresponding collider, resulting in gimbal lock and unnatural behaviour.
- \[Client?/Host?] Moved a localPlayerController check on StartMagneting to before the isOwner check, should hopefully prevent a nullref error.

### Compatibility  

- Consolidated VehicleID checks & added checks to vehicle collision trigger in order to prevent modifying LLL-Vehicles.
- CruiserImproved is no longer compatible with LCVR, however this will be investigated.

## 1.5.0

Updated compatibility for v69.

### Features
- \[Client\] Ability to toggle the Cruiser's cabin light by interacting with it.
- \[Client\] Ability to scan while seated in the Cruiser (thanks to [Buttery Stancakes](https://github.com/ButteryStancakes))

### Bugfixes
- \[Client\] Fix 'ghosting' of interactable prompts across the Cruiser's windshield.

### Compatibility
- Fixed an issue causing the player's camera to be modified when not seated in the Cruiser, which was breaking compatibility with some mods that affect the camera (thanks to [WinterMantis](https://github.com/WinterMantis))
- Added checks to make sure only the vanilla Cruiser is affected by most of CruiserImproved's changes. This should improve compatibility with mods that add new vehicles.

## 1.4.1

Full compatibility for v64. Should still be compatible with v56.

### Bugfixes
- \[Client\] Fix wheel skidding sound effect and visuals playing while the Cruiser is airborne or magneted.
- \[Client\] Fix Cruiser accelerating while attached to the magnet if not in park.

### Other
- Fixed an issue since v60 where Old Birds could not damage the Cruiser.
- Fixed an issue where the Cruiser would be facing the wrong way when not loading a saved position.

## 1.4.0

### Features
- \[Client\] Ability to open the Cruiser's doors while holding a 2-handed item.
- \[Client\] Ability remove the key from the ignition while not seated.
- \[Client\] Added a scan node to the Cruiser to help locate it from far away. Can be configured to show turbos and health.
- \[Client\] Cruiser exhaust is tinted blue when it has stored turbo charges.

### Bugfixes
- \[Client\] Fix items dropping through the Cruiser when standing in the back.
- \[Client\] Fix some parts of the Cruiser like the driver seat and exhaust smoke remaining after destruction.
- \[Client\] Fix items being unscannable when placed in the Cruiser.

### Other
- Fixed an issue where a player could rarely be run over while driving a tilted Cruiser that was supposed to be fixed in v1.0.3.
- Fixed a null reference when a Cruiser was left behind or despawned.
- Fixed Cruiser bouncing off unkillable enemies that it'd usually push in vanilla, like bees.

### Compatibility
- Added compatibility for [LethalCompanyVR](https://thunderstore.io/c/lethal-company/p/DaXcess/LethalCompanyVR/)

## 1.3.0

### Features
- \[Host\] Equipment and weapons moved into the ship from the Cruiser on save reload is sorted into a separate pile from the rest of the scrap.
- \[Host\] Magnet position, turbo boosts, and ignition state are saved and restored when loading a save, preventing lost turbos and removing the need to restart the ignition.
- \[Client\] Option to disable the Cruiser's radio interference static. Disabled by default.

### Bugfixes
- \[Host&Client\] Fix radio desync between players.
- \[Host\] Fix items left floating where the Cruiser was when reloading a save. All items will now be moved into the ship instead of just some.

## 1.2.2

- Fixed null reference when syncing vehicle features. This should fix issues after reloading a save with a Cruiser in play.
- Fixed null reference exception logged in StartMagneting when hosting a game.
- Fixed null reference when hitting the eject button on a Cruiser with no driver (thanks to [1A3Dev](https://github.com/1A3Dev))

## 1.2.1

- Fixed issue from 1.2.0 preventing the lean feature from working on newly purchased Cruisers as client.

## 1.2.0

Added network sync. Clients will copy the host's config settings if they also have CruiserImproved.

### Bugfixes
- \[Host&Client\] Fix steering wheel position not synchronizing between players.
- \[Client\] Fix weedkiller being able to shrink the Cruiser's bounding box.

### Other
- Fixed the magnet almost always attaching the Cruiser fully nose-down.
- Weedkiller can now prevent an imminent Cruiser explosion when critically damaged during the CriticalInvulnerabilityDuration.

## 1.1.1

### Bugfixes
- \[Client\] Fix clients seeing a saved Cruiser in the wrong spot when connecting.
- \[Client\] Fix sliding off the Cruiser while the ship is taking off or landing.
- \[Client\] Fix being abandoned when standing on or sitting in a magneted Cruiser while the ship is taking off.
- \[Client\] Fix ship magnet attaching the Cruiser in the wrong position or rotation (will not stick frontfirst into the ship)

### Other
- Improve driver and passenger exit check to not drop players off edges.
- Fix Cruisers that existed at save load not playing the magnet sound effect.

## 1.1.0

### Features
- \[Host\] Entities pathfind around stationary Cruisers with no one seated, instead of walking straight through and causing damage.
- Prevent the Cruiser damage sound being detected by dogs if the engine is off, preventing them from repeatedly attacking Cruisers due to the sound they cause.


### Bugfixes
- \[Client\] Fixed passengers being able to exit the Cruiser into walls, which would often launch both the player and the Cruiser.
- \[Client\] Improved driver and passenger exit space check. Player will be placed into the seat instead of outside if there is little or no room, such as when magneted to the ship.

### Other
- Fixed issue from 1.0.3 that could prevent entities taking damage when run over

## 1.0.3

### Features
- Prevent the Cruiser from sliding sideways down slopes (ie, when dropped off the magnet on Artifice).
- \[Host\] Prevent anyone in the lobby other than the driver pressing the eject button on Cruisers. Disabled in the config by default.

### Bugfixes
- \[Client\] Fixed Cruiser colliding with players standing on it, or colliding with entities standing in the back.

## 1.0.2

### Bugfixes
- \[Client\] Fixed Baboon Hawks requiring very high speed to run over
- \[Client\] Fixed controls continuing to work in the Cruiser while typing in chat or the pause menu is open.
- \[Client\] Fixed slow collisions dealing 2 damage to entities clientside (thanks to [Buttery Stancakes](https://github.com/ButteryStancakes))

### Other
- Removed patch for entities dying clientside when run over (this is now fixed in vanilla)

## 1.0.1

### Features
- Added the ability to push destroyed Cruisers

### Other
- Improved critical damage protection to block a limited number of hits before exploding after the protection wears off, configured by `Critical Protection Hit Count`

## 1.0.0

- Initial release
