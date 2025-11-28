using DocWatcher.Core.Dtos;
using System;

namespace DocWatcher.Wpf.DTO;

public class DocumentRow
{
	public DocumentDto Document { get; }

	public int Id => Document.Id ?? -1;
	public string Titolo => Document.Titolo;
	public DateTime DataScadenza => Document.DataScadenza;
	public string? PercorsoAllegato => Document.PercorsoAllegato;

	/// <summary>
	/// Giorni che mancano alla scadenza (negativi se già scaduto).
	/// </summary>
	internal int GiorniAllaScadenza => (Document.DataScadenza.Date - DateTime.Today).Days;

	/// <summary>
	/// True se esiste un percorso allegato non vuoto.
	/// </summary>
	internal bool HaAllegato => !string.IsNullOrWhiteSpace(PercorsoAllegato);

	/// <summary>
	/// Testo descrittivo dello stato (Scade tra X giorni / Scade oggi / Scaduto da X giorni).
	/// </summary>
	internal string StatoTesto
	{
		get
		{
			var diff = GiorniAllaScadenza;

			if (diff > 0)
				return $"Scade tra {diff} giorn{(diff == 1 ? "o" : "i")}";

			if (diff == 0)
				return "Scade oggi";

			var giorniPassati = -diff;
			return $"Scaduto da {giorniPassati} giorn{(giorniPassati == 1 ? "o" : "i")}";
		}
	}

	public DocumentRow(DocumentDto document)
	{
		Document = document ?? throw new ArgumentNullException(nameof(document));
	}
}
