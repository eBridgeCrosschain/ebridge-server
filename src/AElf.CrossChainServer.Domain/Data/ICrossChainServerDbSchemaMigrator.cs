using System.Threading.Tasks;

namespace AElf.CrossChainServer.Data;

public interface ICrossChainServerDbSchemaMigrator
{
    Task MigrateAsync();
}
