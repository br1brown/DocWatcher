using DocWatcher.Core;
using DocWatcher.Core.Dtos;
using DocWatcher.Wpf.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;

namespace DocWatcher.Wpf.Views;

public partial class ImportCsvWindow : Window
{
    private readonly string _csvPath;
    private readonly DocumentController _DocumentController;

    private List<string> _headers = new();
    public ImportCsvWindow(string csvPath, DocumentController DocumentController)
    {
        InitializeComponent();
        _csvPath = csvPath;
        _DocumentController = DocumentController;

        Loaded += (_, _) => LoadPreview();
    }

    private void LoadPreview()
    {
        try
        {
            var (headers, _) = CsvImporter.LoadPreview(_csvPath);
            _headers = headers;

            CmbTitle.ItemsSource = headers;
            CmbDueDate.ItemsSource = headers;
            CmbPath.ItemsSource = headers;

            if (headers.Count > 0)
            {
                CmbTitle.SelectedIndex = 0;
                if (headers.Count > 1)
                    CmbDueDate.SelectedIndex = 1;
            }
        }
        catch (Exception ex)
        {
            ErrorHelper.Show(this, "Errore CSV", ex);
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => Close();

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        if (CmbTitle.SelectedIndex < 0 || CmbDueDate.SelectedIndex < 0)
        {
            MessageBox.Show(this,
                "Seleziona almeno le colonne per Titolo e Data scadenza.",
                "Mappatura incompleta",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        int idxTitle = CmbTitle.SelectedIndex;
        int idxDue = CmbDueDate.SelectedIndex;
        int? idxPath = CmbPath.SelectedIndex >= 0 ? CmbPath.SelectedIndex : null;

        List<DocumentDto> docs;
        try
        {
            IsEnabled = false;
            docs = await CsvImporter.MapFileToDocumentsAsync(_csvPath, idxTitle, idxDue, idxPath);
        }
        catch (Exception ex)
        {
            ErrorHelper.Show(this, "Errore importazione CSV", ex);
            return;
        }
        finally
        {
            IsEnabled = true;
        }

        if (docs.Count == 0)
        {
            MessageBox.Show(this,
                "Nessun documento valido trovato nel CSV.",
                "Importazione",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            int imported = await _DocumentController.BulkImportAsync(docs);

            MessageBox.Show(this,
                $"{imported} documenti importati correttamente.",
                "Importazione completata",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            ErrorHelper.Show(this, "Errore importazione CSV", ex);
        }
    }
}
