using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocWatcher.Wpf.Helpers
{
	public static class AutoStartHelper
	{
		private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
		private const string AppName = "DocWatcher";

		public static void EnsureAutoStart()
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
	}
}
