using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Data;

/* This is used if database provider does't define
 * ICrossChainServerDbSchemaMigrator implementation.
 */
public class NullCrossChainServerDbSchemaMigrator : ICrossChainServerDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
