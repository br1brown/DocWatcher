using DocWatcher.Core;
using DocWatcher.Core.Dtos;
using DocWatcher.Wpf.DTO;
using DocWatcher.Wpf.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DocWatcher.Wpf.Views;

public partial class MainWindow : Window
{
	private readonly DocumentController _documentController;
	private readonly AppConfig _config;

	private readonly ObservableCollection<DocumentRow> _rows = new();

	private bool _isEditMode;
	private bool _isLoading;

	private DocumentGridFilterMode _currentFilterMode = DocumentGridFilterMode.Expiring;
	private int _currentFilterDays;

	public MainWindow()
	{
		InitializeComponent();

		_documentController = App.DocumentController;
		_config = App.Config;

		_currentFilterDays = Math.Clamp(_config.FilterDays, 1, 600);

		GridPanel.ItemsSource = _rows;
		GridPanel.CurrentDaysFilter = _currentFilterDays;

		// Eventi dal pannello griglia
		GridPanel.SearchRequested += GridPanel_SearchRequested;
		GridPanel.SelectionChanged += GridPanel_SelectionChanged;

		// Eventi dal pannello di view
		ViewPanel.EditRequested += ViewPanel_EditRequested;
		ViewPanel.DeleteRequested += ViewPanel_DeleteRequested;
		ViewPanel.OpenFileRequested += ViewPanel_OpenFileRequested;

		// Evento dal pannello di edit
		EditPanel.Completed += EditPanel_Completed;

		Loaded += MainWindow_Loaded;
	}

