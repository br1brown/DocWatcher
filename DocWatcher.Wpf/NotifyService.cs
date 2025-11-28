using CommunityToolkit.WinUI.Notifications;
using DocWatcher.Core;
using System.Linq;
using System.Threading.Tasks; 

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
			//.AddAudio()
			.AddText("DocWatcher")
			.AddText($"Ci sono {count} documenti che scadono entro {giorni} giorni.")
			.Show();
	}
}
