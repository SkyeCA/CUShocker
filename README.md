# CUShocker

This is a mod to connect your OpenShock or PiShock shockers to Casualties Unknown and extra motivation to keep your Expie safe!

If your Expie gets hurt? So do you!

## Links

[Nexus Mod Page](https://www.nexusmods.com/scavprototype/mods/879)

## Requirements

- Casualties Unknown with BepInEx 5
- [OpenShock Desktop](https://openshock.org/desktop) 1.3.1+ if using OpenShock
- [MultiShock](https://mshock.akiradev.me/) 4.8.3+ if using PiShock

## Building

Note: You will need the dotnet 10 SDK to build this mod.

```
dotnet build
dotnet test
```

Build Notes: 

- The game path defaults to the Steam install, so make sure to set `CU_PATH` if you aren't running via Steam.
- For dev testing add the arg `-p:Simulator=true` when building to include a pain simulator in the OpenShock/Multishock module
- Make sure the game, OpenShock Desktop, and MultiShock are closed before doing a build.

## Releasing

You can build releasable version with the following command:

```
powershell -ExecutionPolicy Bypass -File package.ps1
```

## Installing

Download the most recent version from the releases page, unzip it, and follow the instructions in the `README.txt` file.

## Disclaimer

**USE AT YOUR OWN RISK**: This software is provided "as is," without warranty of any kind. I make no guarantees regarding the safety, reliability, or accuracy of this software. By using this software, you assume full responsibility for any unintended effects, injuries, or damages that may occur.

## AI

AI was used for unit testing, generating build scripts, generating the `.gitignore`, and code review.