	private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		await ReloadDataAsync(_currentFilterMode, _currentFilterDays);
	}

	#region Caricamento / filtro

	private async void GridPanel_SearchRequested(object? sender, DocumentSearchRequestedEventArgs e)
	{
		_currentFilterMode = e.Mode;
		_currentFilterDays = e.Days;
		await ReloadDataAsync(_currentFilterMode, _currentFilterDays);
	}

	private async Task ReloadDataAsync(DocumentGridFilterMode mode, int days)
	{
		try
		{
			SetLoading(true);
			_rows.Clear();

			List<DocumentDto> docs = mode switch
			{
				DocumentGridFilterMode.Expiring => await _documentController.GetExpiringAsync(days),
				DocumentGridFilterMode.Expired => await _documentController.GetExpiredAsync(),
				DocumentGridFilterMode.All => await _documentController.GetAllAsync(),
				_ => await _documentController.GetAllAsync()
			};

			foreach (var doc in docs.OrderBy(d => d.DataScadenza))
				_rows.Add(new DocumentRow(doc));

			GridPanel.SelectedItem = null;
			ShowNoSelection();
		}
		catch (Exception ex)
		{
			ErrorHelper.Show(this, "Errore caricamento dati", ex);
		}
		finally
		{
			SetLoading(false);
		}
	}

	private void SetLoading(bool isLoading)
	{
		_isLoading = isLoading;

		LoadingOverlay.Visibility = isLoading
			? Visibility.Visible
			: Visibility.Collapsed;

		GridPanel.IsEnabled = !isLoading;
		ViewPanel.IsEnabled = !isLoading;
		EditPanel.IsEnabled = !isLoading;
		BtnAddSingle.IsEnabled = !isLoading;
	}

	#endregion

	#region Selezione / dettaglio

	private void GridPanel_SelectionChanged(object? sender, EventArgs e)
	{
		_isEditMode = false;
		UpdateDetailPanels();
	}

	private void ShowNoSelection()
	{
		EmptyPanel.Visibility = Visibility.Visible;
		ViewPanel.Visibility = Visibility.Collapsed;
		EditPanel.Visibility = Visibility.Collapsed;
	}



	private void UpdateDetailPanels()
	{
		if (GridPanel.SelectedItem is not DocumentRow row)
		{
			ShowNoSelection();
			return;
		}

		EmptyPanel.Visibility = Visibility.Collapsed;

		var doc = row.Document;

		if (_isEditMode)
		{
			// EDIT MODE
			ViewPanel.Visibility = Visibility.Collapsed;
			EditPanel.Visibility = Visibility.Visible;

			EditPanel.Initialize(_documentController, doc);
		}
		else
		{
			// VIEW MODE
			EditPanel.Visibility = Visibility.Collapsed;
			ViewPanel.Visibility = Visibility.Visible;

			ViewPanel.Title = row.Titolo;
			ViewPanel.DueDate = row.DataScadenza;
			ViewPanel.FilePath = row.PercorsoAllegato ?? string.Empty;
			ViewPanel.Status = row.StatoTesto;
		}
	}

	#endregion

	#region Pannello View (DocumentViewPanel) eventi


	private void ViewPanel_EditRequested(object? sender, EventArgs e)
	{
		if (GridPanel.SelectedItem is not DocumentRow)
		{
			MessageBox.Show(this,
				"Seleziona un documento dalla lista.",
				"Modifica",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
			return;
		}

		_isEditMode = true;
		UpdateDetailPanels();
	}

	private async void ViewPanel_DeleteRequested(object? sender, EventArgs e)
	{
		await DeleteSelectedDocumentAsync();
	}

	private void ViewPanel_OpenFileRequested(object? sender, EventArgs e)
	{
		if (GridPanel.SelectedItem is not DocumentRow row)
			return;

		if (!string.IsNullOrWhiteSpace(row.PercorsoAllegato))
		{
			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = row.PercorsoAllegato,
					UseShellExecute = true,
					Verb = "open"
				};
				Process.Start(psi);
			}
			catch (Exception ex)
			{
				ErrorHelper.Show(this, "Errore apertura file", ex);
			}
		}
	}

	#endregion

	#region Pannello Edit (DocumentEditPanel) evento Completed

	private async void EditPanel_Completed(object? sender, DocumentEditCompletedEventArgs e)
	{
		_isEditMode = false;

		if (e.Result == DocumentEditResult.Canceled)
		{
			// Torno semplicemente alla view
			UpdateDetailPanels();
			return;
		}

		// Se salvato o eliminato, ricarico la lista
		await ReloadDataAsync(_currentFilterMode, _currentFilterDays);
	}

	#endregion

	#region Aggiunta documento

	private async void AddSingle_Click(object sender, RoutedEventArgs e)
	{
		var win = new DocumentNewWindow(_documentController)
		{
			Owner = this
		};

		if (win.ShowDialog() == true)
		{
			await ReloadDataAsync(_currentFilterMode, _currentFilterDays);
		}
	}

	#endregion

	#region Import / Export / Notifiche

	private void NotificationSettings_Click(object sender, RoutedEventArgs e)
	{
		var win = new SettingsWindow(_config)
		{
			Owner = this
		};

		if (win.ShowDialog() == true)
		{
			_currentFilterDays = _config.FilterDays;
			GridPanel.CurrentDaysFilter = _currentFilterDays;
		}
	}


	private async void ImportCsv_Click(object sender, RoutedEventArgs e)
	{
		var dlg = new OpenFileDialog
		{
			Filter = "File CSV|*.csv|Tutti i file|*.*",
			Multiselect = false
		};

		if (dlg.ShowDialog(this) == true)
		{
			var win = new ImportCsvWindow(dlg.FileName, _documentController)
			{
				Owner = this
			};

			win.ShowDialog();

			await ReloadDataAsync(_currentFilterMode, _currentFilterDays);
		}
	}

	private async void ExportCsv_Click(object sender, RoutedEventArgs e)
	{
		var dlg = new SaveFileDialog
		{
			Filter = "File CSV|*.csv",
			FileName = $"DocWatcher_export_{DateTime.Now:yyyyMMdd}.csv"
		};

		if (dlg.ShowDialog(this) == true)
		{
			try
			{
				var docs = await _documentController.GetAllAsync();
				using var writer = new StreamWriter(dlg.FileName);
				const char separator = ';';
				writer.WriteLine(string.Join(separator, "Titolo", "DataScadenza", "PercorsoAllegato"));

				foreach (var doc in docs)
				{
					var titolo = CsvImporter.EscapeCsv(doc.Titolo, separator);
					var data = CsvImporter.EscapeCsv(doc.DataScadenza.ToString("dd/MM/yyyy"), separator);
					var path = CsvImporter.EscapeCsv(doc.PercorsoAllegato, separator);
					writer.WriteLine($"{titolo}{separator}{data}{separator}{path}");
				}

				MessageBox.Show(this,
					"Esportazione completata.",
					"Esporta CSV",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				ErrorHelper.Show(this, "Errore esportazione CSV", ex);
			}
		}
	}

	#endregion

	#region Eliminazione comune

	private async Task DeleteSelectedDocumentAsync()
	{
		if (GridPanel.SelectedItem is not DocumentRow row)
			return;

		var res = MessageBox.Show(this,
			"Sei sicuro di voler eliminare questo documento?",
			"Conferma eliminazione",
			MessageBoxButton.YesNo,
			MessageBoxImage.Warning);

		if (res != MessageBoxResult.Yes)
			return;

		try
		{
			await _documentController.DeleteAsync(row.Id);
		}
		catch (Exception ex)
		{
			ErrorHelper.Show(this, "Errore eliminazione documento", ex);
			return;
		}

		MessageBox.Show(this,
			"Documento eliminato.",
			"Elimina",
			MessageBoxButton.OK,
			MessageBoxImage.Information);

		await ReloadDataAsync(_currentFilterMode, _currentFilterDays);
	}

	#endregion
}
