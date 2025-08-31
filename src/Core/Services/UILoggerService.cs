using System.Collections.ObjectModel;
using System.ComponentModel;
using WMO.Core.Logging;

namespace WMO.Core.Services;

/// <summary>
/// Service for providing real-time log updates to UI components
/// </summary>
public class UILoggerService : INotifyPropertyChanged
{
    private readonly ObservableCollection<LogEntry> _logEntries = new();
    private readonly object _lockObject = new();
    private int _maxLogEntries = 1000;
    
    public UILoggerService()
    {
        // Subscribe to the global logger events
        Logger.LogReceived += OnLogReceived;
    }
    
    /// <summary>
    /// Collection of log entries for UI binding
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries => _logEntries;
    
    /// <summary>
    /// Maximum number of log entries to keep in memory
    /// </summary>
    public int MaxLogEntries
    {
        get => _maxLogEntries;
        set
        {
            if (_maxLogEntries != value)
            {
                _maxLogEntries = value;
                OnPropertyChanged();
                TrimLogEntries();
            }
        }
    }
    
    /// <summary>
    /// Clears all log entries
    /// </summary>
    public void ClearLogs()
    {
        lock (_lockObject)
        {
            // Add to collection (we're not using WPF yet, so just add directly)
        _logEntries.Clear();
        }
    }
    
    /// <summary>
    /// Exports log entries to a file
    /// </summary>
    public async Task<bool> ExportLogsAsync(string filePath)
    {
        try
        {
            var logLines = new List<string>();
            
            lock (_lockObject)
            {
                foreach (var entry in _logEntries)
                {
                    logLines.Add($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {entry.Level}: {entry.Message}");
                }
            }
            
            await File.WriteAllLinesAsync(filePath, logLines);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to export logs: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Filters log entries by level
    /// </summary>
    public ObservableCollection<LogEntry> GetFilteredLogs(LogLevel minimumLevel)
    {
        var filtered = new ObservableCollection<LogEntry>();
        
        lock (_lockObject)
        {
            foreach (var entry in _logEntries.Where(e => e.Level >= minimumLevel))
            {
                filtered.Add(entry);
            }
        }
        
        return filtered;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    private void OnLogReceived(object? sender, string logMessage)
    {
        // Parse the log message to extract level and content
        var entry = ParseLogMessage(logMessage);
        
        lock (_lockObject)
        {
            // Add to collection (we're not using WPF yet, so just add directly)
            _logEntries.Add(entry);
            TrimLogEntries();
        }
    }
    
    private LogEntry ParseLogMessage(string logMessage)
    {
        // Expected format: [timestamp] LEVEL: message
        var timestamp = DateTime.Now;
        var level = LogLevel.Info;
        var message = logMessage;
        
        try
        {
            // Try to parse the structured log format
            if (logMessage.StartsWith("[") && logMessage.Contains("]"))
            {
                var timestampEnd = logMessage.IndexOf(']');
                if (timestampEnd > 0)
                {
                    var timestampStr = logMessage.Substring(1, timestampEnd - 1);
                    if (DateTime.TryParse(timestampStr, out var parsedTimestamp))
                    {
                        timestamp = parsedTimestamp;
                    }
                    
                    var remainder = logMessage.Substring(timestampEnd + 1).Trim();
                    var levelEnd = remainder.IndexOf(':');
                    if (levelEnd > 0)
                    {
                        var levelStr = remainder.Substring(0, levelEnd).Trim();
                        if (Enum.TryParse<LogLevel>(levelStr, out var parsedLevel))
                        {
                            level = parsedLevel;
                        }
                        message = remainder.Substring(levelEnd + 1).Trim();
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, use defaults
        }
        
        return new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            Message = message
        };
    }
    
    private void TrimLogEntries()
    {
        while (_logEntries.Count > _maxLogEntries)
        {
            _logEntries.RemoveAt(0);
        }
    }
}

/// <summary>
/// Represents a single log entry for UI display
/// </summary>
public class LogEntry
{
    public required DateTime Timestamp { get; init; }
    public required LogLevel Level { get; init; }
    public required string Message { get; init; }
    
    /// <summary>
    /// Formatted timestamp for display
    /// </summary>
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss.fff");
    
    /// <summary>
    /// Level as display string with emoji
    /// </summary>
    public string LevelDisplay => Level switch
    {
        LogLevel.Fatal => "💀 FATAL",
        LogLevel.Error => "❌ ERROR",
        LogLevel.Warning => "⚠️ WARN",
        LogLevel.Success => "✅ SUCCESS",
        LogLevel.Info => "ℹ️ INFO",
        LogLevel.Debug => "🔍 DEBUG",
        LogLevel.Performance => "⏱️ PERF",
        LogLevel.Trace => "📝 TRACE",
        _ => Level.ToString().ToUpper()
    };
    
    /// <summary>
    /// Color for the log level (for UI styling)
    /// </summary>
    public string LevelColor => Level switch
    {
        LogLevel.Fatal => "#8B0000",     // Dark red
        LogLevel.Error => "#FF0000",     // Red
        LogLevel.Warning => "#FFA500",   // Orange
        LogLevel.Success => "#008000",   // Green
        LogLevel.Info => "#0000FF",      // Blue
        LogLevel.Debug => "#808080",     // Gray
        LogLevel.Performance => "#800080", // Purple
        LogLevel.Trace => "#A0A0A0",     // Light gray
        _ => "#000000"                   // Black
    };
}
