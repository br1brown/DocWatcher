using DocWatcher.Core;
using DocWatcher.Core.Dtos;
using System;
using System.Windows;

namespace DocWatcher.Wpf.Views;

public partial class DocumentNewWindow : Window
{
	private readonly DocumentController _DocumentController;
	private readonly DocumentDto? _existingDocument;
	private readonly bool _isEdit;

	// ==================  COSTRUTTORE: NUOVO DOCUMENTO  ==================

	public DocumentNewWindow(DocumentController DocumentController)
	{
		InitializeComponent();

		_DocumentController = DocumentController ?? throw new ArgumentNullException(nameof(DocumentController));
		_existingDocument = null;
		_isEdit = false;

		Title = "Nuovo documento";

		// inizializza il pannello in modalità "nuovo"
		EditPanel.Initialize(_DocumentController, null);
		EditPanel.Completed += EditPanel_Completed;
	}

	// ==================  COSTRUTTORE: MODIFICA DOCUMENTO ESISTENTE  ==================

	public DocumentNewWindow(DocumentController DocumentController, DocumentDto existingDocument)
	{
		InitializeComponent();

		_DocumentController = DocumentController ?? throw new ArgumentNullException(nameof(DocumentController));
		_existingDocument = existingDocument ?? throw new ArgumentNullException(nameof(existingDocument));
		_isEdit = true;

		Title = "Modifica documento";

		// inizializza il pannello in modalità "modifica"
		EditPanel.Initialize(_DocumentController, _existingDocument);
		EditPanel.Completed += EditPanel_Completed;
	}

	// ==================  HANDLER EVENTO Completed DEL PANNELLO  ==================

	private void EditPanel_Completed(object? sender, DocumentEditCompletedEventArgs e)
	{
		// Se l'utente ha annullato, DialogResult = false
		// Se ha salvato o eliminato, DialogResult = true
		try
		{
			DialogResult = e.Result != DocumentEditResult.Canceled;
		}
		catch { }
		Close();
	}
}
