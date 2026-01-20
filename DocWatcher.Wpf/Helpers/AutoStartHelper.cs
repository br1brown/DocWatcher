using DocWatcher.Core.Services;
using Microsoft.Win32;
using System;
using System.Diagnostics;

public static class AutoStartHelper
{
	private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
	private const string AppName = "DocWatcher";

	public static void EnsureAutoStart()
	{
#if DEBUG
		// In debug non facciamo nulla, cosi non sporco il Run
		return;
#endif
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
			if (key is null)
				return;

			string exePath = Process.GetCurrentProcess().MainModule!.FileName!;
			string value = $"\"{exePath}\" --background";
			var current = key.GetValue(AppName) as string;

			if (!string.Equals(current, value, StringComparison.OrdinalIgnoreCase))
			{
				key.SetValue(AppName, value);
			}
		}
		catch (Exception ex)
		{
			LogHelper.Log(ex, "AutoStartHelper.EnsureAutoStart");
			throw;
		}
	}

	public static void RemoveAutoStart()
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
			if (key is null)
				return;

			var current = key.GetValue(AppName);
			if (current is not null)
			{
				key.DeleteValue(AppName, throwOnMissingValue: false);
			}
		}
		catch (Exception ex)
		{
			LogHelper.Log(ex, "AutoStartHelper.RemoveAutoStart");
			throw;
		}
	}

	public static bool IsAutoStartEnabled()
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
			if (key is null)
				return false;

			return key.GetValue(AppName) is not null;
		}
		catch (Exception ex)
		{
			LogHelper.Log(ex, "AutoStartHelper.IsAutoStartEnabled");
			return false;
		}
	}
}
