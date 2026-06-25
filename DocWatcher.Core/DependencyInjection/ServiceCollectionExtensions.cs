using DocWatcher.Core.Data;
using DocWatcher.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocWatcher.Core;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registra i servizi di DocWatcher.Core nel container di DI:
	/// un <see cref="IDbContextFactory{TContext}"/> per creare context brevi
	/// (uno per operazione), piu' il service e il controller dei documenti.
	/// </summary>
	/// <param name="services">Il service collection.</param>
	/// <param name="databasePath">
	/// Percorso del file SQLite. Se null usa <see cref="DocWatcherContext.DefaultDatabasePath"/>.
	/// </param>
	public static IServiceCollection AddDocWatcherCore(
		this IServiceCollection services,
		string? databasePath = null)
	{
		var dbPath = databasePath ?? DocWatcherContext.DefaultDatabasePath();

		services.AddDbContextFactory<DocWatcherContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));

		services.AddSingleton<DocumentService>();
		services.AddSingleton<DocumentController>();

		return services;
	}
}
