using WMO.Core.Logging;
using WMO.Core.Services;
using WMO.Core.Helpers;
using WMO.Core.Patching;
using WMO.UI.Forms;
using WMO.Core.Models;
using WMO.Core.Models.Enums;
using WMO.UI.ViewModels;

namespace WMO.UI;

/// <summary>
/// Main application form with tabbed interface
/// </summary>
public partial class MainForm : Form
{
    private readonly FolderModService _folderModService = new();
    private readonly AssetScannerService _assetScannerService = new();
    private bool _isPatchingInProgress = false;
    private CancellationTokenSource? _scanCancellationSource;

    public MainForm()
    {
        InitializeComponent();
        InitializeForm();
        LoadMods();
        InitializeAssetScanner();
    }

    private void InitializeForm()
    {
        // Set form properties
        this.Text = "WMO Asset Patcher";
        this.StartPosition = FormStartPosition.CenterScreen;
        
        // Load window size from settings
        var settings = SettingsService.Current;
        if (settings.RememberWindowSize)
        {
            this.Size = new Size(settings.WindowWidth, settings.WindowHeight);
        }
        else
        {
            this.Size = new Size(900, 700);
        }
        
        this.MinimumSize = new Size(800, 600);
        
        // Set up event handlers
        this.FormClosing += MainForm_FormClosing;
        this.SizeChanged += MainForm_SizeChanged;
        
        // Initialize settings controls
        InitializeSettingsControls();
        
        // Update status
        UpdateGamePathStatus();
    }

    private void InitializeSettingsControls()
    {
        var settings = SettingsService.Current;
        
        // Populate log level combo box
        cmbLogLevel.Items.Clear();
        foreach (LogLevel level in Enum.GetValues<LogLevel>())
        {
            cmbLogLevel.Items.Add(level);
        }
        
        // Populate game version combo box
        cmbGameVersion.Items.Clear();
        cmbGameVersion.Items.Add("Full Game");
        cmbGameVersion.Items.Add("Friend's Pass");
        
        // Bind settings to controls
        LoadSettingsToControls();
        
        // Set up event handlers for settings controls
        txtGamePath.TextChanged += TxtGamePath_TextChanged;
        btnBrowseGamePath.Click += BtnBrowseGamePath_Click;
        cmbGameVersion.SelectedIndexChanged += CmbGameVersion_SelectedIndexChanged;
        cmbLogLevel.SelectedIndexChanged += CmbLogLevel_SelectedIndexChanged;
        chkRememberWindowSize.CheckedChanged += ChkRememberWindowSize_CheckedChanged;
        chkDarkMode.CheckedChanged += ChkDarkMode_CheckedChanged;
    }

    private void LoadSettingsToControls()
    {
        var settings = SettingsService.Current;
        
        txtGamePath.Text = settings.GamePath ?? "";
        cmbGameVersion.SelectedIndex = (int)settings.GameVersion;
        cmbLogLevel.SelectedItem = settings.LogLevel;
        chkRememberWindowSize.Checked = settings.RememberWindowSize;
        chkDarkMode.Checked = settings.DarkMode;
    }

