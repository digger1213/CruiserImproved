# CruiserImproved

[![Thunderstore Version](https://img.shields.io/thunderstore/v/DiggC/CruiserImproved?style=for-the-badge&logo=thunderstore)](https://thunderstore.io/c/lethal-company/p/DiggC/CruiserImproved/)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/DiggC/CruiserImproved?style=for-the-badge)](https://thunderstore.io/c/lethal-company/p/DiggC/CruiserImproved/)\
[![GitHub Version](https://img.shields.io/github/v/release/digger1213/CruiserImproved?include_prereleases&sort=semver&style=for-the-badge&logo=github)](https://github.com/digger1213/CruiserImproved/releases)
[![GitHub Build](https://img.shields.io/github/actions/workflow/status/digger1213/CruiserImproved/build.yml?branch=main&style=for-the-badge)](https://github.com/digger1213/CruiserImproved/actions/workflows/build.yml)

A clientside [BepInEx](https://docs.bepinex.dev/) mod for Lethal Company to improve the behaviour of the 'Company Cruiser' and fix various bugs and issues with it's vanilla implementation.

For more details on the features, bugfixes and mod compatibility of this mod, check out the [Thunderstore page](https://thunderstore.io/c/lethal-company/p/DiggC/CruiserImproved/) or the [Thunderstore readme](THUNDERSTORE.md).

Feel free to post any suggestions or issues to the [issues](https://github.com/digger1213/CruiserImproved/issues) or on the [LC Modding discord server](https://discord.gg/XeyYqRdRGC) in the mod's [thread](https://discord.com/channels/1168655651455639582/1258980772996448309).

# Install
The recommended way to install CruiserImproved is through the [Thunderstore page](https://thunderstore.io/c/lethal-company/p/DiggC/CruiserImproved/) using a mod manager like [r2modman](https://thunderstore.io/package/ebkr/r2modman/). 

You can also manually install the mod. [Install BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html) first, then download the [latest version of CruiserImproved](releases/latest) from the Releases. Finally, copy the BepInEx folder in the mod download into your game directory.

# Compile
Ensure you have installed [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or higher.

You may optionally copy `CruiserImproved.template.props.user` to `CruiserImproved.props.user` and specify the path to your Lethal Company mod profile or game directory in it. If this file exists and a profile directory is specified, building the project will automatically install the mod to the profile.

In the repository directory, run the following commands:
```
dotnet tool restore
dotnet build -c Release
```
The built mod will be located in `source/bin` and installed to the profile optionally specified in `CruiserImproved.props.user`.