using System;
using System.IO;
using System.Text.Json;

namespace DocWatcher.Wpf;

public class AppConfig
{
    public int NotifySpanDays { get; set; } = 60;

    private const string FileName = "docwatcher.config.json";
	public bool NotifyAlwaysOnStartup { get; set; } = true;
	public bool BGStartup { get; set; } = false;      // avvia automaticamente

	public static AppConfig Load()
    {
        try
        {
            if (File.Exists(FileName))
            {
                var json = File.ReadAllText(FileName);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch
        {
            // Ignore errors and use default
        }

        return new AppConfig();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(FileName, json);
    }
}
