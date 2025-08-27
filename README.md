# Whisper Mountain Outbreak AssetPatcher

An asset patcher for the Survival Horror game **Whisper Mountain Outbreak**.
Made specifically for Resident Evil: Retribution.  

## Usage

While the patcher is made for RE: Retribution, it could be used for editing any Audio and texture/sprite from the game.
What you need to do is to add the file you want to add with the name of the asset you want to modify

- Example: bgm-lobby.ogg will replace the 'bgm-lobby' asset in the game.
- Example: head-default-0.png will replace the 'head-default-0' sprite.

For getting all the asset names, i'll leave it up to you to find it yourself with a tool like [AssetRipper](https://github.com/AssetRipper/AssetRipper)
SUPPORTED FORMATS:
Audio:

- .ogg
- .wav
- .mp3 (not recommended)

Sprites/Textures:

- .png
- .jpg
- .jpeg


After placing your mod files here, run the patcher to apply them to the game.

## Build & Compile

### Prerequisites

- [.NET SDK 7.0+](https://dotnet.microsoft.com/download)

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

- Make sure to back up your game files before patching.
- For issues or contributions, please open an issue or pull request.
