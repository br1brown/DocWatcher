using DocWatcher.Core.Csv;
using Xunit;

namespace DocWatcher.Core.Tests;

public class CsvDocumentParserTests
{
	/// <summary>Scrive il contenuto in un file temporaneo, lo passa all'azione e lo rimuove.</summary>
	private static void WithCsv(string content, Action<string> action)
	{
		var path = Path.Combine(Path.GetTempPath(), $"docwatcher-test-{Guid.NewGuid():N}.csv");
		File.WriteAllText(path, content);
		try
		{
			action(path);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void LoadPreview_ReadsHeadersAndRows_SemicolonSeparator()
	{
		const string csv = "Titolo;DataScadenza;Percorso\nPassaporto;31/12/2026;C:\\doc.pdf\n";

		WithCsv(csv, path =>
		{
			var (headers, rows) = CsvDocumentParser.LoadPreview(path);

			Assert.Equal(new[] { "Titolo", "DataScadenza", "Percorso" }, headers.ToArray());
			Assert.Single(rows);
			Assert.Equal("Passaporto", rows[0][0]);
			Assert.Equal("31/12/2026", rows[0][1]);
		});
	}

	[Fact]
	public void LoadPreview_DetectsCommaSeparator()
	{
		const string csv = "Titolo,DataScadenza\nCarta,01/01/2027\n";

		WithCsv(csv, path =>
		{
			var (headers, rows) = CsvDocumentParser.LoadPreview(path);

			Assert.Equal(2, headers.Count);
			Assert.Equal("Carta", rows[0][0]);
		});
	}

	[Fact]
	public void MapFile_ParsesQuotedFieldWithEmbeddedSeparator()
	{
		const string csv = "Titolo;DataScadenza\n\"Contratto; rev. 2\";15/06/2026\n";

		WithCsv(csv, path =>
		{
			var docs = CsvDocumentParser.MapFileToDocuments(path, idxTitle: 0, idxDueDate: 1, idxPath: null);

			Assert.Single(docs);
			Assert.Equal("Contratto; rev. 2", docs[0].Titolo);
		});
	}

	[Fact]
	public void MapFile_ParsesQuotedMultilineField_AsSingleRecord()
	{
		// Un campo quotato che contiene un a-capo: il vecchio parser riga-per-riga
		// lo avrebbe spezzato; CsvHelper lo tiene come singolo record.
		const string csv = "Titolo;DataScadenza\n\"Riga 1\nRiga 2\";20/06/2026\n";

		WithCsv(csv, path =>
		{
			var docs = CsvDocumentParser.MapFileToDocuments(path, idxTitle: 0, idxDueDate: 1, idxPath: null);

			Assert.Single(docs);
			Assert.Equal("Riga 1\nRiga 2", docs[0].Titolo);
		});
	}

	[Fact]
	public void MapFile_SkipsRowsWithInvalidOrMissingDate()
	{
		const string csv =
			"Titolo;DataScadenza\n" +
			"Valido;31/12/2026\n" +
			"DataAssurda;non-una-data\n" +
			"SenzaData;\n";

		WithCsv(csv, path =>
		{
			var docs = CsvDocumentParser.MapFileToDocuments(path, idxTitle: 0, idxDueDate: 1, idxPath: null);

			Assert.Single(docs);
			Assert.Equal("Valido", docs[0].Titolo);
		});
	}

	[Fact]
	public void MapFile_MapsOptionalAttachmentPath()
	{
		const string csv = "Titolo;DataScadenza;Percorso\nDoc;01/01/2027;C:\\a.pdf\nDoc2;02/01/2027;\n";

		WithCsv(csv, path =>
		{
			var docs = CsvDocumentParser.MapFileToDocuments(path, idxTitle: 0, idxDueDate: 1, idxPath: 2);

			Assert.Equal(2, docs.Count);
			Assert.Equal("C:\\a.pdf", docs[0].PercorsoAllegato);
			Assert.Null(docs[1].PercorsoAllegato);
		});
	}

	[Theory]
	[InlineData("31/12/2026", 2026, 12, 31)]
	[InlineData("1/2/2026", 2026, 2, 1)]
	[InlineData("2026-03-15", 2026, 3, 15)]
	[InlineData("01-04-2026", 2026, 4, 1)]
	public void TryParseDate_AcceptsSupportedFormats(string input, int year, int month, int day)
	{
		Assert.True(CsvDocumentParser.TryParseDate(input, out var date));
		Assert.Equal(new DateTime(year, month, day), date.Date);
	}

	[Fact]
	public void TryParseDate_RejectsGarbage()
	{
		Assert.False(CsvDocumentParser.TryParseDate("non-una-data", out _));
	}

	[Theory]
	[InlineData("semplice", ';', "semplice")]
	[InlineData("con;separatore", ';', "\"con;separatore\"")]
	[InlineData("con\"virgolette", ';', "\"con\"\"virgolette\"")]
	public void EscapeCsv_QuotesWhenNeeded(string input, char separator, string expected)
	{
		Assert.Equal(expected, CsvDocumentParser.EscapeCsv(input, separator));
	}
}
