# CruiserImproved
 A clientside mod to improve the behaviour of Lethal Company's 'Company Cruiser' and fix some issues with its usage such as low visibility, high damage received from small impacts, and fix bugs.

### Features
All features can be configured or disabled in the generated .cfg file in BepInEx/config.
- Ability to lean to look back around the side of the truck and through the small window by turning the camera around.
- Increased seat height for better visibility over the Cruiser's hood.
- Slight invulnerability for the Cruiser after taking damage to reduce the damage taken by rolling or multi-impacts at low speed.
- Short invulnerability for the Cruiser when critically damaged (engine on fire) allowing players to react and escape before the Cruiser explodes.
- Prevent knockback from Old Bird missiles knocking players out of the seat.
- Ability to push destroyed Cruisers.
- Prevent the Cruiser from sliding sideways down slopes (ie, when dropped off the magnet on Artifice).
- Prevent the Cruiser damage sound being detected by dogs if the engine is off, preventing them from repeatedly attacking Cruisers due to the sound they cause.
- \[Host\] Prevent anyone in the lobby other than the driver pressing the eject button on Cruisers. Disabled in the config by default.
- \[Host\] Entities pathfind around stationary Cruisers with no one seated, instead of walking straight through and causing damage.

### Bugfixes
- \[Host\] Prevent the gas pedal or brake pedal from being stuck down if the player leaves the Cruiser while holding them.
- \[Client\] Fix small entities (anything except Eyeless Dog, Kidnapper Fox, Forest Giant, Old Bird) being impossible to run over.
- \[Client\] Fix steering wheel visually desyncing from the actual steering angle.
- \[Client\] Fixed Baboon Hawks requiring very high speed to run over.
- \[Client\] Fixed controls continuing to work in the Cruiser while typing in chat or the pause menu is open.
- \[Client\] Fixed slow collisions dealing 2 damage to entities clientside (thanks to [Buttery Stancakes](https://github.com/ButteryStancakes))
- \[Client\] Fixed Cruiser colliding with players standing on it, or colliding with entities standing in the back.
- \[Client\] Fixed passengers being able to exit the Cruiser into walls, which would often launch both the player and the Cruiser.
- \[Client\] Improved driver and passenger exit space check. Player will be placed into the seat instead of outside if there is little or no room, such as when magneted to the ship.
- \[Client\] Fix clients seeing a saved Cruiser in the wrong spot when connecting.
- \[Client\] Fix sliding off the Cruiser while the ship is taking off or landing.
- \[Client\] Fix being abandoned when standing on or sitting in a magneted Cruiser while the ship is taking off.
- \[Client\] Fix ship magnet attaching the Cruiser in the wrong position or rotation (will not stick frontfirst into the ship)

### Feedback

Feel free to post suggestions or issues to:

- [Github Issues](https://github.com/digger1213/CruiserImproved/issues)
- The mod's [thread](https://discord.com/channels/1168655651455639582/1258980772996448309) in the [LC Modding discord server](https://discord.gg/XeyYqRdRGC)

### Compatibility
Compatible with V56.

Works well with [BetterVehicleControls](https://thunderstore.io/c/lethal-company/p/Dev1A3/BetterVehicleControls/).
