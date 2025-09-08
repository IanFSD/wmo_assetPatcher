using WMO.Core.Logging;
using WMO.Core.Services;

namespace WMO.Core.Helpers;

public static class BackupManager
{
	private const string BACKUPS_RELATIVE_PATH = "Backups";

	// Safe access to backup folder path - uses default path if GamePath is null
	private static string BackupFolderPath => Path.Combine(SettingsService.Current.GamePath ?? SettingsService.DEFAULT_GAME_PATH, BACKUPS_RELATIVE_PATH);

    public static bool RecoverBackups()
	{
		Logger.Log(LogLevel.Info, $"Starting backup recovery process...");
		
		// Get the effective install path (default if null)
		var effectiveInstallPath = SettingsService.Current.GamePath ?? SettingsService.DEFAULT_GAME_PATH;
		Logger.Log(LogLevel.Debug, $"Using install path: {effectiveInstallPath}");
		Logger.Log(LogLevel.Debug, $"Backup folder path: {BackupFolderPath}");
		
		if (!Directory.Exists(BackupFolderPath))
		{
			Logger.Log(LogLevel.Info, $"No backup folder found at {BackupFolderPath} - nothing to recover");
			return true;
		}

		Logger.Log(LogLevel.Info, $"Backup folder exists, scanning for backup files...");
		var backupFiles = Directory.GetFiles(BackupFolderPath, "*.*", SearchOption.AllDirectories);
		Logger.Log(LogLevel.Info, $"Found {backupFiles.Length} backup files to restore");

		if (backupFiles.Length == 0)
		{
			Logger.Log(LogLevel.Info, $"No backup files found in backup folder");
			return true;
		}

		var failedRestorations = new List<string>();
		var successfulRestorations = 0;

		foreach (var backup in backupFiles)
		{
			var relativePath = Path.GetRelativePath(BackupFolderPath, backup);
			var originalPath = Path.Combine(effectiveInstallPath, relativePath);
			
			Logger.Log(LogLevel.Debug, $"Processing backup file: {relativePath}");
			Logger.Log(LogLevel.Trace, $"  Backup path: {backup}");
			Logger.Log(LogLevel.Trace, $"  Target path: {originalPath}");

			if (!File.Exists(originalPath))
			{
				Logger.Log(LogLevel.Error, $"Backup restoration failed: Original file does not exist at {originalPath}");
				Logger.Log(LogLevel.Error, $"This backup file cannot be restored: {relativePath}");
				ErrorHandler.Handle("A backup is present for a file not present in the original directory. " +
									"The backup could not be restored properly", null);
				return false;
			}

			try
			{
				Logger.Log(LogLevel.Debug, $"Copying backup file to original location: {relativePath}");
				File.Copy(backup, originalPath, true);
				
				Logger.Log(LogLevel.Debug, $"Deleting backup file: {backup}");
				File.Delete(backup);
				
				successfulRestorations++;
				Logger.Log(LogLevel.Debug, $"Successfully restored file from backup: {relativePath}");
			}
			catch (UnauthorizedAccessException ex)
			{
				Logger.Log(LogLevel.Error, $"Access denied while restoring backup for {relativePath}");
				Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
				Logger.Log(LogLevel.Error, $"Exception message: {ex.Message}");
				Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
				failedRestorations.Add(relativePath);
			}
			catch (IOException ex) when (ex.HResult == -2147024864) // File is being used by another process
			{
				Logger.Log(LogLevel.Error, $"Cannot restore {relativePath} - file is in use by another process");
				Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
				Logger.Log(LogLevel.Error, $"Exception message: {ex.Message}");
				Logger.Log(LogLevel.Error, $"Exception HResult: 0x{ex.HResult:X8}");
				Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
				failedRestorations.Add(relativePath);
			}
			catch (Exception ex)
			{
				Logger.Log(LogLevel.Error, $"Failed to restore backup for {relativePath}");
				Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
				Logger.Log(LogLevel.Error, $"Exception message: {ex.Message}");
				Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
				if (ex.InnerException != null)
				{
					Logger.Log(LogLevel.Error, $"Inner exception type: {ex.InnerException.GetType().FullName}");
					Logger.Log(LogLevel.Error, $"Inner exception message: {ex.InnerException.Message}");
					Logger.Log(LogLevel.Error, $"Inner exception stack trace: {ex.InnerException.StackTrace}");
				}
				failedRestorations.Add(relativePath);
			}
		}

		Logger.Log(LogLevel.Info, $"Backup restoration summary: {successfulRestorations} successful, {failedRestorations.Count} failed");

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
			Logger.Log(LogLevel.Debug, $"Attempting to delete backup folder: {BackupFolderPath}");
			Directory.Delete(BackupFolderPath, true);
			Logger.Log(LogLevel.Info, $"Successfully deleted backup folder after restoration");
		}
		catch (Exception ex)
		{
			Logger.Log(LogLevel.Warning, $"Could not delete backup folder after restoration");
			Logger.Log(LogLevel.Warning, $"Exception type: {ex.GetType().FullName}");
			Logger.Log(LogLevel.Warning, $"Exception message: {ex.Message}");
			Logger.Log(LogLevel.Warning, $"Stack trace: {ex.StackTrace}");
			// Don't return false here - the restoration was successful, just cleanup failed
		}

