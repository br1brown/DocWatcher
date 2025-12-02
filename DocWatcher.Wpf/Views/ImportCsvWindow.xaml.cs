using System;
using System.Collections.Generic;
using System.Windows;
using DocWatcher.Core;
using DocWatcher.Wpf.Helpers;

namespace DocWatcher.Wpf.Views;

public partial class ImportCsvWindow : Window
{
    private readonly string _csvPath;
    private readonly DocumentController _DocumentController;

    private List<string> _headers = new();
    private List<string[]> _rows = new();

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
            var (headers, rows) = CsvImporter.LoadPreview(_csvPath);
            _headers = headers;
            _rows = rows;

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
            MessageBox.Show(this,
                $"Errore durante la lettura del CSV:\n{ex.Message}",
                "Errore CSV",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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

        var docs = CsvImporter.MapToDocuments(_rows, idxTitle, idxDue, idxPath);

        if (docs.Count == 0)
        {
            MessageBox.Show(this,
                "Nessun documento valido trovato nel CSV.",
                "Importazione",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        int imported = await _DocumentController.BulkImportAsync(docs);

        MessageBox.Show(this,
            $"{imported} documenti importati correttamente.",
            "Importazione completata",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        Close();
    }
}
