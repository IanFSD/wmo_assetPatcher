# Whisper Mountain Outbreak AssetPatcher

A Mod loader for the Survival Horror game **Whisper Mountain Outbreak**.
Made specifically for Resident Evil: Retribution.  

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
