using DocWatcher.Wpf.DTO;
using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
		public DocumentGridPanel()
		{
			InitializeComponent();

			// Gestione incolla per il filtro giorni
			DataObject.AddPastingHandler(TxtDaysFilter, OnDaysFilterPaste);

			// Imposta la visibilità iniziale del filtro giorni
			// (dato che SelectedIndex="0" è "Documenti in scadenza")
			DaysFilterPanel.Visibility = Visibility.Visible;
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

		/// <summary>
		/// Valore di default per il filtro giorni (es. preso da AppConfig).
		/// </summary>
		public int DefaultDaysFilter
		{
			get => (int)GetValue(DefaultDaysFilterProperty);
			set => SetValue(DefaultDaysFilterProperty, value);
		}

		public static readonly DependencyProperty DefaultDaysFilterProperty =
			DependencyProperty.Register(
				nameof(DefaultDaysFilter),
				typeof(int),
				typeof(DocumentGridPanel),
				new PropertyMetadata(30, OnDefaultDaysFilterChanged));

		private static void OnDefaultDaysFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is DocumentGridPanel panel && panel.TxtDaysFilter != null)
			{
				var def = (int)e.NewValue;
				def = Math.Clamp(def, 1, 600);
				panel.TxtDaysFilter.Text = def.ToString();
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
		/// La MainWindow può leggere SelectedItem.
		/// </summary>
		public event EventHandler? SelectionChanged;

		// ==============================
		//   LOGICA FILTRO GIORNI
		// ==============================

		// Solo numeri in input
		private void TxtDaysFilter_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !e.Text.All(char.IsDigit);
		}

		// Gestione incolla
		private void OnDaysFilterPaste(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(DataFormats.Text))
			{
				var text = (string)e.DataObject.GetData(DataFormats.Text);
				if (!text.All(char.IsDigit))
					e.CancelCommand();
			}
			else
			{
				e.CancelCommand();
			}
		}

		private int GetDaysFilter()
		{
			var def = Math.Clamp(DefaultDaysFilter <= 0 ? 30 : DefaultDaysFilter, 1, 600);

			if (int.TryParse(TxtDaysFilter.Text, out var days))
			{
				if (days < 1) days = 1;
				if (days > 600) days = 600;

				TxtDaysFilter.Text = days.ToString();
				return days;
			}

			TxtDaysFilter.Text = def.ToString();
			return def;
		}

		// ==============================
		//   LOGICA FILTRO TIPO
		// ==============================

		private DocumentGridFilterMode GetCurrentFilterMode()
		{
			if (CmbFilterType.SelectedItem is ComboBoxItem item)
			{
				var tag = item.Tag?.ToString() ?? "all";

				return tag.ToLower() switch
				{
					"expiring" => DocumentGridFilterMode.Expiring,
					"expired" => DocumentGridFilterMode.Expired,
					"all" => DocumentGridFilterMode.All,
					_ => DocumentGridFilterMode.All
				};
			}

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

		private void CmbFilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!IsLoaded)
				return;

			if (CmbFilterType.SelectedItem is ComboBoxItem item)
			{
				var tag = item.Tag?.ToString() ?? "all";

				if (DaysFilterPanel != null)
				{
					// Mostra il filtro giorni solo per "Documenti in scadenza"
					DaysFilterPanel.Visibility = tag.ToLower() == "expiring"
						? Visibility.Visible
						: Visibility.Collapsed;
				}
			}

			RaiseSearchRequested();
		}

		private void RaiseSearchRequested()
		{
			var mode = GetCurrentFilterMode();
			var days = GetDaysFilter();

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
