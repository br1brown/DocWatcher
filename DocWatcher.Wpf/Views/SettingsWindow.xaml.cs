using DocWatcher.Wpf; // per AppConfig
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DocWatcher.Wpf.Views
{
	public partial class SettingsWindow : Window
	{
		private readonly AppConfig _config;

		public SettingsWindow(AppConfig config)
		{
			InitializeComponent();
			LoadMarkdown();

			_config = config ?? throw new ArgumentNullException(nameof(config));

			InitializeControlsFromConfig();
		}

		private void LoadMarkdown()
		{
			var uri = new Uri("Resources/manuale.md", UriKind.Relative);
			var info = Application.GetResourceStream(uri);

			using var reader = new StreamReader(info.Stream);
			var md = reader.ReadToEnd();
			var htmlBody = Markdig.Markdown.ToHtml(md);
			string fullHtml = $@"
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{
            font-family: Segoe UI, sans-serif;
            margin: 20px;
            line-height: 1.5;
            font-size: 14px;
        }}
        h1 {{ font-size: 24px; margin-bottom: 10px; }}
        h2 {{ font-size: 20px; margin-top: 20px; margin-bottom: 10px; }}
        h3 {{ font-size: 17px; margin-top: 15px; margin-bottom: 10px; }}
        code {{
            background: #eee;
            padding: 3px 6px;
            border-radius: 4px;
        }}
        pre {{
            background: #eee;
            padding: 10px;
            border-radius: 4px;
            overflow-x: auto;
        }}
        ul {{ margin-left: 20px; }}
    </style>
</head>
<body>
{htmlBody}
</body>
</html>"; BrowserInfo.NavigateToString(fullHtml);
		}

		private void InitializeControlsFromConfig()
		{
			// NOTIFICHE
			TxtNotifySpanDays.Text = _config.NotifySpanDays.ToString();
			ChkNotifyAlwaysOnStartup.IsChecked = _config.NotifyAlwaysOnStartup;
			ChkBGStartup.IsChecked = _config.BGStartup;

			// FILTRI
			// NB: assicurati di aver aggiunto in AppConfig:
			// public int FilterDays { get; set; } = 60;
			TxtFilterDays.Text = _config.FilterDays.ToString();

			// TAB Info: per ora lasciamo il placeholder del XAML
		}

		private void BtnOk_Click(object sender, RoutedEventArgs e)
		{
			// Validazione input per i giorni di notifica
			if (!TryReadDays(TxtNotifySpanDays.Text, 1, 600, out var notifyDays))
			{
				MessageBox.Show(this,
					"Inserisci un numero di giorni valido per le notifiche (1-600).",
					"Valore non valido",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				TxtNotifySpanDays.Focus();
				TxtNotifySpanDays.SelectAll();
				return;
			}

			// Validazione input per i giorni di filtro
			if (!TryReadDays(TxtFilterDays.Text, 1, 600, out var filterDays))
			{
				MessageBox.Show(this,
					"Inserisci un numero di giorni valido per il filtro (1-600).",
					"Valore non valido",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);

				TxtFilterDays.Focus();
				TxtFilterDays.SelectAll();
				return;
			}

			// Applico i valori all'oggetto di config condiviso
			_config.NotifySpanDays = notifyDays;
			_config.FilterDays = filterDays;
			_config.NotifyAlwaysOnStartup = ChkNotifyAlwaysOnStartup.IsChecked == true;
			_config.BGStartup = ChkBGStartup.IsChecked == true;

			_config.SaveApply();

			// Chiudo con DialogResult = true nel caso il chiamante voglia reagire
			DialogResult = true;
			Close();
		}

		private static bool TryReadDays(string? text, int min, int max, out int value)
		{
			if (!int.TryParse(text, out value))
				return false;

			if (value < min || value > max)
				return false;

			return true;
		}
	}
}
