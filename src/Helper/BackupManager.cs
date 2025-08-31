using WMO.Logging;

namespace WMO.Helper;

public static class BackupManager
{
	private const string BACKUPS_RELATIVE_PATH = "Backups";

	// Always call only after validating InstallPath is not null.
	private static string BackupFolderPath => Path.Combine(SettingsHolder.InstallPath!, BACKUPS_RELATIVE_PATH);

	public static bool RecoverBackups()
	{
		if (SettingsHolder.InstallPath == null)
			return false;

		if (!Directory.Exists(BackupFolderPath))
			return true;

		var failedRestorations = new List<string>();

		foreach (var backup in Directory.GetFiles(BackupFolderPath, "*.*", SearchOption.AllDirectories))
		{
			var relativePath = Path.GetRelativePath(BackupFolderPath, backup);
			var originalPath = Path.Combine(SettingsHolder.InstallPath, relativePath);

			if (!File.Exists(originalPath))
			{
				Logger.Log(LogLevel.Error, $"A backup is present for a file not present in the original directory: {relativePath}");
				ErrorHandler.Handle("A backup is present for a file not present in the original directory. " +
									"The backup could not be restored properly", null);
				return false;
			}

			try
			{
				File.Copy(backup, originalPath, true);
				File.Delete(backup);
				Logger.Log(LogLevel.Debug, $"Successfully restored file from backup: {relativePath}");
			}
			catch (UnauthorizedAccessException ex)
			{
				Logger.Log(LogLevel.Error, $"Access denied while restoring backup for {relativePath}: {ex.Message}");
				failedRestorations.Add(relativePath);
			}
			catch (IOException ex) when (ex.HResult == -2147024864) // File is being used by another process
			{
				Logger.Log(LogLevel.Error, $"Cannot restore {relativePath} - file is in use by another process: {ex.Message}");
				failedRestorations.Add(relativePath);
			}
			catch (Exception ex)
			{
				Logger.Log(LogLevel.Error, $"Failed to restore backup for {relativePath}: {ex.Message}");
				failedRestorations.Add(relativePath);
			}
		}

		if (failedRestorations.Count > 0)
		{
			Logger.Log(LogLevel.Error, $"Failed to restore {failedRestorations.Count} files from backup:");
			foreach (var failedFile in failedRestorations)
			{
				Logger.Log(LogLevel.Error, $"  - {failedFile}");
			}
			Logger.Log(LogLevel.Warning, $"Some files may still be locked by other processes. Please close all programs using game files and manually restore these files from the Backups folder if needed.");
			return false;
		}

		try
		{
			Directory.Delete(BackupFolderPath, true);
			Logger.Log(LogLevel.Debug, $"Successfully deleted backup folder after restoration");
		}
		catch (Exception ex)
		{
			Logger.Log(LogLevel.Warning, $"Could not delete backup folder after restoration: {ex.Message}");
			// Don't return false here - the restoration was successful, just cleanup failed
		}

		return true;
	}

	public static string? CreateBackup(string filePath)
	{
		if (SettingsHolder.InstallPath == null)
			return null;

		if (!Directory.Exists(BackupFolderPath))
			Directory.CreateDirectory(BackupFolderPath);

		var relativePath = Path.GetRelativePath(SettingsHolder.InstallPath, filePath);
		var backupPath = Path.Combine(BackupFolderPath, relativePath);

		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
		}
		catch
		{
			Logger.Log(LogLevel.Error, $"Cannot create necessary directory: {relativePath} {backupPath}");
		}

		// Never overwrite existing backups if two mods modify the same file.
		if (!File.Exists(backupPath))
			File.Copy(filePath, backupPath, false);

		return backupPath;
	}

	public static string? GetBackupPath(string filePath)
	{
		if (SettingsHolder.InstallPath == null)
			return null;

		var backupPath = Path.Combine(BackupFolderPath, Path.GetRelativePath(SettingsHolder.InstallPath, filePath));
		return File.Exists(backupPath) ? backupPath : null;
	}

	public static void DeleteAllBackups()
	{
		if (SettingsHolder.InstallPath == null)
			return;

		if (Directory.Exists(BackupFolderPath))
			Directory.Delete(BackupFolderPath, true);
	}

	public static bool TryDeleteBackup(string filePath)
	{
		if (SettingsHolder.InstallPath == null)
			return false;
		var backupPath = GetBackupPath(filePath);
		if (backupPath == null)
			return false;
		try
		{
			File.Delete(backupPath);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log(LogLevel.Error, $"Failed to delete backup file: {ex.Message}");
			return false;
		}
	}
}