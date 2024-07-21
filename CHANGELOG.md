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
