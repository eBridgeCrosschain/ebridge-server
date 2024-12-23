using System.Threading.Tasks;

namespace AElf.CrossChainServer.Worker.TokenPoolSync;

public interface IEvmSyncProvider
{
    Task ExecuteAsync(string chainId);
}

public class EvmSyncProvider : IEvmSyncProvider
{
    public async Task ExecuteAsync(string chainId)
    {
        // todo
    }
}