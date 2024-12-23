using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AElf.CrossChainServer.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class CrossChainServerDbContextFactory : IDesignTimeDbContextFactory<CrossChainServerDbContext>
{
    public CrossChainServerDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        CrossChainServerEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<CrossChainServerDbContext>()
            .UseMySql(configuration.GetConnectionString("Default"), MySqlServerVersion.LatestSupportedServerVersion);

        return new CrossChainServerDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../AElf.CrossChainServer.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}