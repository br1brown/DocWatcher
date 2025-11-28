using DocWatcher.Core.Data;
using DocWatcher.Core.Dtos;
using DocWatcher.Core.Models;
using DocWatcher.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
	{
		var docs = await _documentService.GetAllDocumentsAsync().ConfigureAwait(false);
		return docs.Select(ToDto).ToList();
	}

	public async Task<List<DocumentDto>> GetExpiringAsync(int days)
	{
		var docs = await _documentService.GetDocumentiInScadenzaAsync(days, DateTime.Today).ConfigureAwait(false);
		return docs.Select(ToDto).ToList();
	}
	public async Task<int> GetNumExpiringAsync(int days)
	{
		return await _documentService.GetNumInScadenzaAsync(days, DateTime.Today).ConfigureAwait(false);
	}

	public async Task<List<DocumentDto>> GetExpiredAsync()
	{
		var docs = await _documentService.GetDocumentiScadutiAsync().ConfigureAwait(false);
		return docs.Select(ToDto).ToList();
	}

	public Task<Document?> GetByIdAsync(int id)
		=> _documentService.GetByIdAsync(id);

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

		await _documentService.InsertAsync(entity).ConfigureAwait(false);
		return entity;
	}

	public async Task UpdateAsync(DocumentDto dto)
	{
		if (dto is null) throw new ArgumentNullException(nameof(dto));

		var existing = await _documentService.GetByIdAsync(dto.Id.Value).ConfigureAwait(false);
		if (existing is null)
			throw new KeyNotFoundException($"Documento con Id={dto.Id.Value} non trovato.");

		ApplyToEntity(existing, dto);
		await _documentService.UpdateAsync(existing).ConfigureAwait(false);
	}

	public Task DeleteAsync(int id)
		=> _documentService.DeleteAsync(id);

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

		return await _documentService.BulkInsertAsync(docs).ConfigureAwait(false);
	}
}
