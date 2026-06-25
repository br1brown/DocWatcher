using Microsoft.EntityFrameworkCore;
using DocWatcher.Core.Models;

namespace DocWatcher.Core.Data;

public class DocWatcherContext : DbContext
{
	public DbSet<Document> Documents => Set<Document>();

	// Costruttore usato dal design-time / fallback (OnConfiguring).
	public DocWatcherContext()
	{
	}

	// Costruttore usato dalla Dependency Injection / IDbContextFactory.
	public DocWatcherContext(DbContextOptions<DocWatcherContext> options)
		: base(options)
	{
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		// Configura solo se non gia' configurato tramite DI (AddDbContextFactory).
		if (!optionsBuilder.IsConfigured)
		{
			optionsBuilder.UseSqlite($"Data Source={DefaultDatabasePath()}");
		}
	}

	/// <summary>
	/// Percorso di default del database in %LOCALAPPDATA%\DocWatcher\Data.
	/// Crea la cartella se non esiste.
	/// </summary>
	public static string DefaultDatabasePath()
	{
		var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		var dbFolder = Path.Combine(appData, "DocWatcher", "Data");
		Directory.CreateDirectory(dbFolder);

#if DEBUG
		const string dbName = "docwatcher-dev.db";
#else
		const string dbName = "docwatcher.db";
#endif

		return Path.Combine(dbFolder, dbName);
	}
}
