using WMO.Logging;

namespace WMO.Helper;

public static class ErrorHandler {
	
	public static void Handle(string msg, Exception? e, bool skipLogging = false) { 	
		var message = e != null ? ": " + e.Message : string.Empty;
		var stackTrace = e != null ? e.StackTrace + "\n\n" : string.Empty;
		
		if (!skipLogging)
			Logger.Log(LogLevel.Error, $"{message}\n{stackTrace}");
		
	}
}