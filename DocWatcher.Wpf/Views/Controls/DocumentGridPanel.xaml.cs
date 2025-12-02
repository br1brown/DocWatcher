using DocWatcher.Wpf.DTO;
using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;

namespace DocWatcher.Wpf.Views
{
	public enum DocumentGridFilterMode
	{
		All,
		Expiring,
		Expired
	}

	public class DocumentSearchRequestedEventArgs : EventArgs
	{
		public DocumentGridFilterMode Mode { get; }
		public int Days { get; }

		public DocumentSearchRequestedEventArgs(DocumentGridFilterMode mode, int days)
		{
			Mode = mode;
			Days = days;
		}
	}

	public partial class DocumentGridPanel : UserControl
	{
		// valore di default nel caso in cui nessuno lo imposti da fuori
		private int _expiringDays = 30;


		public DocumentGridPanel()
		{
			InitializeComponent();
			BuildFilterComboItems(DocumentGridFilterMode.Expiring);

		}
		private void BuildFilterComboItems(DocumentGridFilterMode? selectedMode = null)
		{
			if (CmbFilterType == null)
				return;

			if (selectedMode == null)
				selectedMode = CmbFilterType.SelectedValue as DocumentGridFilterMode?;

			CmbFilterType.ItemsSource = new[]
			{
				new {
					Text  = $"Documenti in scadenza entro {_expiringDays} giorni",
					Value = DocumentGridFilterMode.Expiring
				},
				new {
					Text  = "Documenti scaduti",
					Value = DocumentGridFilterMode.Expired
				},
				new {
					Text  = "Tutti i documenti",
					Value = DocumentGridFilterMode.All
				}
			};

			CmbFilterType.DisplayMemberPath = "Text";
			CmbFilterType.SelectedValuePath = "Value";

			if (selectedMode.HasValue)
			{
				CmbFilterType.SelectedValue = selectedMode.Value;
			}
			else
			{
				CmbFilterType.SelectedValue = DocumentGridFilterMode.Expiring;
			}
		}

		// ==============================
		//   DEPENDENCY PROPERTIES
		// ==============================

		/// <summary>
		/// Elenco dei DocumentRow da visualizzare nella griglia.
		/// MainWindow ci passa la sua ObservableCollection.
		/// </summary>
		public IEnumerable? ItemsSource
		{
			get => (IEnumerable?)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}

		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register(
				nameof(ItemsSource),
				typeof(IEnumerable),
				typeof(DocumentGridPanel),
				new PropertyMetadata(null));

		/// <summary>
		/// Documento selezionato nella griglia.
		/// </summary>
		public DocumentRow? SelectedItem
		{
			get => (DocumentRow?)GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register(
				nameof(SelectedItem),
				typeof(DocumentRow),
				typeof(DocumentGridPanel),
				new PropertyMetadata(null));


		public int CurrentDaysFilter
		{
			get => _expiringDays;
			set
			{
				_expiringDays = Math.Abs(value);
				if (CmbFilterType != null)
					BuildFilterComboItems(GetCurrentFilterMode());
			}
		}


		// ==============================
		//   EVENTI VERSO L'ESTERNO
		// ==============================

		/// <summary>
		/// Richiesta di ricerca: la MainWindow deve ricaricare i dati in base a Mode/Days.
		/// </summary>
		public event EventHandler<DocumentSearchRequestedEventArgs>? SearchRequested;

		/// <summary>
		/// Notifica che è cambiata la selezione nella griglia.
		/// </summary>
		public event EventHandler? SelectionChanged;

		// ==============================
		//   LOGICA FILTRO TIPO
		// ==============================

		private DocumentGridFilterMode GetCurrentFilterMode()
		{
			if (CmbFilterType.SelectedValue is DocumentGridFilterMode mode)
				return mode;

			// fallback
			return DocumentGridFilterMode.All;
		}


		// ==============================
		//   HANDLER PULSANTE CERCA
		// ==============================

		private void BtnSearch_Click(object sender, RoutedEventArgs e)
		{
			RaiseSearchRequested();
		}


		private void RaiseSearchRequested()
		{
			var mode = GetCurrentFilterMode();
			var days = _expiringDays;

			SearchRequested?.Invoke(this, new DocumentSearchRequestedEventArgs(mode, days));
		}


		// ==============================
		//   HANDLER SELEZIONE GRIGLIA
		// ==============================

		private void DocumentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedItem = DocumentsGrid.SelectedItem as DocumentRow;
			SelectionChanged?.Invoke(this, EventArgs.Empty);
		}

	}
}
