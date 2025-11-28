using DocWatcher.Core.Data;
using DocWatcher.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocWatcher.Core.Services;

internal static class DocumentQueryExtensions
{
	/// <summary>
	/// Filtra i documenti scaduti PRIMA della data di riferimento (esclusa la data stessa)
	/// </summary>
	public static IQueryable<Document> ScadutiPrimaDi(
		this IQueryable<Document> query,
		DateTime dataRiferimento)
	{
		var refDate = dataRiferimento.Date;
		return query.Where(d => d.DataScadenza < refDate);
	}

	/// <summary>
	/// Filtra i documenti che scadono ENTRO n giorni a partire dalla data di riferimento
	/// </summary>
	/// <param name="nGiorni">Numero di giorni (deve essere >= 0)</param>
	/// <param name="dataRiferimento">Data di partenza (default = oggi)</param>
	/// <param name="includiDataPartenza">Se true include la data di partenza (default = true)</param>
	public static IQueryable<Document> InScadenzaEntro(
		this IQueryable<Document> query,
		int nGiorni,
		DateTime dataRiferimento)
	{
		if (nGiorni < 0) throw new ArgumentOutOfRangeException(nameof(nGiorni));

		var startDate = dataRiferimento.Date;

		var endDate = dataRiferimento.Date.AddDays(nGiorni);

		return query.Where(d => d.DataScadenza >= startDate && d.DataScadenza <= endDate);
	}

	/// <summary>
	/// Ordina sempre per data scadenza (ascendente) – utile per comporre
	/// </summary>
	public static IOrderedQueryable<Document> OrdinatiPerScadenza(
		this IQueryable<Document> query)
	{
		return query.OrderBy(d => d.DataScadenza);
	}
}

internal class DocumentService
{
	private readonly DocWatcherContext _context;

	public DocumentService(DocWatcherContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	/// <summary>
	/// Query di base componibile su Documents.
	/// Tutte le query partono da qui.
	/// </summary>
	private IQueryable<Document> BaseQuery => _context.Documents.AsQueryable();

	/// <summary>
	/// Restituisce tutti i documenti ordinati per data di scadenza.
	/// </summary>
	public Task<List<Document>> GetAllDocumentsAsync()
		=> BaseQuery
			.OrdinatiPerScadenza()
			.ToListAsync();

	/// <summary>
	/// Restituisce i documenti che scadono entro "days" giorni da partenza (incluso).
	/// </summary>
	public Task<List<Document>> GetDocumentiInScadenzaAsync(int days, DateTime partenza)
	=> BaseQuery
			.InScadenzaEntro(days, partenza)
			.OrdinatiPerScadenza()
			.ToListAsync();
	/// <summary>
	/// Restituisce il numero di documenti che scadono entro "days" giorni da partenza (incluso).
	/// </summary>
	public Task<int> GetNumInScadenzaAsync(int days, DateTime partenza)
	=> BaseQuery
			.InScadenzaEntro(days, partenza).CountAsync();


	/// <summary>
	/// Restituisce i documenti già scaduti (data &lt; oggi).
	/// </summary>
	public Task<List<Document>> GetDocumentiScadutiAsync()
	=> BaseQuery
			.ScadutiPrimaDi(DateTime.Today)
			.OrdinatiPerScadenza()
			.ToListAsync();

	/// <summary>
	/// Restituisce un documento per Id, oppure null se non esiste.
	/// </summary>
	public Task<Document?> GetByIdAsync(int id)
		=> BaseQuery.FirstOrDefaultAsync(d => d.Id == id);

	/// <summary>
	/// Inserisce un nuovo documento.
	/// </summary>
	public async Task InsertAsync(Document document)
	{
		if (document == null) throw new ArgumentNullException(nameof(document));

		_context.Documents.Add(document);
		await _context.SaveChangesAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Inserimento massivo di documenti (es. import da CSV).
	/// </summary>
	public async Task<int> BulkInsertAsync(IEnumerable<Document> documents)
	{
		if (documents == null) throw new ArgumentNullException(nameof(documents));

		var list = documents.ToList();
		if (list.Count == 0)
			return 0;

		_context.Documents.AddRange(list);
		await _context.SaveChangesAsync().ConfigureAwait(false);

		return list.Count;
	}

	/// <summary>
	/// Aggiorna un documento esistente.
	/// </summary>
	public async Task UpdateAsync(Document document)
	{
		if (document == null) throw new ArgumentNullException(nameof(document));

		_context.Documents.Update(document);
		await _context.SaveChangesAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Elimina un documento per Id (se esiste).
	/// </summary>
	public async Task DeleteAsync(int id)
	{
		var doc = await _context.Documents.FindAsync(id).ConfigureAwait(false);
		if (doc is null)
			return;

		_context.Documents.Remove(doc);
		await _context.SaveChangesAsync().ConfigureAwait(false);
	}
}
