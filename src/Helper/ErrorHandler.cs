using WMO.Logging;

namespace WMO.Helper;

public static class ErrorHandler {
	
	public static void Handle(string msg, Exception? e, bool skipLogging = false) { 	
		if (e != null)
		{
			var message = $"{msg}: {e.Message}";
			var details = GetExceptionDetails(e);
			
			if (!skipLogging)
			{
				Logger.Log(LogLevel.Error, $"{message}");
				Logger.Log(LogLevel.Error, $"{details}");
			}
		}
		else
		{
			if (!skipLogging)
				Logger.Log(LogLevel.Error, $"{msg}");
		}
	}
	
	private static string GetExceptionDetails(Exception ex)
	{
		var details = new List<string>
		{
			$"Exception Type: {ex.GetType().FullName}",
			$"Message: {ex.Message}",
			$"Stack Trace: {ex.StackTrace}"
		};
		
		if (ex.InnerException != null)
		{
			details.Add("--- Inner Exception ---");
			details.Add($"Inner Exception Type: {ex.InnerException.GetType().FullName}");
			details.Add($"Inner Exception Message: {ex.InnerException.Message}");
			details.Add($"Inner Exception Stack Trace: {ex.InnerException.StackTrace}");
		}
		
		return string.Join("\n", details);
	}
}