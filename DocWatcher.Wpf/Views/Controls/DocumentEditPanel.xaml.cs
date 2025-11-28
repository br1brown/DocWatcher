using DocWatcher.Core;
using DocWatcher.Core.Dtos;
using DocWatcher.Core.Services;
using DocWatcher.Wpf.DTO;
using DocWatcher.Wpf.Validation;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DocWatcher.Wpf.Views
{
	public enum DocumentEditResult
	{
		Saved,
		Deleted,
		Canceled
	}

	public class DocumentEditCompletedEventArgs : EventArgs
	{
		public DocumentEditResult Result { get; }
		public DocumentDto? Document { get; }

		public DocumentEditCompletedEventArgs(DocumentEditResult result, DocumentDto? document)
		{
			Result = result;
			Document = document;
		}
	}

	public partial class DocumentEditPanel : UserControl
	{
		private DocumentController? _documentController;
		private DocumentDto? _document;
		private bool _isEdit;

		public DocumentEditPanel()
		{
			InitializeComponent();
		}

		// ==================  DEPENDENCY PROPERTIES  ==================

		public string? TitleText
		{
			get => (string?)GetValue(TitleTextProperty);
			set => SetValue(TitleTextProperty, value);
		}

		public static readonly DependencyProperty TitleTextProperty =
			DependencyProperty.Register(
				nameof(TitleText),
				typeof(string),
				typeof(DocumentEditPanel),
				new PropertyMetadata(string.Empty));

		public DateTime? DueDate
		{
			get => (DateTime?)GetValue(DueDateProperty);
			set => SetValue(DueDateProperty, value);
		}

		public static readonly DependencyProperty DueDateProperty =
			DependencyProperty.Register(
				nameof(DueDate),
				typeof(DateTime?),
				typeof(DocumentEditPanel),
				new PropertyMetadata(null));

		public string? FilePath
		{
			get => (string?)GetValue(FilePathProperty);
			set => SetValue(FilePathProperty, value);
		}

		public static readonly DependencyProperty FilePathProperty =
			DependencyProperty.Register(
				nameof(FilePath),
				typeof(string),
				typeof(DocumentEditPanel),
				new PropertyMetadata(string.Empty));

		// ==================  INIZIALIZZAZIONE  ==================

		/// <summary>
		/// Inizializza il pannello in modalità NUOVO (document == null)
		/// o MODIFICA (document valorizzato).
		/// </summary>
		public void Initialize(DocumentController documentController, DocumentDto? document)
		{
			_documentController = documentController ?? throw new ArgumentNullException(nameof(documentController));
			_document = document;
			_isEdit = document is not null;

			if (_isEdit)
			{
				TitleText = document!.Titolo;
				DueDate = document.DataScadenza;
				FilePath = document.PercorsoAllegato ?? string.Empty;
			}
			else
			{
				TitleText = string.Empty;
				DueDate = DateTime.Today;
				FilePath = string.Empty;
			}
		}

		// ==================  EVENTO DI COMPLETAMENTO  ==================

		public event EventHandler<DocumentEditCompletedEventArgs>? Completed;

		private Window? OwnerWindow => Window.GetWindow(this) ?? Application.Current.MainWindow;

		// ==================  HANDLER BOTTONI  ==================

		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			if (_documentController is null)
			{
				MessageBox.Show(OwnerWindow,
					"Pannello non inizializzato correttamente.",
					"Errore",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
				return;
			}

			var input = new DocumentInput
			{
				Titolo = TitleText,
				DataScadenza = DueDate,
				PercorsoAllegato = FilePath
			};

			var errors = DocumentValidator.Validate(input);
			if (errors.Any())
			{
				MessageBox.Show(OwnerWindow,
					string.Join(Environment.NewLine, errors),
					"Campi mancanti",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}

			if (_isEdit && _document is not null)
			{
				// MODIFICA DOCUMENTO ESISTENTE
				_document.Titolo = input.Titolo!.Trim();
				_document.DataScadenza = input.DataScadenza!.Value.Date;
				_document.PercorsoAllegato = string.IsNullOrWhiteSpace(input.PercorsoAllegato)
					? null
					: input.PercorsoAllegato.Trim();

				await _documentController.UpdateAsync(_document);

				MessageBox.Show(OwnerWindow,
					"Documento aggiornato.",
					"Modifica",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			else
			{
				// NUOVO DOCUMENTO
				var newDoc = new DocumentDto
				{
					Titolo = input.Titolo!.Trim(),
					DataScadenza = input.DataScadenza!.Value.Date,
					PercorsoAllegato = string.IsNullOrWhiteSpace(input.PercorsoAllegato)
						? null
						: input.PercorsoAllegato.Trim()
				};

				await _documentController.CreateAsync(newDoc);

				_document = newDoc;
				_isEdit = true;

				MessageBox.Show(OwnerWindow,
					"Documento creato.",
					"Nuovo documento",
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}

			Completed?.Invoke(this,
				new DocumentEditCompletedEventArgs(DocumentEditResult.Saved, _document));
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Completed?.Invoke(this,
				new DocumentEditCompletedEventArgs(DocumentEditResult.Canceled, _document));
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			if (!_isEdit || _documentController is null || _document is null)
			{
				// Niente da eliminare → lo tratto come cancel
				Completed?.Invoke(this,
					new DocumentEditCompletedEventArgs(DocumentEditResult.Canceled, _document));
				return;
			}

			var result = MessageBox.Show(OwnerWindow,
				"Eliminare il documento selezionato?",
				"Conferma eliminazione",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			if (result != MessageBoxResult.Yes)
				return;

			await _documentController.DeleteAsync(_document.Id.Value);

			MessageBox.Show(OwnerWindow,
				"Documento eliminato.",
				"Elimina",
				MessageBoxButton.OK,
				MessageBoxImage.Information);

			Completed?.Invoke(this,
				new DocumentEditCompletedEventArgs(DocumentEditResult.Deleted, _document));
		}

		private void Browse_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				Title = "Seleziona il file da collegare",
				Filter = "Tutti i file (*.*)|*.*",
				CheckFileExists = true,
				CheckPathExists = true
			};

			if (dialog.ShowDialog(OwnerWindow) == true)
			{
				FilePath = dialog.FileName;
			}
		}
	}
}
