using System.Threading.Tasks;

namespace AElf.CrossChainServer.CrossChain;

public interface ICrossChainTransferProvider
{
    CrossChainType CrossChainType { get; }

    Task<CrossChainTransfer> FindTransferAsync(string fromChainId, string toChainId,
        string transferTransactionId, string receiptId);
    Task<int> CalculateCrossChainProgressAsync(CrossChainTransfer transfer);
    Task<string> SendReceiveTransactionAsync(CrossChainTransfer transfer);
}