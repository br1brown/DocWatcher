using System;
using System.Windows;

namespace DocWatcher.Wpf.Helpers;

public static class ErrorHelper
{
	public static void Show(Window? owner, string title, Exception ex)
	{
		var message = ex.InnerException is null
			? ex.Message
			: $"{ex.Message}\n{ex.InnerException.Message}";

		MessageBox.Show(
			owner ?? Application.Current.MainWindow,
			message,
			title,
			MessageBoxButton.OK,
			MessageBoxImage.Error);
	}
}
