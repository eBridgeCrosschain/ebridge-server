using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.TokenPool;

namespace AElf.CrossChainServer;

public class MockIndexerAppService: CrossChainServerAppService, IIndexerAppService
{
    public async Task<long> GetLatestIndexHeightAsync(string chainId)
    {
        return 100;
    }

    public Task<CrossChainTransferInfoDto> GetPendingTransactionAsync(string chainId, string transferTransactionId)
    {
        throw new System.NotImplementedException();
    }
}