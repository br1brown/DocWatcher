using DocWatcher.Core.Services;
using System;
using System.IO;
using System.Text.Json;

namespace DocWatcher.Wpf;

public class AppConfig
{
	public int NotifySpanDays { get; set; } = 60;
	public int FilterDays { get; set; } = 60;
	public bool NotifyAlwaysOnStartup { get; set; } = true;
	public bool BGStartup { get; set; } = false;

	public static AppConfig Load(bool apply)
	{
		try
		{
			if (File.Exists(FullPath()))
			{
				var json = File.ReadAllText(FullPath());
				var ret = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
				ret.NotifySpanDays = Math.Clamp(ret.NotifySpanDays, 1, 600);
				ret.FilterDays = Math.Clamp(ret.FilterDays, 1, 600);
				ret.BGStartup = AutoStartHelper.IsAutoStartEnabled();

				if (apply)
					ret.Apply();

				return ret;
			}
		}
		catch (Exception ex)
		{
			LogHelper.Log(ex, "AppConfig.Load");
		}

		return new AppConfig();
	}

	public void SaveApply()
	{
		if (this.BGStartup)
		{
			if (!AutoStartHelper.IsAutoStartEnabled())
				AutoStartHelper.EnsureAutoStart();
		}
		else
			AutoStartHelper.RemoveAutoStart();

		this.Save();
	}
	private void Apply()
	{
		if (this.BGStartup)
		{
			if (!AutoStartHelper.IsAutoStartEnabled())
				AutoStartHelper.EnsureAutoStart();
		}
		else
			AutoStartHelper.RemoveAutoStart();

	}

	private void Save()
	{
		try
		{
			var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			File.WriteAllText(FullPath(), json);
		}
		catch (Exception ex)
		{
			LogHelper.Log(ex, "AppConfig.Save");
			throw;
		}
	}

	private static string FullPath()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "docwatcher.config.json");
	}
}
