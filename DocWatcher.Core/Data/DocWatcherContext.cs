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
			//Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)

			var dbPath = Path.Combine(AppContext.BaseDirectory, "docwatcher.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

}
