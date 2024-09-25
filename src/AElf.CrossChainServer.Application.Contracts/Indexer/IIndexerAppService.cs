using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;

namespace AElf.CrossChainServer.Indexer;

public interface IIndexerAppService
{
    Task<long> GetLatestIndexHeightAsync(string chainId);
    Task<CrossChainTransferDto> GetPendingTransactionAsync(string chainId,string transferTransactionId);
}