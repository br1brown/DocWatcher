using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DocWatcher.Core.Dtos;

namespace DocWatcher.Core.Csv;

/// <summary>
/// Parsing/serializzazione CSV dei documenti, basato su CsvHelper.
/// La mappatura avviene per indice di colonna (scelto dall'utente nella UI di import).
/// Il separatore (<c>;</c> oppure <c>,</c>) viene rilevato dalla riga di intestazione.
/// </summary>
public static class CsvDocumentParser
{
	/// <summary>
	/// Legge intestazioni e prime <paramref name="maxRows"/> righe per l'anteprima.
	/// </summary>
	public static (List<string> Headers, List<string[]> Rows) LoadPreview(string path, int maxRows = 50)
	{
		var separator = DetectSeparator(path);

		using var reader = new StreamReader(path);
		using var csv = new CsvReader(reader, CreateConfig(separator));

		if (!csv.Read() || !TryReadHeader(csv, out var headers))
			throw new InvalidOperationException("File CSV vuoto.");

		var rows = new List<string[]>();
		while (rows.Count < maxRows && csv.Read())
		{
			rows.Add(CurrentRecord(csv));
		}

		return (headers, rows);
	}

	/// <summary>
	/// Mappa l'intero file in DTO (versione asincrona, eseguita su thread di background).
	/// </summary>
	public static Task<List<DocumentDto>> MapFileToDocumentsAsync(
		string path,
		int idxTitle,
		int idxDueDate,
		int? idxPath)
		=> Task.Run(() => MapFileToDocuments(path, idxTitle, idxDueDate, idxPath));

	/// <summary>
	/// Mappa l'intero file in DTO.
	/// </summary>
	public static List<DocumentDto> MapFileToDocuments(
		string path,
		int idxTitle,
		int idxDueDate,
		int? idxPath)
	{
		var separator = DetectSeparator(path);

		using var reader = new StreamReader(path);
		using var csv = new CsvReader(reader, CreateConfig(separator));

		if (!csv.Read() || !TryReadHeader(csv, out _))
			throw new InvalidOperationException("File CSV vuoto.");

		var docs = new List<DocumentDto>();
		while (csv.Read())
		{
			var dto = MapRowToDocument(CurrentRecord(csv), idxTitle, idxDueDate, idxPath);
			if (dto is not null)
				docs.Add(dto);
		}

		return docs;
	}

	/// <summary>
	/// Mappa righe gia' lette (es. dall'anteprima) in DTO.
	/// </summary>
	public static List<DocumentDto> MapRowsToDocuments(
		IEnumerable<string[]> rows,
		int idxTitle,
		int idxDueDate,
		int? idxPath)
	{
		var docs = new List<DocumentDto>();
		foreach (var cols in rows)
		{
			var dto = MapRowToDocument(cols, idxTitle, idxDueDate, idxPath);
			if (dto is not null)
				docs.Add(dto);
		}

		return docs;
	}

	private static DocumentDto? MapRowToDocument(
		string[] cols,
		int idxTitle,
		int idxDueDate,
		int? idxPath)
	{
		if (idxTitle >= cols.Length || idxDueDate >= cols.Length)
			return null;

		var titolo = cols[idxTitle].Trim();
		var dataStr = cols[idxDueDate].Trim();

		if (string.IsNullOrWhiteSpace(titolo) || string.IsNullOrWhiteSpace(dataStr))
			return null;

		if (!TryParseDate(dataStr, out var data))
			return null;

		string? path = null;
		if (idxPath.HasValue && idxPath.Value < cols.Length)
		{
			var rawPath = cols[idxPath.Value].Trim();
			path = string.IsNullOrWhiteSpace(rawPath) ? null : rawPath;
		}

		return new DocumentDto
		{
			Titolo = titolo,
			DataScadenza = data,
			PercorsoAllegato = path,
		};
	}

	/// <summary>
	/// Esegue l'escape di un campo per la scrittura CSV.
	/// </summary>
	public static string EscapeCsv(string? value, char separator)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;

		var needsQuotes = value.Contains(separator)
			|| value.Contains('"')
			|| value.Contains('\n')
			|| value.Contains('\r');

		if (!needsQuotes)
			return value;

		var escaped = value.Replace("\"", "\"\"");
		return $"\"{escaped}\"";
	}

	/// <summary>
	/// Prova a interpretare una data nei formati supportati (it-IT e ISO).
	/// </summary>
	public static bool TryParseDate(string input, out DateTime date)
	{
		var styles = DateTimeStyles.AllowWhiteSpaces;
		var it = CultureInfo.GetCultureInfo("it-IT");
		var formats = new[]
		{
			"d/M/yyyy",
			"dd/MM/yyyy",
			"d-M-yyyy",
			"dd-MM-yyyy",
			"yyyy-MM-dd"
		};

		if (DateTime.TryParseExact(input, formats, it, styles, out date))
			return true;

		if (DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, styles, out date))
			return true;

		return DateTime.TryParse(input, it, styles, out date);
	}

	private static char DetectSeparator(string path)
	{
		foreach (var line in File.ReadLines(path))
		{
			if (string.IsNullOrWhiteSpace(line))
				continue;

			return line.Contains(';') ? ';' : ',';
		}

		throw new InvalidOperationException("File CSV vuoto.");
	}

	private static CsvConfiguration CreateConfig(char separator) =>
		new(CultureInfo.InvariantCulture)
		{
			Delimiter = separator.ToString(),
			HasHeaderRecord = true,
			IgnoreBlankLines = true,
			MissingFieldFound = null,
			BadDataFound = null,
			DetectColumnCountChanges = false,
		};

	private static bool TryReadHeader(CsvReader csv, out List<string> headers)
	{
		csv.ReadHeader();
		headers = (csv.HeaderRecord ?? Array.Empty<string>())
			.Select(h => h.Trim())
			.ToList();
		return headers.Count > 0;
	}

	private static string[] CurrentRecord(CsvReader csv)
		=> csv.Parser.Record ?? Array.Empty<string>();
}
