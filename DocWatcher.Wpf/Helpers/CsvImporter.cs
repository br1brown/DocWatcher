using DocWatcher.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocWatcher.Wpf.Helpers;

public static class CsvImporter
{
    public static (List<string> Headers, List<string[]> Rows) LoadPreview(string path, int maxRows = 50)
    {
        using var reader = new StreamReader(path);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidOperationException("File CSV vuoto.");

        char separator = headerLine.Contains(';') ? ';' : ',';

        var headers = SplitCsvLine(headerLine, separator).Select(h => h.Trim()).ToList();
        var rows = new List<string[]>();

        string? line;
        while (rows.Count < maxRows && (line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            rows.Add(SplitCsvLine(line, separator));
        }

        return (headers, rows);
    }

    public static List<DocumentDto> MapToDocuments(
        List<string[]> rows,
        int idxTitle,
        int idxDueDate,
        int? idxPath)
    {
        var docs = new List<DocumentDto>();

        foreach (var cols in rows)
        {
            if (idxTitle >= cols.Length || idxDueDate >= cols.Length)
                continue;

            var titolo = cols[idxTitle].Trim();
            var dataStr = cols[idxDueDate].Trim();

            if (string.IsNullOrWhiteSpace(titolo) || string.IsNullOrWhiteSpace(dataStr))
                continue;

            if (!TryParseDate(dataStr, out var data))
                continue;

            string? path = null;
            if (idxPath.HasValue && idxPath.Value < cols.Length)
            {
                var rawPath = cols[idxPath.Value].Trim();
                path = string.IsNullOrWhiteSpace(rawPath) ? null : rawPath;
            }

            var dto = new DocumentDto
            {
                Titolo = titolo,
                DataScadenza = data,
                PercorsoAllegato = path,
            };

            docs.Add(dto);
        }

        return docs;
    }

    public static Task<List<DocumentDto>> MapFileToDocumentsAsync(
        string path,
        int idxTitle,
        int idxDueDate,
        int? idxPath)
    {
        return Task.Run(() => MapFileToDocuments(path, idxTitle, idxDueDate, idxPath));
    }

    private static List<DocumentDto> MapFileToDocuments(
        string path,
        int idxTitle,
        int idxDueDate,
        int? idxPath)
    {
        using var reader = new StreamReader(path);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidOperationException("File CSV vuoto.");

        char separator = headerLine.Contains(';') ? ';' : ',';
        var docs = new List<DocumentDto>();

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cols = SplitCsvLine(line, separator);
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

    public static string EscapeCsv(string? value, char separator)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var needsQuotes = value.Contains(separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuotes)
            return value;

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static string[] SplitCsvLine(string line, char separator)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (c == separator && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }

    private static bool TryParseDate(string input, out DateTime date)
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
}
