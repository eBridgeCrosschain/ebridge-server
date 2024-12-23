using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.TokenPool;

namespace AElf.CrossChainServer.Indexer;

public interface IIndexerAppService
{
    Task<long> GetLatestIndexHeightAsync(string chainId);
    Task<CrossChainTransferInfoDto> GetPendingTransactionAsync(string chainId,string transferTransactionId);
}