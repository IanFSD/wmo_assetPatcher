using WMO.Core.Logging;
using System.Collections.Concurrent;

namespace WMO.UI.Forms;

/// <summary>
/// Form for displaying console output and logging during operations
/// </summary>
public partial class ConsoleOutputForm : Form
{
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly System.Windows.Forms.Timer _updateTimer;
    private readonly LogLevel _logLevel;
    private bool _isOperationComplete = false;
    private bool _operationResult = false;

    public ConsoleOutputForm(LogLevel logLevel, string title = "Operation Progress")
    {
        InitializeComponent();
        _logLevel = logLevel;
        
        this.Text = title;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Size = new Size(800, 600);
        this.MinimumSize = new Size(600, 400);
        
        _updateTimer = new System.Windows.Forms.Timer();
        _updateTimer.Interval = 100; // Update every 100ms
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
        
        Logger.LogReceived += OnLogMessageReceived;
        
        btnClose.Enabled = false;
    }

    private void OnLogMessageReceived(object? sender, string logMessage)
    {
        var level = LogLevel.Info; // default
        
        try
        {
            if (logMessage.Contains("] ") && logMessage.Contains(": "))
            {
                var levelStart = logMessage.IndexOf("] ") + 2;
                var levelEnd = logMessage.IndexOf(": ", levelStart);
                if (levelEnd > levelStart)
                {
                    var levelStr = logMessage.Substring(levelStart, levelEnd - levelStart).Trim();
                    if (Enum.TryParse<LogLevel>(levelStr, true, out var parsedLevel))
                    {
                        level = parsedLevel;
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, use default level
        }

        if (level >= _logLevel)
        {
            _messageQueue.Enqueue(logMessage);
        }
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        var messages = new List<string>();
        while (_messageQueue.TryDequeue(out var message))
        {
            messages.Add(message);
        }

        if (messages.Count > 0)
        {
            if (txtOutput.InvokeRequired)
            {
                txtOutput.Invoke(new Action(() =>
                {
                    foreach (var msg in messages)
                    {
                        txtOutput.AppendText(msg + Environment.NewLine);
                    }
                    
                    txtOutput.SelectionStart = txtOutput.Text.Length;
                    txtOutput.ScrollToCaret();
                }));
            }
            else
            {
                foreach (var msg in messages)
                {
                    txtOutput.AppendText(msg + Environment.NewLine);
                }
                
                txtOutput.SelectionStart = txtOutput.Text.Length;
                txtOutput.ScrollToCaret();
            }
        }

        if (_isOperationComplete && !btnClose.Enabled)
        {
            btnClose.Enabled = true;
            btnClose.Text = _operationResult ? "Close" : "Close";
            
            this.Text += _operationResult ? " - Completed Successfully" : " - Failed";
        }
    }

    public void SetOperationComplete(bool success)
    {
        _isOperationComplete = true;
        _operationResult = success;
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    private void btnClear_Click(object sender, EventArgs e)
    {
        txtOutput.Clear();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        
        Logger.LogReceived -= OnLogMessageReceived;
        
        base.OnFormClosing(e);
    }

    private void btnCopy_Click(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(txtOutput.Text))
        {
            Clipboard.SetText(txtOutput.Text);
            MessageBox.Show("Log output copied to clipboard.", "Copied", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