    private async void LoadMods()
    {
        try
        {
            Logger.Log(LogLevel.Info, $"Loading available folder mods...");
            
            await _folderModService.RefreshModsAsync();
            
            Logger.Log(LogLevel.Info, $"Loaded {_folderModService.AvailableMods.Count} mods total");
            
            // Update the mods list in UI
            UpdateModsList();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error loading mods: {ex.Message}");
            MessageBox.Show($"Error loading mods: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateModsList()
    {
        lstMods.Items.Clear();
        
        foreach (var folderMod in _folderModService.AvailableMods)
        {
            var item = new ListViewItem(folderMod.Name)
            {
                Tag = folderMod,
                Checked = folderMod.IsEnabled
            };
            
            item.SubItems.Add(folderMod.TypesSummary);
            item.SubItems.Add($"{folderMod.FileCount} files ({folderMod.FormattedTotalSize})");
            
            lstMods.Items.Add(item);
        }
        
        lblModCount.Text = $"Mods found: {_folderModService.AvailableMods.Count}";
    }

    private void UpdateGamePathStatus()
    {
        var settings = SettingsService.Current;
        var gamePath = settings.GamePath;
        
        if (string.IsNullOrEmpty(gamePath))
        {
            lblGamePathStatus.Text = "⚠ No game path configured";
            lblGamePathStatus.ForeColor = Color.Orange;
            btnPatchGame.Enabled = false;
            btnLaunchGame.Enabled = false;
        }
        else if (GamePathService.ValidateGamePath(gamePath))
        {
            var gameVersionText = settings.GameVersion == GameVersion.FullGame ? "Full Game" : "Friend's Pass";
            lblGamePathStatus.Text = $"✓ {gameVersionText} (Steam ID: {settings.SteamAppId}) - {gamePath}";
            lblGamePathStatus.ForeColor = Color.Green;
            btnPatchGame.Enabled = !_isPatchingInProgress && _folderModService.AvailableMods.Any(m => m.IsEnabled);
            btnLaunchGame.Enabled = !_isPatchingInProgress;
        }
        else
        {
            lblGamePathStatus.Text = "✗ Invalid game path";
            lblGamePathStatus.ForeColor = Color.Red;
            btnPatchGame.Enabled = false;
            btnLaunchGame.Enabled = false;
        }
    }

    private async void btnPatchGame_Click(object sender, EventArgs e)
    {
        if (_isPatchingInProgress) return;
        
        try
        {
            // Get selected mods
            var selectedMods = _folderModService.AvailableMods.Where(m => m.IsEnabled).ToList();
            if (!selectedMods.Any())
            {
                MessageBox.Show("No mods selected for patching.", "No Mods Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Always show confirmation before patching
            var totalFiles = selectedMods.Sum(m => m.FileCount);
            var result = MessageBox.Show(
                $"This will patch the game with {selectedMods.Count} selected mod(s) containing {totalFiles} files. " +
                "The operation will create backups of original files before making changes.\n\n" +
                "Do you want to continue?",
                "Confirm Patching",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (result != DialogResult.Yes)
                return;

            _isPatchingInProgress = true;
            UpdateGamePathStatus();
            
            // Update status for selected mods
            foreach (var mod in selectedMods)
            {
                mod.Status = "Preparing...";
            }
            
            // Allocate console window for patching output
            var settings = SettingsService.Current;
            ConsoleService.AllocateConsole("WMO Asset Patcher - Patching Progress");
            ConsoleService.WriteHeader("Starting Game Patching Process", ConsoleColor.Green);
            
            // Enable console output temporarily
            var originalConsoleOutput = settings.ConsoleOutput;
            settings.ConsoleOutput = true;
            
            // Run patching in background
            bool success = false;
            await Task.Run(() =>
            {
                try
                {
                    success = AssetPatcher.TryPatch(settings.GamePath!);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Patching failed with exception: {ex.Message}");
                    success = false;
                }
            });
            
            // Update status for all mods (don't remove them from list!)
            foreach (var mod in selectedMods)
            {
                mod.Status = success ? "Patched" : "Failed";
            }
            
            // Restore original console setting
            settings.ConsoleOutput = originalConsoleOutput;
            
            // Show completion in console
            if (success)
            {
                ConsoleService.WriteHeader("Patching Completed Successfully!", ConsoleColor.Green);
                ConsoleService.WriteColoredMessage("All mods have been applied successfully. You can close this window.", ConsoleColor.Green);
            }
            else
            {
                ConsoleService.WriteHeader("Patching Failed!", ConsoleColor.Red);
                ConsoleService.WriteColoredMessage("Some errors occurred during patching. Check the log messages above for details.", ConsoleColor.Red);
            }
            
            ConsoleService.WriteColoredMessage("\nPress any key to close this window...", ConsoleColor.Yellow);
            
            // Wait for user input in a background task so UI remains responsive
            _ = Task.Run(() =>
            {
                try
                {
                    Console.ReadKey(true);
                    this.Invoke(() => ConsoleService.FreeConsoleWindow());
                }
                catch
                {
                    // Ignore errors if console is already closed
                }
            });
            
            // Show result message in main UI
            if (success)
            {
                MessageBox.Show("Game patching completed successfully!", "Patching Complete", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Game patching failed. Check the console output for details.", "Patching Failed", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error during patching: {ex.Message}");
            ConsoleService.WriteHeader("Critical Error!", ConsoleColor.Red);
            ConsoleService.WriteColoredMessage($"A critical error occurred: {ex.Message}", ConsoleColor.Red);
            ConsoleService.WriteColoredMessage("\nPress any key to close this window...", ConsoleColor.Yellow);
            
            // Wait for user input in error case too
            _ = Task.Run(() =>
            {
                try
                {
                    Console.ReadKey(true);
                    this.Invoke(() => ConsoleService.FreeConsoleWindow());
                }
                catch
                {
                    // Ignore errors if console is already closed
                }
            });
            
            MessageBox.Show($"Error during patching: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isPatchingInProgress = false;
            UpdateGamePathStatus();
        }
    }

    private void btnLaunchGame_Click(object sender, EventArgs e)
    {
        try
        {
            var settings = SettingsService.Current;
            
            // Try to launch through Steam first if Steam App ID is configured
            bool success = false;
            if (!string.IsNullOrEmpty(settings.SteamAppId))
            {
                success = GamePathService.LaunchGameThroughSteam(settings.SteamAppId);
                if (!success)
                {
                    Logger.Log(LogLevel.Warning, $"Failed to launch through Steam, falling back to direct launch");
                }
            }
            
            // Fallback to direct launch if Steam launch failed or no Steam App ID configured
            if (!success)
            {
                success = GamePathService.LaunchGame(settings.GamePath);
            }
            
            if (!success)
            {
                MessageBox.Show("Failed to launch the game. Check that Steam is running and the game is installed, or that the game path is correct.", 
                    "Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error launching game: {ex.Message}");
            MessageBox.Show($"Error launching game: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnRefreshMods_Click(object sender, EventArgs e)
    {
        LoadMods();
    }

    private void lstMods_ItemChecked(object sender, ItemCheckedEventArgs e)
    {
        if (e.Item?.Tag is FolderMod folderMod)
        {
            folderMod.IsEnabled = e.Item.Checked;
        }
        
        // Update patch button state
        UpdateGamePathStatus();
    }

    private void MainForm_SizeChanged(object? sender, EventArgs e)
    {
        if (this.WindowState != FormWindowState.Minimized)
        {
            var settings = SettingsService.Current;
            if (settings.RememberWindowSize)
            {
                settings.WindowWidth = this.Width;
                settings.WindowHeight = this.Height;
            }
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // Save window size
        var settings = SettingsService.Current;
        if (settings.RememberWindowSize && this.WindowState != FormWindowState.Minimized)
        {
            settings.WindowWidth = this.Width;
            settings.WindowHeight = this.Height;
        }
        
        // Clean up console if it was allocated
        ConsoleService.FreeConsoleWindow();
    }

    // Menu event handlers
    private void menuFileExit_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    // Settings event handlers
    private void TxtGamePath_TextChanged(object? sender, EventArgs e)
    {
        var settings = SettingsService.Current;
        settings.GamePath = txtGamePath.Text;
        UpdateGamePathStatus();
    }

    private void BtnBrowseGamePath_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog()
        {
            Description = "Select the game installation folder",
            UseDescriptionForTitle = true,
            SelectedPath = SettingsService.Current.GamePath ?? ""
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtGamePath.Text = dialog.SelectedPath;
        }
    }

    private void CmbLogLevel_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbLogLevel.SelectedItem is LogLevel level)
        {
            SettingsService.Current.LogLevel = level;
        }
    }

    private void CmbGameVersion_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbGameVersion.SelectedIndex >= 0)
        {
            SettingsService.Current.GameVersion = (GameVersion)cmbGameVersion.SelectedIndex;
            UpdateGamePathStatus(); // Update the status to show new Steam App ID
        }
    }

    private void ChkRememberWindowSize_CheckedChanged(object? sender, EventArgs e)
    {
        SettingsService.Current.RememberWindowSize = chkRememberWindowSize.Checked;
    }

    private void ChkDarkMode_CheckedChanged(object? sender, EventArgs e)
    {
        SettingsService.Current.DarkMode = chkDarkMode.Checked;
        // TODO: Implement dark mode theme switching later
    }

    #region Asset Scanner

    /// <summary>
    /// Initialize the asset scanner service
    /// </summary>
    private void InitializeAssetScanner()
    {
        // Set up event handlers for asset scanner
        _assetScannerService.ProgressChanged += AssetScanner_ProgressChanged;
        _assetScannerService.ScanCompleted += AssetScanner_ScanCompleted;
        
        // Initialize asset type filter list
        InitializeAssetTypeFilters();
        
        // Set up automatic filter event handlers
        SetupAutoFilterEventHandlers();
        
        // Initialize UI state
        UpdateAssetScannerUI();
    }
    
    /// <summary>
    /// Set up event handlers for automatic filtering
    /// </summary>
    private void SetupAutoFilterEventHandlers()
    {
        // Auto-filter when any control changes
        chklstAssetTypes.ItemCheck += (s, e) => 
        {
            // Use BeginInvoke to delay until after the check state has changed
            this.BeginInvoke(() => UpdateAssetsList());
        };
        
        txtNameFilter.TextChanged += (s, e) => UpdateAssetsList();
        numMinSize.ValueChanged += (s, e) => UpdateAssetsList();
        numMaxSize.ValueChanged += (s, e) => UpdateAssetsList();
        
        // Set up asset double-click for preview
        lstAssets.DoubleClick += LstAssets_DoubleClick;
    }
    


    /// <summary>
    /// Initialize the asset type filters checklist
    /// </summary>
    private void InitializeAssetTypeFilters()
    {
        chklstAssetTypes.Items.Clear();
        
        // Add moddable asset types first (checked by default)
        var moddableTypes = UnityAssetTypeExtensions.GetModdableTypes();
        foreach (var assetType in moddableTypes)
        {
            var displayName = assetType.GetDisplayName();
            var index = chklstAssetTypes.Items.Add(displayName);
            chklstAssetTypes.SetItemChecked(index, true);
            chklstAssetTypes.Items[index] = new AssetTypeItem(assetType, displayName);
        }
        
        // Add other common asset types (unchecked by default)
        var otherTypes = new[]
        {
            UnityAssetType.GameObject,
            UnityAssetType.Transform,
            UnityAssetType.Camera,
            UnityAssetType.Light,
            UnityAssetType.Rigidbody,
            UnityAssetType.Collider,
            UnityAssetType.MonoScript,
            UnityAssetType.MonoBehaviour
        };
        
        foreach (var assetType in otherTypes)
        {
            if (!moddableTypes.Contains(assetType))
            {
                var displayName = assetType.GetDisplayName();
                var index = chklstAssetTypes.Items.Add(displayName);
                chklstAssetTypes.Items[index] = new AssetTypeItem(assetType, displayName);
            }
        }
        
        // Start with all moddable types selected by default
    }

    /// <summary>
    /// Update asset scanner UI state
    /// </summary>
    private void UpdateAssetScannerUI()
    {
        var isScanning = _assetScannerService.IsScanning;
        var hasGamePath = !string.IsNullOrEmpty(SettingsService.Current.GamePath) && 
                         GamePathService.ValidateGamePath(SettingsService.Current.GamePath);
        
        btnScanAssets.Enabled = !isScanning && hasGamePath;
        btnScanAssets.Text = isScanning ? "Cancel Scan" : "Scan Assets";
        
        progressAssets.Visible = isScanning;
        
        if (!isScanning)
        {
            lblScanProgress.Text = "";
            UpdateAssetsList();
            UpdateAssetStatistics();
        }
    }

    /// <summary>
    /// Update the assets list display
    /// </summary>
    private void UpdateAssetsList()
    {
        lstAssets.Items.Clear();
        
        var filter = CreateCurrentFilter();
        var filteredAssets = _assetScannerService.DiscoveredAssets
            .Where(filter.PassesFilter)
            .ToList();
        
        foreach (var asset in filteredAssets)
        {
            var item = new ListViewItem(asset.Name)
            {
                Tag = asset
            };
            
            item.SubItems.Add(asset.AssetTypeName);
            item.SubItems.Add(asset.FormattedSize);
            item.SubItems.Add(asset.FileName);
            
            lstAssets.Items.Add(item);
        }
        
        lblAssetCount.Text = $"Assets found: {filteredAssets.Count}";
    }

    /// <summary>
    /// Update asset statistics display
    /// </summary>
    private void UpdateAssetStatistics()
    {
        var stats = _assetScannerService.GetStatistics();
        lblAssetStats.Text = $"Total: {stats.TotalAssets} assets, {stats.FormattedTotalSize}";
    }

    /// <summary>
    /// Create filter from current UI state
    /// </summary>
    private AssetScanFilter CreateCurrentFilter()
    {
        var filter = new AssetScanFilter();
        
        // Asset type filter - get from checklist
        var selectedTypes = new HashSet<UnityAssetType>();
        for (int i = 0; i < chklstAssetTypes.Items.Count; i++)
        {
            if (chklstAssetTypes.GetItemChecked(i) && chklstAssetTypes.Items[i] is AssetTypeItem item)
            {
                selectedTypes.Add(item.AssetType);
            }
        }
        filter.IncludedTypes = selectedTypes;
        
        // Size filters
        filter.MinSize = (long)numMinSize.Value;
        filter.MaxSize = (long)numMaxSize.Value;
        
        // Name filter
        filter.NameFilter = string.IsNullOrEmpty(txtNameFilter.Text) ? null : txtNameFilter.Text;
        
        // Other filters
        filter.ModdableOnly = false; // We handle this above
        filter.IncludeEmpty = true; // Let user decide with size filters
        
        return filter;
    }

    #endregion

    #region Asset Scanner Event Handlers

    private void btnScanAssets_Click(object sender, EventArgs e)
    {
        if (_assetScannerService.IsScanning)
        {
            // Cancel current scan
            _scanCancellationSource?.Cancel();
            return;
        }
        
        var gamePath = SettingsService.Current.GamePath;
        if (string.IsNullOrEmpty(gamePath) || !GamePathService.ValidateGamePath(gamePath))
        {
            MessageBox.Show("Please configure a valid game path in Settings before scanning assets.", 
                "Game Path Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        
        StartAssetScan();
    }

    private async void StartAssetScan()
    {
        try
        {
            _scanCancellationSource?.Cancel();
            _scanCancellationSource = new CancellationTokenSource();
            
            UpdateAssetScannerUI();
            
            var filter = CreateScanFilter();
            await _assetScannerService.ScanAssetsAsync(SettingsService.Current.GamePath!, filter, _scanCancellationSource.Token);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error starting asset scan: {ex.Message}");
            MessageBox.Show($"Error starting asset scan: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateAssetScannerUI();
        }
    }

    private AssetScanFilter CreateScanFilter()
    {
        // For scanning, we want to get all assets and filter in UI for better performance
        var filter = new AssetScanFilter
        {
            IncludedTypes = Enum.GetValues<UnityAssetType>().ToHashSet(),
            IncludeEmpty = true,
            MinSize = 0,
            MaxSize = 0
        };
        
        return filter;
    }

    private void AssetScanner_ProgressChanged(object? sender, AssetScanProgressEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => AssetScanner_ProgressChanged(sender, e)));
            return;
        }
        
        progressAssets.Value = e.PercentComplete;
        lblScanProgress.Text = $"Scanning {e.CurrentFile}... ({e.ProcessedFiles}/{e.TotalFiles})";
    }

    private void AssetScanner_ScanCompleted(object? sender, AssetScanCompletedEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => AssetScanner_ScanCompleted(sender, e)));
            return;
        }
        
        UpdateAssetScannerUI();
        
        if (e.Success)
        {
            Logger.Log(LogLevel.Info, $"Asset scan completed successfully. Found {e.Assets.Count} assets.");
        }
        else
        {
            Logger.Log(LogLevel.Error, $"Asset scan failed: {e.ErrorMessage}");
            MessageBox.Show($"Asset scan failed: {e.ErrorMessage}", "Scan Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Handle asset double-click to open preview popup
    /// </summary>
    private void LstAssets_DoubleClick(object? sender, EventArgs e)
    {
        if (lstAssets.SelectedItems.Count > 0 && lstAssets.SelectedItems[0].Tag is DiscoveredAsset asset)
        {
            OpenAssetPreview(asset);
        }
    }
    
    /// <summary>
    /// Open the asset preview popup window
    /// </summary>
    private void OpenAssetPreview(DiscoveredAsset asset)
    {
        try
        {
            using (var previewForm = new AssetPreviewForm(asset))
            {
                previewForm.ShowDialog(this);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error opening preview for {asset.Name}: {ex.Message}");
            MessageBox.Show($"Could not open preview for {asset.Name}: {ex.Message}", 
                          "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    #endregion

    /// <summary>
    /// Helper class for asset type items in the checklist
    /// </summary>
    private class AssetTypeItem
    {
        public UnityAssetType AssetType { get; }
        public string DisplayName { get; }
        
        public AssetTypeItem(UnityAssetType assetType, string displayName)
        {
            AssetType = assetType;
            DisplayName = displayName;
        }
        
        public override string ToString() => DisplayName;
    }
}