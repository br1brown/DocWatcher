using DocWatcher.Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DocWatcher.Core.Tests;

/// <summary>
/// <see cref="IDbContextFactory{TContext}"/> per i test, basato su una connessione
/// SQLite in-memory condivisa (lo schema sopravvive finche' la connessione resta aperta).
/// Riproduce il pattern "un context breve per operazione" usato a runtime.
/// </summary>
internal sealed class InMemoryContextFactory : IDbContextFactory<DocWatcherContext>, IDisposable
{
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<DocWatcherContext> _options;

	public InMemoryContextFactory()
	{
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();

		_options = new DbContextOptionsBuilder<DocWatcherContext>()
			.UseSqlite(_connection)
			.Options;

		using var ctx = new DocWatcherContext(_options);
		ctx.Database.EnsureCreated();
	}

	public DocWatcherContext CreateDbContext() => new(_options);

	public void Dispose() => _connection.Dispose();
}
