using DocWatcher.Core.Csv;
using DocWatcher.Core.Dtos;

namespace DocWatcher.Wpf.Helpers;

/// <summary>
/// Wrapper sottile sul parser CSV condiviso (<see cref="CsvDocumentParser"/> in DocWatcher.Core).
/// Mantenuto per compatibilita' con le View: la logica di parsing vive nel Core ed e' coperta da test.
/// </summary>
public static class CsvImporter
{
	public static (List<string> Headers, List<string[]> Rows) LoadPreview(string path, int maxRows = 50)
		=> CsvDocumentParser.LoadPreview(path, maxRows);

	public static List<DocumentDto> MapToDocuments(
		List<string[]> rows,
		int idxTitle,
		int idxDueDate,
		int? idxPath)
		=> CsvDocumentParser.MapRowsToDocuments(rows, idxTitle, idxDueDate, idxPath);

	public static Task<List<DocumentDto>> MapFileToDocumentsAsync(
		string path,
		int idxTitle,
		int idxDueDate,
		int? idxPath)
		=> CsvDocumentParser.MapFileToDocumentsAsync(path, idxTitle, idxDueDate, idxPath);

	public static string EscapeCsv(string? value, char separator)
		=> CsvDocumentParser.EscapeCsv(value, separator);
}
