using WMO.Core.Logging;
using WMO.Core.Services;
using WMO.Core.Helpers;
using WMO.Core.Patching;
using WMO.UI.Forms;
using WMO.Core.Models;

namespace WMO.UI;

/// <summary>
/// Main application form with tabbed interface
/// </summary>
public partial class MainForm : Form
{
    private readonly FolderModService _folderModService = new();
    private bool _isPatchingInProgress = false;

    public MainForm()
    {
        InitializeComponent();
        InitializeForm();
        LoadMods();
    }

    private void InitializeForm()
    {
        // Set form properties
        this.Text = "WMO Asset Patcher";
        this.StartPosition = FormStartPosition.CenterScreen;
        
        // Load window size from settings
        var settings = UISettingsService.Current;
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
        var settings = UISettingsService.Current;
        
        // Populate log level combo box
        cmbLogLevel.Items.Clear();
        foreach (LogLevel level in Enum.GetValues<LogLevel>())
        {
            cmbLogLevel.Items.Add(level);
        }
        
        // Bind settings to controls
        LoadSettingsToControls();
        
        // Set up event handlers for settings controls
        txtGamePath.TextChanged += TxtGamePath_TextChanged;
        btnBrowseGamePath.Click += BtnBrowseGamePath_Click;
        chkAutoBackup.CheckedChanged += ChkAutoBackup_CheckedChanged;
        chkConfirmBeforePatching.CheckedChanged += ChkConfirmBeforePatching_CheckedChanged;
        chkCheckForUpdates.CheckedChanged += ChkCheckForUpdates_CheckedChanged;
        cmbLogLevel.SelectedIndexChanged += CmbLogLevel_SelectedIndexChanged;
        chkRememberWindowSize.CheckedChanged += ChkRememberWindowSize_CheckedChanged;
        chkDarkMode.CheckedChanged += ChkDarkMode_CheckedChanged;
    }

    private void LoadSettingsToControls()
    {
        var settings = UISettingsService.Current;
        
        txtGamePath.Text = settings.GamePath ?? "";
        chkAutoBackup.Checked = settings.AutoBackup;
        chkConfirmBeforePatching.Checked = settings.ConfirmBeforePatching;
        chkCheckForUpdates.Checked = settings.CheckForUpdatesOnStartup;
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
        var settings = UISettingsService.Current;
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
            lblGamePathStatus.Text = $"✓ Game path: {gamePath}";
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

            var settings = UISettingsService.Current;
            
            // Show confirmation if enabled
            if (settings.ConfirmBeforePatching)
            {
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
            }

            _isPatchingInProgress = true;
            UpdateGamePathStatus();
            
            // Update status for selected mods
            foreach (var mod in selectedMods)
            {
                mod.Status = "Preparing...";
            }
            
            // Show console output form
            var consoleForm = new ConsoleOutputForm(settings.LogLevel, "Patching Game - Progress");
            consoleForm.Show();
            
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
            
            // Update console form to show completion
            consoleForm.SetOperationComplete(success);
            
            // Show result message
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
            var settings = UISettingsService.Current;
            bool success = GamePathService.LaunchGame(settings.GamePath);
            
            if (!success)
            {
                MessageBox.Show("Failed to launch the game. Check that the game path is correct and the executable exists.", 
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
            var settings = UISettingsService.Current;
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
        var settings = UISettingsService.Current;
        if (settings.RememberWindowSize && this.WindowState != FormWindowState.Minimized)
        {
            settings.WindowWidth = this.Width;
            settings.WindowHeight = this.Height;
        }
    }

    // Menu event handlers
    private void menuFileExit_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    // Settings event handlers
    private void TxtGamePath_TextChanged(object? sender, EventArgs e)
    {
        var settings = UISettingsService.Current;
        settings.GamePath = txtGamePath.Text;
        UpdateGamePathStatus();
    }

    private void BtnBrowseGamePath_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog()
        {
            Description = "Select the game installation folder",
            UseDescriptionForTitle = true,
            SelectedPath = UISettingsService.Current.GamePath ?? ""
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtGamePath.Text = dialog.SelectedPath;
        }
    }

    private void ChkAutoBackup_CheckedChanged(object? sender, EventArgs e)
    {
        UISettingsService.Current.AutoBackup = chkAutoBackup.Checked;
    }

    private void ChkConfirmBeforePatching_CheckedChanged(object? sender, EventArgs e)
    {
        UISettingsService.Current.ConfirmBeforePatching = chkConfirmBeforePatching.Checked;
    }

    private void ChkCheckForUpdates_CheckedChanged(object? sender, EventArgs e)
    {
        UISettingsService.Current.CheckForUpdatesOnStartup = chkCheckForUpdates.Checked;
    }

    private void CmbLogLevel_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbLogLevel.SelectedItem is LogLevel level)
        {
            UISettingsService.Current.LogLevel = level;
        }
    }

    private void ChkRememberWindowSize_CheckedChanged(object? sender, EventArgs e)
    {
        UISettingsService.Current.RememberWindowSize = chkRememberWindowSize.Checked;
    }

    private void ChkDarkMode_CheckedChanged(object? sender, EventArgs e)
    {
        UISettingsService.Current.DarkMode = chkDarkMode.Checked;
        // TODO: Implement dark mode theme switching
        if (chkDarkMode.Checked)
        {
            MessageBox.Show("Dark mode is experimental and will be implemented in a future version.", 
                "Dark Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}