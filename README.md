# Whisper Mountain Outbreak AssetPatcher

An asset patcher for the Survival Horror game **Whisper Mountain Outbreak**.
Made specifically for Resident Evil: Retribution.  

## Features

- **Asset Replacement**: Replace audio, sprites, and textures
- **Manifest System**: SMAPI-style mod organization with `manifest.json`
- **BepInEx Integration**: Automatic installation and plugin support
- **Steam Integration**: Launch game directly through Steam

## Usage

### Manifest-Based Mods (Recommended)

Create a mod folder with a `manifest.json` file for better organization and control:

```
mods/
└── YourMod/
    ├── manifest.json
    ├── audio/
    │   └── custom_music.ogg
    └── sprites/
        └── custom_sprite.png
```

**Example manifest.json:**
```json
{
  "Name": "Your Mod Name",
  "UniqueID": "Author.YourMod",
  "Version": "1.0.0",
  "Author": "YourName",
  "Description": "What your mod does",
  "ContentFiles": [
    {
      "FilePath": "audio/custom_music.ogg",
      "Target": "bgm-lobby",
      "Type": "Audio"
    },
    {
      "FilePath": "sprites/custom_sprite.png",
      "Target": "head-default-0",
      "Type": "Sprite"
    }
  ]
}
```

See [mods/MANIFEST_EXAMPLE.md](mods/MANIFEST_EXAMPLE.md) for complete documentation and examples.

### Finding Asset Names

Use a tool like [AssetRipper](https://github.com/AssetRipper/AssetRipper) to browse the game's assets and find the exact names you want to replace.

## Supported File Types

**Audio:**
- .ogg (recommended)
- .wav
- .mp3 (not recommended)

**Sprites/Textures:**
- .png (recommended)
- .jpg / .jpeg

**Code Mods:**
- .dll (BepInEx plugins)

## BepInEx Plugin Support

The patcher automatically installs BepInEx on first run. You can include BepInEx plugins in your mods:

**With manifest:**
```json
{
  "Name": "My Plugin Mod",
  "UniqueID": "Author.PluginMod",
  "Version": "1.0.0",
  "BepInExPlugins": [
    "plugins/MyPlugin.dll"
  ]
}
```

**Legacy:** Simply place `.dll` files in your mod folder - they will be automatically detected and installed to `BepInEx/plugins`.

## Restoration

To remove changes and restore the original game:
1. Open Steam
2. Right-click the game → Properties
3. Go to "Installed Files" → "Verify integrity of game files"

## Build & Compile

### Prerequisites

- [.NET SDK 9.0+](https://dotnet.microsoft.com/download)

1. **Clone the repository:**

    ```bash
    git clone https://github.com/IanFSD/wmo_assetPatcher.git
    cd wmo_assetPatcher
    ```

2. **Restore dependencies:**

    ```bash
    dotnet restore
    ```

3. **Build the project:**

    ```bash
    dotnet build --configuration Release
    ```

## Notes

- Make sure to deactivate Steam Cloud for the game
- You can undo changes by verifying files on Steam
- The patcher will automatically install BepInEx on first run
- For issues or contributions, please open an issue or pull request
