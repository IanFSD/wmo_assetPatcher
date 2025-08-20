using WMO.Logging;

namespace WMO.Helper;

public static class SettingsHolder {
	private static string? _installPath;
	private static bool _allowStartupWithConflicts;
	private static bool _isPatched = true;
	private static LogLevel _logLevel = LogLevel.Info;
	
	public static event Action? InstallPathChanged,
		StartupWithConflictsChanged,
		PatchStatusChanged,
		ModStateChanged,
		CheckForUpdatesOnStartupChanged,
		WindowSizeChanged,
		LogLevelChanged;

	public static string? InstallPath {
		get => _installPath;
		set { 	
			var isValid = value != null && File.Exists(Path.Combine(value, "Whisper Mountain Outbreak.exe"));
        
			if (_installPath == value) return;
        
			_installPath = isValid ? value : null;
			InstallPathChanged?.Invoke();
			Logger.Log(LogLevel.Debug, $"Setting {nameof(InstallPath)} changed to: {value}");
		}
	}

	public static bool AllowStartupWithConflicts {
		get => _allowStartupWithConflicts;
		set { 	
			_allowStartupWithConflicts = value;
			StartupWithConflictsChanged?.Invoke();
			Logger.Log(LogLevel.Debug, $"Setting {nameof(AllowStartupWithConflicts)} changed to: {value}");
		}
	}

	public static bool IsPatched {
		get => _isPatched;
		set { 	
			if (_isPatched == value) return;
			_isPatched = value;
			PatchStatusChanged?.Invoke();
			Logger.Log(LogLevel.Debug, $"Setting {nameof(IsPatched)} changed to: {value}");
		}
	}
	
	public static LogLevel LogLevel {
		get => _logLevel;
		set { 	
			if (_logLevel == value) return;
			_logLevel = value;
			LogLevelChanged?.Invoke();
			Logger.Log(LogLevel.Debug, $"Setting {nameof(LogLevel)} changed to: {value}");
		}
	}
}