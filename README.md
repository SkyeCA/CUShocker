# CUShocker

Connects the pain your character feels in Casualties Unknown to OpenShock shockers.

It has two parts:

- **The mod** (`src/CUShocker.Mod`): a BepInEx plugin that reads the player's condition and sends it to `127.0.0.1` over UDP. It has no settings beyond the port, and never talks to OpenShock.
- **The module** (`src/CUShocker.Module`): an [OpenShock Desktop](https://github.com/OpenShock/Desktop) module that receives those messages, applies your limits and filters, and controls your shockers. OpenShock Desktop handles your login and API token.

> **Status:** project skeleton. Both parts build and install, but they don't do anything yet.

## Requirements

- Casualties Unknown with BepInEx 5
- OpenShock Desktop 1.1.4 or later, logged in
- .NET 10 SDK to build

## Building

```
dotnet build
dotnet test
```

A build also installs both parts:

- The mod is copied to `<game>\BepInEx\plugins\CUShocker\`. The game folder defaults to the Steam install location. To use another one, set the `CU_PATH` environment variable or build with `-p:GamePath=<game folder>`. While the game is running the DLL is locked, so the copy is skipped with a warning.
- Debug builds of the module are copied to `%APPDATA%\OpenShock\Desktop\modules\dev.skyeca.cushocker\`. Restart OpenShock Desktop to load a new build.

## Layout

| Path | What it is |
|---|---|
| `src/Shared/Protocol.cs` | The message format. Compiled into both the mod and the module, so they can't drift apart |
| `src/CUShocker.Mod` | BepInEx plugin, .NET Framework 4.8 |
| `src/CUShocker.Module` | OpenShock Desktop module, .NET 10 + Blazor/MudBlazor |
| `tests/CUShocker.Tests` | Tests for the message format |

## Message format (protocol v1)

The mod sends one JSON object per UDP datagram to `127.0.0.1:37412`. It sends when the player's state changes, and at least once a second as a heartbeat. If no message arrives for 3 seconds, the module treats the game as disconnected.

```json
{"v":1,"type":"state","session":"…","seq":42,"inWorld":true,"alive":true,"conscious":true,"averagePain":12.5}
```

| Field | Meaning |
|---|---|
| `v` | Protocol version. The receiver ignores versions it doesn't know |
| `type` | Always `state` for now |
| `session` | Random ID chosen when the game starts; `seq` starts again for each new session |
| `seq` | Increases with every message, so stale or out-of-order datagrams can be dropped |
| `inWorld` | `false` in menus and loading screens, where there is no player |
| `alive`, `conscious` | The player's `Body.alive` and `Body.conscious` |
| `averagePain` | The player's `Body.averagePain`; `0` when not in the world |
