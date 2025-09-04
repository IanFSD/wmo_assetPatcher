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
        
        // Set up timer for updating UI
        _updateTimer = new System.Windows.Forms.Timer();
        _updateTimer.Interval = 100; // Update every 100ms
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
        
        // Subscribe to logger events
        Logger.LogReceived += OnLogMessageReceived;
        
        btnClose.Enabled = false;
    }

    private void OnLogMessageReceived(object? sender, string logMessage)
    {
        // Parse log level from the message format: [timestamp] LEVEL: message
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

        // Only show messages at or above the configured log level
        if (level >= _logLevel)
        {
            _messageQueue.Enqueue(logMessage);
        }
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        // Process queued messages
        var messages = new List<string>();
        while (_messageQueue.TryDequeue(out var message))
        {
            messages.Add(message);
        }

        if (messages.Count > 0)
        {
            // Append messages to the text box
            if (txtOutput.InvokeRequired)
            {
                txtOutput.Invoke(new Action(() =>
                {
                    foreach (var msg in messages)
                    {
                        txtOutput.AppendText(msg + Environment.NewLine);
                    }
                    
                    // Auto-scroll to bottom
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
                
                // Auto-scroll to bottom
                txtOutput.SelectionStart = txtOutput.Text.Length;
                txtOutput.ScrollToCaret();
            }
        }

        // Check if operation is complete and enable close button
        if (_isOperationComplete && !btnClose.Enabled)
        {
            btnClose.Enabled = true;
            btnClose.Text = _operationResult ? "Close" : "Close";
            
            // Update title to show result
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
        // Clean up
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        
        // Unsubscribe from logger events
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
