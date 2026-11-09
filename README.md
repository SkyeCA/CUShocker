# CUShocker

This is a mod to connect your OpenShock shockers to Casualties Unknown and extra motivation to keep your Expie safe!

If your Expie gets hurt? So do you!

## Requirements

- Casualties Unknown with BepInEx 5
- OpenShock Desktop 1.1.4+

## Building

Note: You will need the dotnet 10 SDK to build this mod.

```
dotnet build
dotnet test
```

Debug builds copy the mod into the `BepInEx\plugins\CUShocker` game directory and the OpenShock module into the `%APPDATA%\OpenShock\Desktop\modules\my.vore.cushocker` directory. The game path defaults to the Steam install, so make sure to set `CU_PATH` if you aren't running via Steam. Make sure both the game and OpenShock Desktop are closed before doing a build.

## Releasing

You can build releasable version with the following command:

```
powershell -ExecutionPolicy Bypass -File package.ps1
```

## Installing

Download the most recent version from the releases page, unzip it, and follow the instructions in the `README.txt` file.

## Disclaimer

**USE AT YOUR OWN RISK**: This software is provided "as is," without warranty of any kind. The author makes no guarantees regarding the safety, reliability, or accuracy of this software. By using this software, you assume full responsibility for any unintended effects, injuries, or damages that may occur.

## AI

AI was used for unit testing, generating build scripts, generating the `.gitignore`, and code review.