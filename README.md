# FourKit-Schematics

Schematic save/load plugin for FourKit servers running Minecraft Legacy Console Edition. Capture a region of blocks to a named file and paste it back anywhere later.

## Features

- Save a selected region (uses SimpleEdit's `//pos1`/`//pos2` selection) to a named schematic file
- Paste a saved schematic at your current location
- List, delete, and inspect saved schematics
- Block IDs and data values preserved verbatim

## Installation

```powershell
.\build.ps1 -StopServer
```

Recommended: install FourKit-SimpleEdit alongside this plugin for region selection. Schematics relies on SimpleEdit's selection system to know what to save.

## Commands

- `/schem save <name>` - save the current selection to `<name>.schem`
- `/schem load <name>` - load a saved schematic into memory (clipboard)
- `/schem paste` - paste the loaded schematic at your current location
- `/schem list` - list saved schematics with dimensions
- `/schem delete <name>` - remove a schematic
- `/schem info <name>` - dimensions, block count, file size

## Storage

Schematic files are saved in `plugins/Schematics-data/` as binary blobs containing a small header plus block ID and data byte arrays. Files are portable across servers running this plugin.

## License

MIT