		Logger.Log(LogLevel.Info, $"Backup recovery process completed successfully");
		return true;
	}

	public static string? CreateBackup(string filePath)
	{
		Logger.Log(LogLevel.Debug, $"Starting backup creation for file: {filePath}");
		
		// Get the effective install path (default if null)
		var effectiveInstallPath = SettingsService.Current.GamePath ?? SettingsService.DEFAULT_GAME_PATH;
		Logger.Log(LogLevel.Debug, $"Using install path: {effectiveInstallPath}");
		Logger.Log(LogLevel.Debug, $"Backup folder path: {BackupFolderPath}");

		if (!Directory.Exists(BackupFolderPath))
		{
			Logger.Log(LogLevel.Debug, $"Backup folder does not exist, creating: {BackupFolderPath}");
			try
			{
				Directory.CreateDirectory(BackupFolderPath);
				Logger.Log(LogLevel.Debug, $"Successfully created backup folder");
			}
			catch (Exception ex)
			{
				Logger.Log(LogLevel.Error, $"Failed to create backup folder: {BackupFolderPath}");
				Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
				Logger.Log(LogLevel.Error, $"Exception message: {ex.Message}");
				Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
				return null;
			}
		}

		var relativePath = Path.GetRelativePath(effectiveInstallPath, filePath);
		var backupPath = Path.Combine(BackupFolderPath, relativePath);
		
		Logger.Log(LogLevel.Debug, $"Relative path: {relativePath}");
		Logger.Log(LogLevel.Debug, $"Target backup path: {backupPath}");

		var backupDirectory = Path.GetDirectoryName(backupPath);
		if (backupDirectory != null && !Directory.Exists(backupDirectory))
		{
			Logger.Log(LogLevel.Debug, $"Creating backup subdirectory: {backupDirectory}");
			try
			{
				Directory.CreateDirectory(backupDirectory);
				Logger.Log(LogLevel.Debug, $"Successfully created backup subdirectory");
			}
			catch (Exception ex)
			{
				Logger.Log(LogLevel.Error, $"Cannot create necessary backup directory: {backupDirectory}");
				Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
				Logger.Log(LogLevel.Error, $"Exception message: {ex.Message}");
				Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
				return null;
			}
		}

		// Check if backup already exists
		if (File.Exists(backupPath))
		{
			Logger.Log(LogLevel.Info, $"Backup already exists for file: {relativePath}");
			Logger.Log(LogLevel.Debug, $"Existing backup path: {backupPath}");
			return backupPath;
		}

		// Verify source file exists
		if (!File.Exists(filePath))
		{
			Logger.Log(LogLevel.Error, $"Cannot create backup: Source file does not exist: {filePath}");
			return null;
		}

		try
		{
			Logger.Log(LogLevel.Debug, $"Copying file to backup location...");
			var sourceFileInfo = new FileInfo(filePath);
			Logger.Log(LogLevel.Debug, $"Source file size: {sourceFileInfo.Length} bytes");
			Logger.Log(LogLevel.Debug, $"Source file last modified: {sourceFileInfo.LastWriteTime}");
			
			File.Copy(filePath, backupPath, false);
			
			var backupFileInfo = new FileInfo(backupPath);
			Logger.Log(LogLevel.Debug, $"Backup file size: {backupFileInfo.Length} bytes");
			Logger.Log(LogLevel.Info, $"Successfully created backup for: {relativePath}");
			
			// Verify backup integrity
			if (sourceFileInfo.Length != backupFileInfo.Length)
			{
				Logger.Log(LogLevel.Warning, $"Backup file size mismatch! Source: {sourceFileInfo.Length}, Backup: {backupFileInfo.Length}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log(LogLevel.Error, $"Failed to create backup for file: {filePath}");
			Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
			Logger.Log(LogLevel.Error, $"Exception message: {ex.Message}");
			Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Logger.Log(LogLevel.Error, $"Inner exception type: {ex.InnerException.GetType().FullName}");
				Logger.Log(LogLevel.Error, $"Inner exception message: {ex.InnerException.Message}");
				Logger.Log(LogLevel.Error, $"Inner exception stack trace: {ex.InnerException.StackTrace}");
			}
			return null;
		}

		return backupPath;
	}

	public static string? GetBackupPath(string filePath)
	{
		var effectiveInstallPath = SettingsService.Current.GamePath ?? SettingsService.DEFAULT_GAME_PATH;
		var backupPath = Path.Combine(BackupFolderPath, Path.GetRelativePath(effectiveInstallPath, filePath));
		return File.Exists(backupPath) ? backupPath : null;
	}

	public static void DeleteAllBackups()
	{
		Logger.Log(LogLevel.Info, $"Starting deletion of all backup files...");
		Logger.Log(LogLevel.Debug, $"Backup folder path: {BackupFolderPath}");

		if (!Directory.Exists(BackupFolderPath))
		{
			Logger.Log(LogLevel.Info, $"No backup folder found at {BackupFolderPath} - nothing to delete");
			return;
		}

		try
		{
			var backupFiles = Directory.GetFiles(BackupFolderPath, "*.*", SearchOption.AllDirectories);
			Logger.Log(LogLevel.Info, $"Found {backupFiles.Length} backup files to delete");
			
			Directory.Delete(BackupFolderPath, true);
			Logger.Log(LogLevel.Info, $"Successfully deleted all backup files and folder");
		}
		catch (Exception ex)
		{
			Logger.Log(LogLevel.Error, $"Failed to delete backup folder: {BackupFolderPath}");
			Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
			Logger.Log(LogLevel.Error, $"Exception message: {ex.Message}");
			Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
		}
	}

	public static bool TryDeleteBackup(string filePath)
	{
		Logger.Log(LogLevel.Debug, $"Attempting to delete backup for file: {filePath}");
		
		var backupPath = GetBackupPath(filePath);
		if (backupPath == null)
		{
			Logger.Log(LogLevel.Debug, $"No backup found for file: {filePath}");
			return false;
		}
		
		Logger.Log(LogLevel.Debug, $"Backup path found: {backupPath}");
		
		try
		{
			if (File.Exists(backupPath))
			{
				var fileInfo = new FileInfo(backupPath);
				Logger.Log(LogLevel.Debug, $"Deleting backup file (size: {fileInfo.Length} bytes): {backupPath}");
				File.Delete(backupPath);
				Logger.Log(LogLevel.Debug, $"Successfully deleted backup file: {backupPath}");
			}
			else
			{
				Logger.Log(LogLevel.Warning, $"Backup file does not exist: {backupPath}");
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log(LogLevel.Error, $"Failed to delete backup file: {backupPath}");
			Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
			Logger.Log(LogLevel.Error, $"Exception message: {ex.Message}");
			Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
			return false;
		}
	}

	/// <summary>
	/// Scans for and deletes outdated backup data on startup
	/// </summary>
	public static void CleanupOutdatedBackups()
	{
		Logger.Log(LogLevel.Info, $"Starting cleanup of outdated backup data...");
		
		// Get the effective install path (default if null)
		var effectiveInstallPath = SettingsService.Current.GamePath ?? SettingsService.DEFAULT_GAME_PATH;
		Logger.Log(LogLevel.Debug, $"Using install path: {effectiveInstallPath}");
		Logger.Log(LogLevel.Debug, $"Checking backup folder: {BackupFolderPath}");

		if (!Directory.Exists(BackupFolderPath))
		{
			Logger.Log(LogLevel.Info, $"No backup folder found - no cleanup needed");
			return;
		}

		try
		{
			var backupFiles = Directory.GetFiles(BackupFolderPath, "*.*", SearchOption.AllDirectories);
			Logger.Log(LogLevel.Info, $"Found {backupFiles.Length} backup files to analyze");

			if (backupFiles.Length == 0)
			{
				Logger.Log(LogLevel.Info, $"Backup folder is empty, removing empty folder");
				Directory.Delete(BackupFolderPath, true);
				return;
			}

			var outdatedFiles = new List<string>();
			var validFiles = new List<string>();
			var orphanedFiles = new List<string>();

			foreach (var backupFile in backupFiles)
			{
				var relativePath = Path.GetRelativePath(BackupFolderPath, backupFile);
				var originalPath = Path.Combine(effectiveInstallPath, relativePath);
				
				Logger.Log(LogLevel.Trace, $"Analyzing backup file: {relativePath}");
				Logger.Log(LogLevel.Trace, $"  Backup path: {backupFile}");
				Logger.Log(LogLevel.Trace, $"  Original path: {originalPath}");

				// Check if original file still exists
				if (!File.Exists(originalPath))
				{
					Logger.Log(LogLevel.Debug, $"Original file no longer exists, marking backup as orphaned: {relativePath}");
					orphanedFiles.Add(backupFile);
					continue;
				}

				try
				{
					var backupInfo = new FileInfo(backupFile);
					var originalInfo = new FileInfo(originalPath);

					Logger.Log(LogLevel.Trace, $"  Backup size: {backupInfo.Length}, Original size: {originalInfo.Length}");
					Logger.Log(LogLevel.Trace, $"  Backup modified: {backupInfo.LastWriteTime}, Original modified: {originalInfo.LastWriteTime}");

					// Check if backup is outdated (original file is newer or different size)
					if (originalInfo.LastWriteTime > backupInfo.LastWriteTime || originalInfo.Length != backupInfo.Length)
					{
						Logger.Log(LogLevel.Debug, $"Backup is outdated (original file modified or size changed): {relativePath}");
						outdatedFiles.Add(backupFile);
					}
					else
					{
						Logger.Log(LogLevel.Debug, $"Backup is current: {relativePath}");
						validFiles.Add(backupFile);
					}
				}
				catch (Exception ex)
				{
					Logger.Log(LogLevel.Warning, $"Error analyzing backup file {relativePath}, treating as outdated");
					Logger.Log(LogLevel.Warning, $"Exception type: {ex.GetType().FullName}");
					Logger.Log(LogLevel.Warning, $"Exception message: {ex.Message}");
					outdatedFiles.Add(backupFile);
				}
			}

			Logger.Log(LogLevel.Info, $"Backup analysis complete: {validFiles.Count} valid, {outdatedFiles.Count} outdated, {orphanedFiles.Count} orphaned");

			// Delete outdated and orphaned files
			var deletedCount = 0;
			var failedDeletions = new List<string>();

			foreach (var file in outdatedFiles.Concat(orphanedFiles))
			{
				try
				{
					var relativePath = Path.GetRelativePath(BackupFolderPath, file);
					Logger.Log(LogLevel.Debug, $"Deleting outdated/orphaned backup: {relativePath}");
					File.Delete(file);
					deletedCount++;
				}
				catch (Exception ex)
				{
					var relativePath = Path.GetRelativePath(BackupFolderPath, file);
					Logger.Log(LogLevel.Error, $"Failed to delete outdated backup: {relativePath}");
					Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
					Logger.Log(LogLevel.Error, $"Exception message: {ex.Message}");
					failedDeletions.Add(relativePath);
				}
			}

			Logger.Log(LogLevel.Info, $"Deleted {deletedCount} outdated/orphaned backup files");
			
			if (failedDeletions.Count > 0)
			{
				Logger.Log(LogLevel.Warning, $"Failed to delete {failedDeletions.Count} backup files:");
				foreach (var failedFile in failedDeletions)
				{
					Logger.Log(LogLevel.Warning, $"  - {failedFile}");
				}
			}

			// Clean up empty directories
			try
			{
				CleanupEmptyDirectories(BackupFolderPath);
			}
			catch (Exception ex)
			{
				Logger.Log(LogLevel.Warning, $"Error cleaning up empty backup directories");
				Logger.Log(LogLevel.Warning, $"Exception type: {ex.GetType().FullName}");
				Logger.Log(LogLevel.Warning, $"Exception message: {ex.Message}");
			}

			// If no valid backups remain, delete the entire backup folder
			if (validFiles.Count == 0 && deletedCount > 0)
			{
				try
				{
					if (Directory.Exists(BackupFolderPath))
					{
						var remainingFiles = Directory.GetFiles(BackupFolderPath, "*.*", SearchOption.AllDirectories);
						if (remainingFiles.Length == 0)
						{
							Logger.Log(LogLevel.Info, $"No valid backups remain, removing backup folder");
							Directory.Delete(BackupFolderPath, true);
						}
					}
				}
				catch (Exception ex)
				{
					Logger.Log(LogLevel.Warning, $"Error removing empty backup folder");
					Logger.Log(LogLevel.Warning, $"Exception type: {ex.GetType().FullName}");
					Logger.Log(LogLevel.Warning, $"Exception message: {ex.Message}");
				}
			}

			Logger.Log(LogLevel.Info, $"Backup cleanup completed successfully");
		}
		catch (Exception ex)
		{
			Logger.Log(LogLevel.Error, $"Error during backup cleanup");
			Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
			Logger.Log(LogLevel.Error, $"Exception message: {ex.Message}");
			Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
		}
	}

	/// <summary>
	/// Recursively removes empty directories from the backup folder
	/// </summary>
	private static void CleanupEmptyDirectories(string path)
	{
		Logger.Log(LogLevel.Trace, $"Checking for empty directories in: {path}");
		
		if (!Directory.Exists(path))
			return;

		// Clean up subdirectories first
		foreach (var directory in Directory.GetDirectories(path))
		{
			CleanupEmptyDirectories(directory);
		}

		// Check if this directory is now empty
		try
		{
			if (Directory.GetFileSystemEntries(path).Length == 0)
			{
				var relativePath = Path.GetRelativePath(BackupFolderPath, path);
				Logger.Log(LogLevel.Debug, $"Removing empty backup directory: {relativePath}");
				Directory.Delete(path);
			}
		}
		catch (Exception ex)
		{
			var relativePath = Path.GetRelativePath(BackupFolderPath, path);
			Logger.Log(LogLevel.Warning, $"Failed to remove empty directory: {relativePath}");
			Logger.Log(LogLevel.Warning, $"Exception message: {ex.Message}");
		}
	}
}
