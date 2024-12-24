using System.Threading.Tasks;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public interface IEvmSyncProvider
{
    Task ExecuteAsync(string chainId);
}
