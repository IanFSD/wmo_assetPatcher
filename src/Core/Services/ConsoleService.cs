using System.Runtime.InteropServices;
using WMO.Core.Logging;

namespace WMO.Core.Services;

/// <summary>
/// Service for managing a console window with colored output
/// </summary>
public static class ConsoleService
{
    #region Win32 API
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetConsoleWindow();
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleTitle(string lpConsoleTitle);
    
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;
    
    #endregion
    
    private static bool _consoleAllocated = false;
    private static readonly object _consoleLock = new();
    
    /// <summary>
    /// Allocate and show a console window
    /// </summary>
    /// <param name="title">Console window title</param>
    public static bool AllocateConsole(string title = "WMO Asset Patcher")
    {
        lock (_consoleLock)
        {
            if (_consoleAllocated) return true;
            
            try
            {
                // Allocate console
                if (!AllocConsole())
                {
                    return false;
                }
                
                // Set title
                SetConsoleTitle(title);
                
                // Redirect standard streams to console
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                Console.SetIn(new StreamReader(Console.OpenStandardInput()));
                
                _consoleAllocated = true;
                
                // Set initial colors
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Clear();
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Free the console window
    /// </summary>
    public static void FreeConsoleWindow()
    {
        lock (_consoleLock)
        {
            if (!_consoleAllocated) return;
            
            try
            {
                FreeConsole();
                _consoleAllocated = false;
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }
    
    /// <summary>
    /// Show or hide the console window
    /// </summary>
    public static void SetConsoleVisibility(bool visible)
    {
        if (!_consoleAllocated) return;
        
        var consoleWindow = GetConsoleWindow();
        if (consoleWindow != IntPtr.Zero)
        {
            ShowWindow(consoleWindow, visible ? SW_SHOW : SW_HIDE);
        }
    }
    
    /// <summary>
    /// Write a message to console with appropriate color based on log level
    /// </summary>
    public static void WriteColoredMessage(LogLevel level, string message)
    {
        if (!_consoleAllocated) return;
        
        lock (_consoleLock)
        {
            try
            {
                var (foreground, background) = GetColorsForLogLevel(level);
                
                var originalForeground = Console.ForegroundColor;
                var originalBackground = Console.BackgroundColor;
                
                Console.ForegroundColor = foreground;
                Console.BackgroundColor = background;
                
                Console.WriteLine(message);
                
                Console.ForegroundColor = originalForeground;
                Console.BackgroundColor = originalBackground;
            }
            catch
            {
                // Fallback to regular console output
                Console.WriteLine(message);
            }
        }
    }
    
    /// <summary>
    /// Write a message with custom colors
    /// </summary>
    public static void WriteColoredMessage(string message, ConsoleColor foreground = ConsoleColor.Gray, ConsoleColor background = ConsoleColor.Black)
    {
        if (!_consoleAllocated) return;
        
        lock (_consoleLock)
        {
            try
            {
                var originalForeground = Console.ForegroundColor;
                var originalBackground = Console.BackgroundColor;
                
                Console.ForegroundColor = foreground;
                Console.BackgroundColor = background;
                
                Console.WriteLine(message);
                
                Console.ForegroundColor = originalForeground;
                Console.BackgroundColor = originalBackground;
            }
            catch
            {
                Console.WriteLine(message);
            }
        }
    }
    
    /// <summary>
    /// Get appropriate console colors for a log level
    /// </summary>
    private static (ConsoleColor foreground, ConsoleColor background) GetColorsForLogLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => (ConsoleColor.DarkGray, ConsoleColor.Black),
            LogLevel.Debug => (ConsoleColor.Cyan, ConsoleColor.Black),
            LogLevel.Info => (ConsoleColor.White, ConsoleColor.Black),
            LogLevel.Warning => (ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Error => (ConsoleColor.Red, ConsoleColor.Black),
            LogLevel.Fatal => (ConsoleColor.White, ConsoleColor.DarkRed),
            LogLevel.Success => (ConsoleColor.Green, ConsoleColor.Black),
            _ => (ConsoleColor.Gray, ConsoleColor.Black)
        };
    }
    
    /// <summary>
    /// Clear the console
    /// </summary>
    public static void Clear()
    {
        if (!_consoleAllocated) return;
        
        try
        {
            Console.Clear();
        }
        catch
        {
            // Ignore errors
        }
    }
    
    /// <summary>
    /// Write a separator line
    /// </summary>
    public static void WriteSeparator(char character = '=', ConsoleColor color = ConsoleColor.DarkGray)
    {
        if (!_consoleAllocated) return;
        
        var width = Console.WindowWidth;
        var line = new string(character, Math.Max(1, width - 1));
        WriteColoredMessage(line, color);
    }
    
    /// <summary>
    /// Write a header with emphasis
    /// </summary>
    public static void WriteHeader(string text, ConsoleColor color = ConsoleColor.Cyan)
    {
        if (!_consoleAllocated) return;
        
        WriteSeparator();
        WriteColoredMessage($" {text}", color);
        WriteSeparator();
    }
}
