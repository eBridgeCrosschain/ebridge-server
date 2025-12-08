using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AElf.CrossChainServer.Data;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.EntityFrameworkCore;

public class EntityFrameworkCoreCrossChainServerDbSchemaMigrator
    : ICrossChainServerDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreCrossChainServerDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the CrossChainServerDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<CrossChainServerDbContext>()
            .Database
            .MigrateAsync();
    }
}