using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.BridgeContract;

namespace AElf.CrossChainServer.Contracts;

public interface IBridgeContractAppService
{
    Task<List<ReceiptInfoDto>> GetTransferReceiptInfosAsync(string chainId, string targetChainId, Guid tokenId,
        long fromIndex, long endIndex);
    Task<List<ReceivedReceiptInfoDto>> GetReceivedReceiptInfosAsync(string chainId, string targetChainId, Guid tokenId,
        long fromIndex, long endIndex);
    Task<BridgeContractSyncInfoDto> GetSyncInfoAsync(string chainId, TransferType type, string targetChainId, Guid tokenId);
    Task UpdateSyncInfoAsync(string chainId, TransferType type, string targetChainId, Guid tokenId, long syncIndex);
    Task<List<ReceiptIndexDto>> GetTransferReceiptIndexAsync(string chainId, List<Guid> tokenIds, List<string> targetChainIds);
    Task<List<ReceiptIndexDto>> GetReceiveReceiptIndexAsync(string chainId, List<Guid> tokenIds, List<string> targetChainIds);
    Task<bool> CheckTransmitAsync(string chainId, string receiptHash);
    Task<string> GetSwapIdByTokenAsync(string chainId, string fromChainId, string symbol);
    Task<string> SwapTokenAsync(string chainId, string swapId, string receiptId, string originAmount,
        string receiverAddress);
}