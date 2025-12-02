using Microsoft.EntityFrameworkCore;
using DocWatcher.Core.Models;

namespace DocWatcher.Core.Data;

public class DocWatcherContext : DbContext
{
    public DbSet<Document> Documents => Set<Document>();

    public DocWatcherContext()
    {
    }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var dbFolder = Path.Combine(appData, "DocWatcher", "Data");
			Directory.CreateDirectory(dbFolder);

#if DEBUG
			var dbName = "docwatcher-dev.db";
#else
        var dbName = "docwatcher.db";
#endif

			var dbPath = Path.Combine(dbFolder, dbName);
			optionsBuilder.UseSqlite($"Data Source={dbPath}");
		}
	}


}
