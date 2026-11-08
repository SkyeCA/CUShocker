# CUShocker

Shocks your OpenShock shockers when you get hurt in Casualties Unknown.

There are two parts:

- `src/CUShocker.Mod` - BepInEx plugin. Reads the player's pain and sends it over UDP to localhost.
- `src/CUShocker.Module` - OpenShock Desktop module. Receives the pain and sends the shocks. Settings live here.

Vibrate is implemented but turned off for now (`Features.Vibrate`).

## Requirements

- Casualties Unknown with BepInEx 5
- OpenShock Desktop 1.1.4+
- .NET 10 SDK

## Building

```
dotnet build
dotnet test
```

Debug builds copy the mod into the game's `BepInEx\plugins\CUShocker` and the module into `%APPDATA%\OpenShock\Desktop\modules\my.vore.cushocker`. The game path defaults to the Steam install, set `CU_PATH` if yours is elsewhere. Close the game and OpenShock Desktop first or the copy fails.

Debug builds also have a pain simulator on the module page for testing without the game.

## Releasing

```
powershell -ExecutionPolicy Bypass -File package.ps1
```

Outputs to `dist\`. `CUShocker-<version>.zip` is the one for players. The `.module.zip` is Desktop's own package format, for if it ever goes in a module repo.

## Protocol

One JSON object per UDP packet to `127.0.0.1:37412`, sent on change and at least once a second.

```json
{"v":1,"type":"state","session":"…","seq":42,"inWorld":true,"alive":true,"conscious":true,"averagePain":12.5}
```

`session` is random per game launch and `seq` counts up within it, so old packets get dropped. The module treats the game as gone after 3 seconds without anything.
