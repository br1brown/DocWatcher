using DocWatcher.Core.Dtos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DocWatcher.Wpf.Helpers;

public static class CsvImporter
{
    public static (List<string> Headers, List<string[]> Rows) LoadPreview(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
            throw new InvalidOperationException("File CSV vuoto.");

        char separator = lines[0].Contains(';') ? ';' : ',';

        var headers = lines[0].Split(separator).Select(h => h.Trim()).ToList();
        var rows = lines
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(separator))
            .ToList();

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

            if (!DateTime.TryParse(dataStr, out var data))
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
}
