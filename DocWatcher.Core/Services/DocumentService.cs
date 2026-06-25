using DocWatcher.Core.Data;
using DocWatcher.Core.Models;
using Microsoft.EntityFrameworkCore;

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

public class DocumentService
{
	private readonly IDbContextFactory<DocWatcherContext> _contextFactory;

	public DocumentService(IDbContextFactory<DocWatcherContext> contextFactory)
	{
		_contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
	}

	/// <summary>
	/// Restituisce tutti i documenti ordinati per data di scadenza.
	/// </summary>
	public async Task<List<Document>> GetAllDocumentsAsync()
	{
		await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
		return await context.Documents
			.OrdinatiPerScadenza()
			.ToListAsync()
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Restituisce i documenti che scadono entro "days" giorni da partenza (incluso).
	/// </summary>
	public async Task<List<Document>> GetDocumentiInScadenzaAsync(int days, DateTime partenza)
	{
		await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
		return await context.Documents
			.InScadenzaEntro(days, partenza)
			.OrdinatiPerScadenza()
			.ToListAsync()
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Restituisce il numero di documenti che scadono entro "days" giorni da partenza (incluso).
	/// </summary>
	public async Task<int> GetNumInScadenzaAsync(int days, DateTime partenza)
	{
		await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
		return await context.Documents
			.InScadenzaEntro(days, partenza)
			.CountAsync()
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Restituisce i documenti già scaduti (data &lt; oggi).
	/// </summary>
	public async Task<List<Document>> GetDocumentiScadutiAsync()
	{
		await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
		return await context.Documents
			.ScadutiPrimaDi(DateTime.Today)
			.OrdinatiPerScadenza()
			.ToListAsync()
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Restituisce un documento per Id, oppure null se non esiste.
	/// </summary>
	public async Task<Document?> GetByIdAsync(int id)
	{
		await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
		return await context.Documents
			.FirstOrDefaultAsync(d => d.Id == id)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Inserisce un nuovo documento.
	/// </summary>
	public async Task InsertAsync(Document document)
	{
		if (document == null) throw new ArgumentNullException(nameof(document));

		await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
		context.Documents.Add(document);
		await context.SaveChangesAsync().ConfigureAwait(false);
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

		await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
		context.Documents.AddRange(list);
		await context.SaveChangesAsync().ConfigureAwait(false);

		return list.Count;
	}

	/// <summary>
	/// Aggiorna un documento esistente.
	/// </summary>
	public async Task UpdateAsync(Document document)
	{
		if (document == null) throw new ArgumentNullException(nameof(document));

		await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
		context.Documents.Update(document);
		await context.SaveChangesAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Elimina un documento per Id (se esiste).
	/// </summary>
	public async Task DeleteAsync(int id)
	{
		await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
		var doc = await context.Documents.FindAsync(id).ConfigureAwait(false);
		if (doc is null)
			return;

		context.Documents.Remove(doc);
		await context.SaveChangesAsync().ConfigureAwait(false);
	}
}
