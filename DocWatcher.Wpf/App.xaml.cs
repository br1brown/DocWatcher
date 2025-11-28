using CommunityToolkit.WinUI.Notifications;
using DocWatcher.Core;
using DocWatcher.Core.Data;
using DocWatcher.Wpf.Helpers;
using DocWatcher.Wpf.Views;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DocWatcher.Wpf;

public partial class App : Application
{
	// Dependency container o service locator pattern sarebbe meglio
	public static DocumentController DocumentController { get; private set; } = null!;
	public static AppConfig Config { get; private set; } = null!;

	private DocWatcherContext? _dbContext;
	private Mutex? _singleInstanceMutex;
	private NotifyService? _notifyService;

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		try
		{
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
		// 1. Carica configurazione
		Config = LoadConfiguration();

		// 2. Inizializza database
		_dbContext = InitializeDatabase();

		// 3. Inizializza servizi
		DocumentController = new DocumentController(_dbContext);
		_notifyService = new NotifyService(DocumentController, Config);

		// 4. Gestisci argomenti
		var args = ParseCommandLineArgs(e.Args);

		// 5. Gestisci single instance (se non in background)
		if (!args.IsBackground && !EnsureSingleInstance())
		{
			ActivateExistingInstance();
			Shutdown(0);
			return;
		}

		// 6. Gestisci notifiche
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

		// 7. Gestisci autostart
		if (Config.BGStartup)
		{
			AutoStartHelper.EnsureAutoStart();
		}

		// 8. Mostra finestra principale
		ShowMainWindow();
	}

	private static AppConfig LoadConfiguration()
	{
		try
		{
			return AppConfig.Load();
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(
				"Impossibile caricare la configurazione", ex);
		}
	}

	private static DocWatcherContext InitializeDatabase()
	{
		try
		{
			var context = new DocWatcherContext();
			context.Database.EnsureCreated();
			return context;
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
		bool createdNew;
		_singleInstanceMutex = new Mutex(
			true,
			"DocWatcher_Wpf_SingleInstance",
			out createdNew);

		if (!createdNew)
		{
			return false;
		}

		return true;
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
		catch
		{
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

	protected override void OnExit(ExitEventArgs e)
	{
		// Rimuovi event handler per evitare memory leak
		ToastNotificationManagerCompat.OnActivated -= Toast_OnActivated;

		// Rilascia risorse
		_dbContext?.Dispose();
		_singleInstanceMutex?.ReleaseMutex();
		_singleInstanceMutex?.Dispose();

		base.OnExit(e);
	}

	private class CommandLineArgs
	{
		public bool IsBackground { get; init; }
	}
}