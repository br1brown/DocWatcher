using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DocWatcher.Core.Data;

/// <summary>
/// Factory usata solo dagli strumenti EF Core a design-time
/// (es. `dotnet ef migrations add ...`). Non viene usata a runtime.
/// </summary>
public class DocWatcherContextDesignTimeFactory : IDesignTimeDbContextFactory<DocWatcherContext>
{
	public DocWatcherContext CreateDbContext(string[] args)
	{
		var options = new DbContextOptionsBuilder<DocWatcherContext>()
			.UseSqlite("Data Source=docwatcher-design.db")
			.Options;

		return new DocWatcherContext(options);
	}
}
