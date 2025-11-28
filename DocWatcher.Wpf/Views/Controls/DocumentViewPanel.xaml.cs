using System;
using System.Windows;
using System.Windows.Controls;

namespace DocWatcher.Wpf.Views
{
	/// <summary>
	/// Logica di interazione per DocumentViewPanel.xaml
	/// </summary>

	public partial class DocumentViewPanel : UserControl
	{
		public DocumentViewPanel()
		{
			InitializeComponent();
		}

		// ==============================
		//   DEPENDENCY PROPERTIES
		// ==============================

		public string? Title
		{
			get => (string?)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

		public static readonly DependencyProperty TitleProperty =
			DependencyProperty.Register(nameof(Title), typeof(string),
				typeof(DocumentViewPanel), new PropertyMetadata(string.Empty));

		public DateTime DueDate
		{
			get => (DateTime)GetValue(DueDateProperty);
			set => SetValue(DueDateProperty, value);
		}

		public static readonly DependencyProperty DueDateProperty =
			DependencyProperty.Register(nameof(DueDate), typeof(DateTime),
				typeof(DocumentViewPanel), new PropertyMetadata(DateTime.Today));

		public string? FilePath
		{
			get => (string?)GetValue(FilePathProperty);
			set => SetValue(FilePathProperty, value);
		}

		public static readonly DependencyProperty FilePathProperty =
			DependencyProperty.Register(nameof(FilePath), typeof(string),
				typeof(DocumentViewPanel), new PropertyMetadata(string.Empty));

		public string? Status
		{
			get => (string?)GetValue(StatusProperty);
			set => SetValue(StatusProperty, value);
		}

		public static readonly DependencyProperty StatusProperty =
			DependencyProperty.Register(nameof(Status), typeof(string),
				typeof(DocumentViewPanel), new PropertyMetadata(string.Empty));

		// ==============================
		//   EVENTI RICHIESTI DALL'ESTERNO
		// ==============================

		public event EventHandler? OpenFileRequested;
		public event EventHandler? EditRequested;
		public event EventHandler? DeleteRequested;

		private void OpenFile_Click(object sender, RoutedEventArgs e)
		{
			OpenFileRequested?.Invoke(this, EventArgs.Empty);
		}

		private void Edit_Click(object sender, RoutedEventArgs e)
		{
			EditRequested?.Invoke(this, EventArgs.Empty);
		}

		private void Delete_Click(object sender, RoutedEventArgs e)
		{
			DeleteRequested?.Invoke(this, EventArgs.Empty);
		}
	}
}
