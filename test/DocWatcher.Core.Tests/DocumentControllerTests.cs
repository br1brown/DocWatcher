using DocWatcher.Core;
using DocWatcher.Core.Dtos;
using DocWatcher.Core.Services;
using Xunit;

namespace DocWatcher.Core.Tests;

public class DocumentControllerTests
{
	private static DocumentController CreateController(InMemoryContextFactory factory)
		=> new(new DocumentService(factory));

	private static DocumentDto Doc(string titolo, DateTime scadenza, string? path = null) => new()
	{
		Titolo = titolo,
		DataScadenza = scadenza,
		PercorsoAllegato = path,
	};

	[Fact]
	public async Task CreateAsync_PersistsTrimmedDocument()
	{
		using var factory = new InMemoryContextFactory();
		var controller = CreateController(factory);

		var created = await controller.CreateAsync(Doc("  Passaporto  ", DateTime.Today.AddDays(10)));

		Assert.True(created.Id > 0);
		Assert.Equal("Passaporto", created.Titolo);

		var all = await controller.GetAllAsync();
		Assert.Single(all);
	}

	[Fact]
	public async Task GetExpiring_And_Expired_FilterByDate()
	{
		using var factory = new InMemoryContextFactory();
		var controller = CreateController(factory);

		await controller.CreateAsync(Doc("In scadenza", DateTime.Today.AddDays(5)));
		await controller.CreateAsync(Doc("Lontano", DateTime.Today.AddDays(100)));
		await controller.CreateAsync(Doc("Scaduto", DateTime.Today.AddDays(-5)));

		var expiring = await controller.GetExpiringAsync(60);
		Assert.Single(expiring);
		Assert.Equal("In scadenza", expiring[0].Titolo);

		var expiringCount = await controller.GetNumExpiringAsync(60);
		Assert.Equal(1, expiringCount);

		var expired = await controller.GetExpiredAsync();
		Assert.Single(expired);
		Assert.Equal("Scaduto", expired[0].Titolo);

		var all = await controller.GetAllAsync();
		Assert.Equal(3, all.Count);
	}

	[Fact]
	public async Task GetAll_IsOrderedByDueDate()
	{
		using var factory = new InMemoryContextFactory();
		var controller = CreateController(factory);

		await controller.CreateAsync(Doc("C", DateTime.Today.AddDays(30)));
		await controller.CreateAsync(Doc("A", DateTime.Today.AddDays(1)));
		await controller.CreateAsync(Doc("B", DateTime.Today.AddDays(10)));

		var all = await controller.GetAllAsync();

		Assert.Equal(new[] { "A", "B", "C" }, all.Select(d => d.Titolo).ToArray());
	}

	[Fact]
	public async Task UpdateAsync_ChangesTitle()
	{
		using var factory = new InMemoryContextFactory();
		var controller = CreateController(factory);

		var created = await controller.CreateAsync(Doc("Vecchio", DateTime.Today.AddDays(5)));

		await controller.UpdateAsync(new DocumentDto
		{
			Id = created.Id,
			Titolo = "Nuovo",
			DataScadenza = created.DataScadenza,
		});

		var reloaded = await controller.GetByIdAsync(created.Id);
		Assert.NotNull(reloaded);
		Assert.Equal("Nuovo", reloaded!.Titolo);
	}

	[Fact]
	public async Task UpdateAsync_WithoutId_Throws()
	{
		using var factory = new InMemoryContextFactory();
		var controller = CreateController(factory);

		await Assert.ThrowsAsync<ArgumentException>(() =>
			controller.UpdateAsync(Doc("Senza id", DateTime.Today)));
	}

	[Fact]
	public async Task DeleteAsync_RemovesDocument()
	{
		using var factory = new InMemoryContextFactory();
		var controller = CreateController(factory);

		var created = await controller.CreateAsync(Doc("Da eliminare", DateTime.Today.AddDays(5)));
		await controller.DeleteAsync(created.Id);

		var all = await controller.GetAllAsync();
		Assert.Empty(all);
	}

	[Fact]
	public async Task BulkImportAsync_SkipsRowsWithoutTitle()
	{
		using var factory = new InMemoryContextFactory();
		var controller = CreateController(factory);

		var imported = await controller.BulkImportAsync(new[]
		{
			Doc("Valido 1", DateTime.Today.AddDays(5)),
			Doc("   ", DateTime.Today.AddDays(5)),
			Doc("Valido 2", DateTime.Today.AddDays(6)),
		});

		Assert.Equal(2, imported);
		Assert.Equal(2, (await controller.GetAllAsync()).Count);
	}
}
