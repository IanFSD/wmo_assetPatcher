using System.Runtime.CompilerServices;
using WMO.Core.Helpers;
using WMO.Core.Services;

namespace WMO.Core.Logging;

public static class Logger {
    private static readonly string ExeLogFilePath;
    private static readonly object LockObject = new();
    private static readonly List<string> BufferedLogs = [];
    private static string? _lastInstallLogPath;
    
    public static event Action<string>? LogMessageAdded;
    public static event EventHandler<string>? LogReceived;
    private static readonly List<string> _logMessages = new();

    static Logger() {
    var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
    Directory.CreateDirectory(logDirectory);
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    ExeLogFilePath = Path.Combine(logDirectory, $"Whisker Mountain Outbreak_{timestamp}.log");
    File.Delete(ExeLogFilePath);
}

    public static string GetLogPath() => GetInstallLogPath() ?? ExeLogFilePath;

    private static string? GetInstallLogPath() {
        if (string.IsNullOrEmpty(SettingsService.Current.GamePath))
            return null;

        var installLogDirectory = Path.Combine(SettingsService.Current.GamePath, "Logs");
        return !Directory.Exists(installLogDirectory) ? null : Path.Combine(installLogDirectory, "Whisker Mountain Outbreak.log");
    }

    private static void HandleInstallPathChange(string newInstallLogPath) {
        if (newInstallLogPath != _lastInstallLogPath) {
            File.WriteAllLines(newInstallLogPath, BufferedLogs);
            _lastInstallLogPath = newInstallLogPath;
        }
    }

    private static void WriteToLogs(string content, bool timestamped = true, LogLevel? logLevel = null) {
        var logMessage = timestamped ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {content}" : content;
        
        // Only output to console if console output is enabled
        if (SettingsService.Current.ConsoleOutput) {
            if (logLevel.HasValue) {
                ConsoleService.WriteColoredMessage(logLevel.Value, logMessage);
            } else {
                Console.WriteLine(logMessage);
            }
        }

        try {
            lock (LockObject) {
                File.AppendAllText(ExeLogFilePath, logMessage + Environment.NewLine);
                BufferedLogs.Add(logMessage);

                _logMessages.Add(logMessage);
                LogMessageAdded?.Invoke(logMessage);
                LogReceived?.Invoke(null, logMessage);

                var installLogPath = GetInstallLogPath();
                if (installLogPath == null) return;
                HandleInstallPathChange(installLogPath);
                File.AppendAllText(installLogPath, logMessage + Environment.NewLine);
            }
        } catch (Exception ex) {
            ErrorHandler.Handle($"Error writing to log file: {ex.Message}", ex, skipLogging: true);
        }
    }

    public static void Log(LogLevel lvl, [InterpolatedStringHandlerArgument("lvl")] LogInterpolatedStringHandler handler)
    {
        if (lvl > SettingsService.Current.LogLevel || SettingsService.Current.LogLevel == LogLevel.None)
            return;

        WriteToLogs($"{lvl.ToString().ToUpper()}: {handler.ToString()}", timestamped: true, logLevel: lvl);
    }
    
    public static List<string> GetAllMessages() {
        lock (LockObject) { return [.._logMessages]; }
    }

    public static void LogLineBreak(LogLevel lvl) {
        if (lvl > SettingsService.Current.LogLevel || SettingsService.Current.LogLevel == LogLevel.None) return;
        WriteToLogs(string.Empty, timestamped: false);
    }
    
    // Helper methods for common logging scenarios
    public static void LogInfo(string message) => Log(LogLevel.Info, $"{message}");
    public static void LogError(string message) => Log(LogLevel.Error, $"{message}");
    public static void LogWarning(string message) => Log(LogLevel.Warning, $"{message}");
    public static void LogSuccess(string message) => Log(LogLevel.Success, $"{message}");
    public static void LogDebug(string message) => Log(LogLevel.Debug, $"{message}");
    
    // Special method for console-only output (for user interaction prompts, etc.)
    public static void WriteConsole(string message, bool addNewLine = true) {
        if (SettingsService.Current.ConsoleOutput) {
            if (addNewLine) {
                ConsoleService.WriteColoredMessage(message);
            } else {
                Console.Write(message);
            }
        }
    }
}