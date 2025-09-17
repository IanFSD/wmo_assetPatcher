using WMO.Core.Logging;
using WMO.Core.Services;
using WMO.Core.Helpers;
using WMO.Core.Models.Enums;

namespace WMO.UI.Forms;

/// <summary>
/// Setup form shown on first application startup
/// </summary>
public partial class SetupForm : Form
{
    public string? SelectedGamePath { get; private set; }
    public GameVersion SelectedGameVersion { get; private set; } = GameVersion.FullGame;
    public LogLevel SelectedLogLevel { get; private set; } = LogLevel.Info;

    public SetupForm()
    {
        InitializeComponent();
        InitializeForm();
    }

    private void InitializeForm()
    {
        // Set form properties
        this.Text = "WMO Asset Patcher - First Time Setup";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Size = new Size(500, 430);
        
        // Show default game path
        txtGamePath.Text = SettingsService.DEFAULT_GAME_PATH;
        
        // Populate game version combo box
        cmbGameVersion.Items.Add("Full Game");
        cmbGameVersion.Items.Add("Friend's Pass");
        cmbGameVersion.SelectedIndex = 0; // Default to Full Game
        
        // Populate log level combo box
        cmbLogLevel.Items.AddRange(Enum.GetValues<LogLevel>().Cast<object>().ToArray());
        cmbLogLevel.SelectedItem = LogLevel.Info;
        
        // Check if default path is valid
        CheckDefaultPath();
    }

    private void CheckDefaultPath()
    {
        bool isValid = GamePathService.ValidateGamePath(SettingsService.DEFAULT_GAME_PATH);
        
        if (isValid)
        {
            lblPathStatus.Text = "Default path is valid";
            lblPathStatus.ForeColor = Color.Green;
            btnFinish.Enabled = true;
        }
        else
        {
            lblPathStatus.Text = " Default path is invalid - please browse for game location";
            lblPathStatus.ForeColor = Color.Orange;
            btnFinish.Enabled = false;
        }
    }

    private void btnBrowse_Click(object sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the Whisper Mountain Outbreak root directory\\n",
            ShowNewFolderButton = false,
            SelectedPath = txtGamePath.Text
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtGamePath.Text = dialog.SelectedPath;
            ValidateSelectedPath();
        }
    }

    private void ValidateSelectedPath()
    {
        string path = txtGamePath.Text.Trim();
        bool isValid = GamePathService.ValidateGamePath(path);

        if (isValid)
        {
            lblPathStatus.Text = "Game path is valid";
            lblPathStatus.ForeColor = Color.Green;
            btnFinish.Enabled = true;
            SelectedGamePath = path;
        }
        else
        {
            lblPathStatus.Text = "Invalid game path - please select a valid Whisper Mountain Outbreak installation";
            lblPathStatus.ForeColor = Color.Red;
            btnFinish.Enabled = false;
            SelectedGamePath = null;
        }
    }

    private void btnFinish_Click(object sender, EventArgs e)
    {
        // Validate one more time before finishing
        ValidateSelectedPath();
        
        if (btnFinish.Enabled)
        {
            SelectedLogLevel = (LogLevel)cmbLogLevel.SelectedItem!;
            SelectedGameVersion = cmbGameVersion.SelectedIndex == 0 ? GameVersion.FullGame : GameVersion.FriendsPass;
            
            // Save initial settings using the unified settings service
            SettingsService.Current.GamePath = SelectedGamePath;
            SettingsService.Current.LogLevel = SelectedLogLevel;
            SettingsService.Current.GameVersion = SelectedGameVersion;
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void txtGamePath_TextChanged(object sender, EventArgs e)
    {
        // Validate path when text changes
        if (!string.IsNullOrWhiteSpace(txtGamePath.Text))
        {
            ValidateSelectedPath();
        }
        else
        {
            lblPathStatus.Text = "Please select a game path";
            lblPathStatus.ForeColor = Color.Gray;
            btnFinish.Enabled = false;
        }
    }

    private void cmbLogLevel_SelectedIndexChanged(object sender, EventArgs e)
    {
        SelectedLogLevel = (LogLevel)cmbLogLevel.SelectedItem!;
    }
}
