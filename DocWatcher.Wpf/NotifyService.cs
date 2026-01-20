using CommunityToolkit.WinUI.Notifications;
using DocWatcher.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DocWatcher.Wpf;

public class NotifyService
{
	private readonly DocumentController _DocumentController;
	private readonly AppConfig _config;

	public NotifyService(DocumentController documentController, AppConfig config)
	{
		_DocumentController = documentController;
		_config = config;
	}

	public async Task RunNotify()
	{
		var result = await _DocumentController.GetExpiringAsync(_config.NotifySpanDays);

		if (!result.Any())
			return;

		ShowNotification(result.Count);
	}

	private void ShowNotification(int count)
	{
		int giorni = _config.NotifySpanDays;

		new ToastContentBuilder()
			.AddText($"Ci sono {count} documenti che scadono entro {giorni} giorni.")
			//.AddAppLogoOverride(new Uri("Resources/asset/ICO.png", UriKind.Relative))
			.Show();
	}
}
