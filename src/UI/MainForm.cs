using WMO.Core.Logging;
using WMO.Core.Services;
using WMO.Core.Helpers;
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
    private bool _isLaunchInProgress = false;
    private bool _isGameRunning = false;
    private CancellationTokenSource? _scanCancellationSource;
    private CancellationTokenSource? _gameMonitorCts;

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
        this.Text = "WMO Mod Loader";
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
            // Create display name with version
            var displayName = folderMod.Name;
            
            if (!string.IsNullOrEmpty(folderMod.Version))
                displayName += $" v{folderMod.Version}";
            
            var item = new ListViewItem(displayName)
            {
                Tag = folderMod,
                Checked = folderMod.IsEnabled
            };
            
            // Add author
            item.SubItems.Add(folderMod.Author ?? "Unknown");
            
            // Add description
            item.SubItems.Add(folderMod.Description ?? "");
            
            lstMods.Items.Add(item);
        }
        
        lblModCount.Text = $"Available Mods ({_folderModService.AvailableMods.Count})";
        UpdateModSummary();
        
        // Resize description column to fill available space
        ResizeDescriptionColumn();
    }

    private void UpdateModSummary()
    {
        var enabledCount = _folderModService.AvailableMods.Count(m => m.IsEnabled);
        var pluginCount = _folderModService.AvailableMods
            .Where(m => m.IsEnabled)
            .Sum(m => m.ModFiles.Count(f => f.Type == ModType.BepInExPlugin && f.IsEnabled));
        
        var parts = new List<string>();
        parts.Add($"{enabledCount} mod{(enabledCount != 1 ? "s" : "")} selected");
        if (pluginCount > 0)
            parts.Add($"{pluginCount} plugin{(pluginCount != 1 ? "s" : "")}");
        
        lblModSummary.Text = string.Join(" · ", parts);
    }

    private void UpdateGamePathStatus()
    {
        var settings = SettingsService.Current;
        var gamePath = settings.GamePath;
        
        if (string.IsNullOrEmpty(gamePath))
        {
            lblGamePathStatus.Text = "⚠ No game path configured";
            lblGamePathStatus.ForeColor = Color.Orange;
            btnLaunchGame.Enabled = false;
        }
        else if (GamePathService.ValidateGamePath(gamePath))
        {
            var gameVersionText = settings.GameVersion == GameVersion.FullGame ? "Full Game" : "Friend's Pass";
            lblGamePathStatus.Text = $"✓ {gameVersionText} (Steam ID: {settings.SteamAppId}) - {gamePath}";
            lblGamePathStatus.ForeColor = Color.Green;
            btnLaunchGame.Enabled = !_isLaunchInProgress && !_isGameRunning;
        }
        else
        {
            lblGamePathStatus.Text = "✗ Invalid game path";
            lblGamePathStatus.ForeColor = Color.Red;
            btnLaunchGame.Enabled = false;
        }
    }

    private void btnLaunchGame_Click(object sender, EventArgs e)
    {
        if (_isLaunchInProgress) return;
        
        bool shouldMonitor = false;
        
        try
        {
            _isLaunchInProgress = true;
            UpdateGamePathStatus();
            
            var settings = SettingsService.Current;
            
            if (string.IsNullOrEmpty(settings.SteamAppId))
            {
                MessageBox.Show("Steam App ID not configured. Please check your game version settings.", 
                    "Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // Get enabled mods that have BepInEx plugins
            var enabledMods = _folderModService.AvailableMods.Where(m => m.IsEnabled).ToList();
            var pluginCount = enabledMods.Sum(m => m.ModFiles.Count(f => f.Type == ModType.BepInExPlugin && f.IsEnabled));
            
            if (pluginCount > 0)
            {
                // Sync enabled plugins to the managed folder
                Logger.Log(LogLevel.Info, $"Syncing {pluginCount} BepInEx plugin(s) before launch...");
                int synced = BepInExPluginManager.SyncPlugins(settings.GamePath!, enabledMods);
                
                if (synced < 0)
                {
                    MessageBox.Show("Failed to sync BepInEx plugins. Make sure BepInEx is installed.\n\n" +
                                   "Check that the game path is correct and BepInEx was properly set up.",
                        "Plugin Sync Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                Logger.Log(LogLevel.Info, $"Successfully synced {synced} plugin(s)");
            }
            else
            {
                // No BepInEx plugins selected — clean managed folder for vanilla launch
                Logger.Log(LogLevel.Info, $"No BepInEx plugins enabled. Cleaning managed folder.");
                BepInExPluginManager.CleanManagedPlugins(settings.GamePath!);
            }
            
            // Launch the game through Steam
            Logger.Log(LogLevel.Info, $"Launching game through Steam (App ID: {settings.SteamAppId}, Version: {settings.GameVersion})");
            bool success = GamePathService.LaunchGameThroughSteam(settings.SteamAppId);
            
            if (success)
            {
                foreach (var mod in enabledMods)
                {
                    mod.Status = "Active";
                }
                
                shouldMonitor = true;
                _ = MonitorGameProcessAsync(settings.GamePath!, enabledMods, pluginCount > 0);
            }
            else
            {
                MessageBox.Show($"Failed to launch the game through Steam.\n\n" +
                               $"Game Version: {settings.GameVersion}\n" +
                               $"Steam App ID: {settings.SteamAppId}\n\n" +
                               $"Please ensure:\n" +
                               $"• Steam is running\n" +
                               $"• The game is installed in your Steam library\n" +
                               $"• The correct game version is selected in Settings", 
                    "Steam Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error launching game: {ex.Message}");
            MessageBox.Show($"Error launching game: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!shouldMonitor)
            {
                _isLaunchInProgress = false;
                UpdateGamePathStatus();
            }
        }
    }

    private async Task MonitorGameProcessAsync(string gameRoot, List<FolderMod> enabledMods, bool hasPlugins)
    {
        const string processName = "Whisper Mountain Outbreak";
        const int pollIntervalMs = 2000;
        const int maxWaitForStartMs = 120_000; // 2 minutes to detect the game process

        try
        {
            _gameMonitorCts = new CancellationTokenSource();
            var token = _gameMonitorCts.Token;

            Invoke(() =>
            {
                SetGameRunningLockState(true);
                statusLabel.Text = "Waiting for game to start...";
            });

            // Poll until the game process appears
            System.Diagnostics.Process? gameProcess = null;
            var startWait = DateTime.UtcNow;

            while (gameProcess == null)
            {
                token.ThrowIfCancellationRequested();

                if ((DateTime.UtcNow - startWait).TotalMilliseconds > maxWaitForStartMs)
                {
                    Logger.Log(LogLevel.Warning, $"Timed out waiting for game process to start. Cleaning up plugins.");
                    break;
                }

                var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    gameProcess = processes[0];
                    for (int i = 1; i < processes.Length; i++)
                        processes[i].Dispose();
                }
                else
                {
                    await Task.Delay(pollIntervalMs, token);
                }
            }

            if (gameProcess != null)
            {
                Logger.Log(LogLevel.Info, $"Game process detected. Monitoring for exit...");

                Invoke(() => statusLabel.Text = "Game is running. Mods will be cleaned up on exit.");

                // Wait for the game to close
                await gameProcess.WaitForExitAsync(token);
                gameProcess.Dispose();

                Logger.Log(LogLevel.Info, $"Game process exited.");
            }

            // Clean up managed plugins if any were synced
            if (hasPlugins)
                BepInExPluginManager.CleanManagedPlugins(gameRoot);

            foreach (var mod in enabledMods)
                mod.Status = "Ready";

            Logger.Log(LogLevel.Success, $"Post-game cleanup completed. Managed plugins removed.");

            Invoke(() =>
            {
                statusLabel.Text = "Game closed. Mods cleaned up.";
                UpdateModsList();
            });
        }
        catch (OperationCanceledException)
        {
            Logger.Log(LogLevel.Info, $"Game monitoring cancelled. Plugins will be cleaned on next launch.");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error monitoring game process: {ex.Message}");
        }
        finally
        {
            _gameMonitorCts?.Dispose();
            _gameMonitorCts = null;

            if (!this.IsDisposed && !this.Disposing)
            {
                Invoke(() =>
                {
                    _isLaunchInProgress = false;
                    SetGameRunningLockState(false);
                    UpdateGamePathStatus();
                });
            }
        }
    }

    /// <summary>
    /// Shows or hides the semi-transparent overlay that blocks all interaction while the game is running.
    /// </summary>
    private void SetGameRunningLockState(bool locked)
    {
        _isGameRunning = locked;
        pnlGameRunningOverlay.Visible = locked;
        if (locked)
            pnlGameRunningOverlay.BringToFront();
    }

    private void btnRefreshMods_Click(object sender, EventArgs e)
    {
        LoadMods();
    }

    private void btnOpenModsFolder_Click(object sender, EventArgs e)
    {
        var modsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods");
        if (!Directory.Exists(modsPath))
            Directory.CreateDirectory(modsPath);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = modsPath,
            UseShellExecute = true
        });
    }

    private void lstMods_ItemChecked(object sender, ItemCheckedEventArgs e)
    {
        if (e.Item?.Tag is FolderMod folderMod)
        {
            folderMod.IsEnabled = e.Item.Checked;
        }
        
        // Update summary and launch button state
        UpdateModSummary();
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
            
            // Resize Description column to fill remaining space
            ResizeDescriptionColumn();
        }
    }
    
    private void ResizeDescriptionColumn()
    {
        if (lstMods.Columns.Count >= 4)
        {
            // Calculate available width (subtract width of first three columns + checkbox space)
            int availableWidth = lstMods.ClientSize.Width - lstMods.Columns[0].Width - lstMods.Columns[1].Width - lstMods.Columns[2].Width - 25;
            if (availableWidth > 100)
            {
                lstMods.Columns[3].Width = availableWidth;
            }
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // Prevent closing while the game is running
        if (_gameMonitorCts != null)
        {
            MessageBox.Show(
                "The game is still running. Please close the game before exiting.\n\n" +
                "Mods will be automatically cleaned up when the game exits.",
                "Game Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }
        
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
        
        btnScanAssets.Enabled = !isScanning && hasGamePath && !_isGameRunning;
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