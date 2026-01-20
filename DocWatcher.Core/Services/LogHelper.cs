namespace DocWatcher.Core.Services;

public static class LogHelper
{
	public static void Log(Exception ex, string context)
	{
		try
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var logFolder = Path.Combine(appData, "DocWatcher");
			Directory.CreateDirectory(logFolder);

			var logPath = Path.Combine(logFolder, $"log-{DateTime.Now:yyyyMMdd}.log");
			var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {context} | {ex}\n";
			File.AppendAllText(logPath, line);
		}
		catch
		{
			// Best effort logging only.
		}
	}

	public static void CleanupOldLogs(int maxAgeDays)
	{
		try
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var logFolder = Path.Combine(appData, "DocWatcher");
			if (!Directory.Exists(logFolder))
				return;

			var cutoff = DateTime.Now.AddDays(-maxAgeDays);
			var files = Directory.GetFiles(logFolder, "log-*.log");
			foreach (var file in files)
			{
				var info = new FileInfo(file);
				if (info.LastWriteTime < cutoff)
					info.Delete();
			}
		}
		catch
		{
			// Best effort cleanup only.
		}
	}
}
