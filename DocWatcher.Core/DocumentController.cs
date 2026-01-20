using DocWatcher.Core.Data;
using DocWatcher.Core.Dtos;
using DocWatcher.Core.Models;
using DocWatcher.Core.Services;

namespace DocWatcher.Core;

public class DocumentController
{
	private readonly DocumentService _documentService;

	public DocumentController(DocWatcherContext context)
	{
		_documentService = new DocumentService(context);
	}

	private static DocumentDto ToDto(Document d) => new()
	{
		Id = d.Id,
		Titolo = d.Titolo,
		DataScadenza = d.DataScadenza,
		PercorsoAllegato = d.PercorsoAllegato
	};

	private static void ApplyToEntity(Document entity, DocumentDto dto)
	{
		if (dto.Titolo is not null)
			entity.Titolo = dto.Titolo.Trim();

		if (dto.PercorsoAllegato is not null)
			entity.PercorsoAllegato = string.IsNullOrWhiteSpace(dto.PercorsoAllegato)
				? null
				: dto.PercorsoAllegato.Trim();
	}

	// =========================
	//   LETTURA
	// =========================

	public async Task<List<DocumentDto>> GetAllAsync()
		=> await RunLoggedAsync(async () =>
		{
			var docs = await _documentService.GetAllDocumentsAsync().ConfigureAwait(false);
			return docs.Select(ToDto).ToList();
		}, "DocumentController.GetAllAsync").ConfigureAwait(false);

	public async Task<List<DocumentDto>> GetExpiringAsync(int days)
		=> await RunLoggedAsync(async () =>
		{
			var docs = await _documentService.GetDocumentiInScadenzaAsync(days, DateTime.Today).ConfigureAwait(false);
			return docs.Select(ToDto).ToList();
		}, "DocumentController.GetExpiringAsync").ConfigureAwait(false);
	public async Task<int> GetNumExpiringAsync(int days)
		=> await RunLoggedAsync(
			() => _documentService.GetNumInScadenzaAsync(days, DateTime.Today),
			"DocumentController.GetNumExpiringAsync").ConfigureAwait(false);

	public async Task<List<DocumentDto>> GetExpiredAsync()
		=> await RunLoggedAsync(async () =>
		{
			var docs = await _documentService.GetDocumentiScadutiAsync().ConfigureAwait(false);
			return docs.Select(ToDto).ToList();
		}, "DocumentController.GetExpiredAsync").ConfigureAwait(false);

	public Task<Document?> GetByIdAsync(int id)
		=> RunLoggedAsync(() => _documentService.GetByIdAsync(id), "DocumentController.GetByIdAsync");

	// =========================
	//   SCRITTURA
	// =========================

	public async Task<Document> CreateAsync(DocumentDto dto)
	{
		if (dto is null) throw new ArgumentNullException(nameof(dto));
		if (string.IsNullOrWhiteSpace(dto.Titolo))
			throw new ArgumentException("Titolo e DataScadenza sono obbligatori.", nameof(dto));

		var entity = new Document
		{
			Titolo = dto.Titolo!.Trim(),
			DataScadenza = dto.DataScadenza.Date,
			PercorsoAllegato = string.IsNullOrWhiteSpace(dto.PercorsoAllegato)
				? null
				: dto.PercorsoAllegato.Trim()
		};

		await RunLoggedAsync(
			() => _documentService.InsertAsync(entity),
			"DocumentController.CreateAsync").ConfigureAwait(false);
		return entity;
	}

	public async Task UpdateAsync(DocumentDto dto)
	{
		if (dto is null) throw new ArgumentNullException(nameof(dto));

		var existing = await _documentService.GetByIdAsync(dto.Id.Value).ConfigureAwait(false);
		if (existing is null)
			throw new KeyNotFoundException($"Documento con Id={dto.Id.Value} non trovato.");

		ApplyToEntity(existing, dto);
		await RunLoggedAsync(
			() => _documentService.UpdateAsync(existing),
			"DocumentController.UpdateAsync").ConfigureAwait(false);
	}

	public Task DeleteAsync(int id)
		=> RunLoggedAsync(() => _documentService.DeleteAsync(id), "DocumentController.DeleteAsync");

	/// <summary>
	/// Import massivo da DTO (es. CSV in uno scenario API/batch).
	/// </summary>
	public async Task<int> BulkImportAsync(IEnumerable<DocumentDto> dtos)
	{
		if (dtos is null) throw new ArgumentNullException(nameof(dtos));

		var docs = dtos
			.Where(d => !string.IsNullOrWhiteSpace(d.Titolo))
			.Select(d => new Document
			{
				Titolo = d.Titolo!.Trim(),
				DataScadenza = d.DataScadenza!.Date,
				PercorsoAllegato = string.IsNullOrWhiteSpace(d.PercorsoAllegato)
					? null
					: d.PercorsoAllegato.Trim()
			})
			.ToList();

		if (docs.Count == 0)
			return 0;

		return await RunLoggedAsync(
			() => _documentService.BulkInsertAsync(docs),
			"DocumentController.BulkImportAsync").ConfigureAwait(false);
	}

	private static async Task<T> RunLoggedAsync<T>(Func<Task<T>> action, string context)
	{
		try
		{
			return await action().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			LogHelper.Log(ex, context);
			throw;
		}
	}

	private static async Task RunLoggedAsync(Func<Task> action, string context)
	{
		try
		{
			await action().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			LogHelper.Log(ex, context);
			throw;
		}
	}
}
