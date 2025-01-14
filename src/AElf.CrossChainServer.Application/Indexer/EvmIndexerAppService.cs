using System.Threading.Tasks;

namespace AElf.CrossChainServer.Indexer;

public class EvmIndexerAppService : IEvmIndexerAppService
{
    public Task<long> GetCurrentBlockNumberAsync(string chainId)
    {
        throw new System.NotImplementedException();
    }
}