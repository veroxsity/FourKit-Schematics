# FourKit-Schematics

Schematic capture-and-paste plugin for FourKit servers running Minecraft Legacy Console Edition. Save a region of the world to disk and paste it back anywhere later. Useful for arena building, prefab structures, and backing up builds before destructive testing.

## Installation

1. Build (see below) or grab the latest `Schematics.dll` from Releases
2. Drop it into `<server>/plugins/`
3. Restart the server

## Workflow

1. `/schem pos1` standing at corner A
2. `/schem pos2` standing at corner B
3. `/schem save <name>` â†’ captures the AABB to `plugins/Schematics-data/<name>.schem`
4. Later, anywhere: `/schem load <name>` â†’ pastes from your current position

## Commands

| Command | Description |
|---|---|
| `/schem pos1` | Mark corner A at your current block position |
| `/schem pos2` | Mark corner B at your current block position |
| `/schem save <name>` | Save the selection to disk |
| `/schem load <name>` | Paste the named schematic from your current position |
| `/schem list` | List saved schematics |
| `/schem delete <name>` | Remove a schematic from disk |
## Building from source

Requires .NET 10 SDK.

```powershell
.\build.ps1 -StopServer
```

The script auto-stops a running `Minecraft.Server.exe`, builds in Release mode, and copies the DLL to `..\..\Server\plugins\`. Or build manually:

```powershell
dotnet build -c Release
```

## License

MIT
