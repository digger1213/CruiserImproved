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
