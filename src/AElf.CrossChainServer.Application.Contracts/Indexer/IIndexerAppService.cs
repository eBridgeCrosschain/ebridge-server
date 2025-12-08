using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.TokenPool;

namespace AElf.CrossChainServer.Indexer;

public interface IIndexerAppService
{
    Task<long> GetLatestIndexHeightAsync(string chainId);
    Task<long> GetLatestIndexBestHeightAsync(string chainId);

    Task<(bool, CrossChainTransferInfoDto)> GetPendingTransactionAsync(string chainId,string transferTransactionId);
    Task<(bool, CrossChainTransferInfoDto)> GetPendingReceiveTransactionAsync(string chainId,string transferTransactionId);

    Task<(bool, CrossChainTransferInfoDto)> GetPendingReceiptAsync(string chainId, string receiptId);
}