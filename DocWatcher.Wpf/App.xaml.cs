using CommunityToolkit.WinUI.Notifications;
using DocWatcher.Core;
using DocWatcher.Core.Data;
using DocWatcher.Core.Services;
using DocWatcher.Wpf.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace DocWatcher.Wpf;

public partial class App : Application
{
	private readonly IHost _host;

	// Bridge statico verso il container DI: usato dalle View (es. MainWindow)
	// che vengono istanziate da WPF e non ricevono i servizi via costruttore.
	public static DocumentController DocumentController { get; private set; } = null!;
	public static AppConfig Config { get; private set; } = null!;

	private Mutex? _singleInstanceMutex;
	private NotifyService? _notifyService;

	// Repository GitHub usato per il controllo degli aggiornamenti (Velopack).
	private const string GithubRepoUrl = "https://github.com/br1brown/DocWatcher";

	public App()
	{
		// DEVE essere la prima cosa eseguita: gestisce gli hook di
		// installazione/aggiornamento/disinstallazione di Velopack.
		VelopackApp.Build().Run();

		_host = Host.CreateDefaultBuilder()
			.ConfigureServices((_, services) =>
			{
				// IDbContextFactory + DocumentService + DocumentController
				services.AddDocWatcherCore();

				// Configurazione applicativa (caricata e applicata all'avvio)
				services.AddSingleton(_ => AppConfig.Load(true));

				// Servizio notifiche
				services.AddSingleton<NotifyService>();
			})
			.Build();
	}

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		LogHelper.CleanupOldLogs(7);

		try
		{
			await _host.StartAsync();
			await InitializeApplicationAsync(e);
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				$"Errore durante l'avvio dell'applicazione: {ex.Message}",
				"Errore critico",
				MessageBoxButton.OK,
				MessageBoxImage.Error);

			Shutdown(1);
		}
	}

	private async Task InitializeApplicationAsync(StartupEventArgs e)
	{
		// 1. Risolvi i servizi dal container
		Config = _host.Services.GetRequiredService<AppConfig>();
		DocumentController = _host.Services.GetRequiredService<DocumentController>();
		_notifyService = _host.Services.GetRequiredService<NotifyService>();

		// 2. Applica le migrazioni del database (crea/aggiorna lo schema)
		await InitializeDatabaseAsync();

		// 3. Gestisci argomenti
		var args = ParseCommandLineArgs(e.Args);

		// 4. Gestisci single instance (se non in background)
		if (!args.IsBackground && !EnsureSingleInstance())
		{
			ActivateExistingInstance();
			Shutdown(0);
			return;
		}

		// 5. Gestisci notifiche
		if (args.IsBackground || Config.NotifyAlwaysOnStartup)
		{
			ToastNotificationManagerCompat.OnActivated += Toast_OnActivated;
			await _notifyService.RunNotify();

			if (args.IsBackground)
			{
				Shutdown(0);
				return;
			}
		}

		// Controlla gli aggiornamenti in background (non blocca l'avvio).
		_ = CheckForUpdatesAsync();

		ShowMainWindow();
	}

	/// <summary>
	/// Cerca aggiornamenti sulle GitHub Releases tramite Velopack.
	/// Se presenti, li scarica e li applica al prossimo riavvio
	/// (senza interrompere la sessione corrente). No-op se l'app non e'
	/// stata installata tramite Velopack (es. esecuzione da Visual Studio).
	/// </summary>
	private static async Task CheckForUpdatesAsync()
	{
		try
		{
			var mgr = new UpdateManager(new GithubSource(GithubRepoUrl, accessToken: null, prerelease: false));

			if (!mgr.IsInstalled)
				return;

			var newVersion = await mgr.CheckForUpdatesAsync();
			if (newVersion is null)
				return;

			await mgr.DownloadUpdatesAsync(newVersion);
			mgr.WaitExitThenApplyUpdates(newVersion);
		}
		catch (Exception ex)
		{
			// Un errore di rete non deve compromettere l'avvio dell'app.
			LogHelper.Log(ex, "App.CheckForUpdatesAsync");
		}
	}

	private async Task InitializeDatabaseAsync()
	{
		try
		{
			var factory = _host.Services.GetRequiredService<IDbContextFactory<DocWatcherContext>>();
			await using var context = await factory.CreateDbContextAsync();
			await context.Database.MigrateAsync();
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(
				"Impossibile inizializzare il database", ex);
		}
	}

	private static CommandLineArgs ParseCommandLineArgs(string[] args)
	{
		var normalized = args.Select(a => a.ToLowerInvariant()).ToArray();
		return new CommandLineArgs
		{
			IsBackground = normalized.Contains("--background")
		};
	}

	private bool EnsureSingleInstance()
	{
		_singleInstanceMutex = new Mutex(
			true,
			"DocWatcher_Wpf_SingleInstance",
			out var createdNew);

		return createdNew;
	}

	private static void ActivateExistingInstance()
	{
		try
		{
			var currentProcess = Process.GetCurrentProcess();
			var processes = Process.GetProcessesByName("DocWatcher.Wpf");

			foreach (var process in processes)
			{
				if (process.Id == currentProcess.Id)
					continue;

				var handle = process.MainWindowHandle;
				if (handle == IntPtr.Zero)
					continue;

				// Ripristina se minimizzata
				if (IsIconic(handle))
				{
					ShowWindow(handle, SW_RESTORE);
				}

				// Porta in primo piano
				SetForegroundWindow(handle);
				break;
			}
		}
		catch (Exception ex)
		{
			LogHelper.Log(ex, "App.ActivateExistingInstance");
			// Se fallisce, non è grave: semplicemente non attiviamo la finestra
		}
	}

	// Import Win32
	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(IntPtr hWnd);

	private const int SW_RESTORE = 9;


	private void ShowMainWindow()
	{
		var mainWindow = new MainWindow();
		MainWindow = mainWindow;
		mainWindow.Show();
	}

	private void Toast_OnActivated(ToastNotificationActivatedEventArgsCompat e)
	{
		Dispatcher.Invoke(() =>
		{
			var main = EnsureMainWindowExists();
			BringMainWindowToFront(main);
		});
	}

	private MainWindow EnsureMainWindowExists()
	{
		if (MainWindow is MainWindow existing)
		{
			return existing;
		}

		var newWindow = new MainWindow();
		MainWindow = newWindow;
		newWindow.Show();
		return newWindow;
	}

	private static void BringMainWindowToFront(MainWindow window)
	{
		if (!window.IsVisible)
		{
			window.Show();
		}

		if (window.WindowState == WindowState.Minimized)
		{
			window.WindowState = WindowState.Normal;
		}

		window.Activate();
		window.Topmost = true;
		window.Topmost = false;
		window.Focus();
	}

	protected override async void OnExit(ExitEventArgs e)
	{
		// Rimuovi event handler per evitare memory leak
		ToastNotificationManagerCompat.OnActivated -= Toast_OnActivated;

		// Rilascia il mutex single-instance (solo se posseduto da questo thread)
		try
		{
			_singleInstanceMutex?.ReleaseMutex();
		}
		catch (ApplicationException)
		{
			// Il mutex non era posseduto da questo thread: niente da fare.
		}
		_singleInstanceMutex?.Dispose();

		// Arresta l'host e rilascia tutti i servizi (incluso il DbContextFactory)
		using (_host)
		{
			await _host.StopAsync(TimeSpan.FromSeconds(2));
		}

		base.OnExit(e);
	}

	private class CommandLineArgs
	{
		public bool IsBackground { get; init; }
	}
}
