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

    public Task<PoolLiquidityInfoDto> GetPoolLiquidityInfoAsync(string chainId, string tokenSymbol)
    {
        throw new System.NotImplementedException();
    }

    public Task<UserLiquidityInfoDto> GetUserLiquidityInfoAsync(string chainId, string tokenSymbol, string provider)
    {
        throw new System.NotImplementedException();
    }
}