using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.BridgeContract;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.CrossChain;
using TransferType = AElf.CrossChainServer.BridgeContract.TransferType;

namespace AElf.CrossChainServer.Worker;

public class BridgeContractReceiveSyncProvider :BridgeContractSyncProviderBase
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;

    public BridgeContractReceiveSyncProvider(
        ICrossChainTransferAppService crossChainTransferAppService)
    {
        _crossChainTransferAppService = crossChainTransferAppService;
    }

    public override TransferType Type { get; } = TransferType.Receive;

    protected override async Task<List<ReceiptIndexDto>> GetReceiveReceiptIndexAsync(string chainId, List<Guid> tokenIds, List<string> targetChainIds)
    {
        return await BridgeContractAppService.GetReceiveReceiptIndexAsync(chainId, tokenIds, targetChainIds);
    }

    protected override async Task<HandleReceiptResult> HandleReceiptAsync(string chainId, string targetChainId, Guid tokenId, long fromIndex, long endIndex)
    {
        var result = new HandleReceiptResult();

        var receipts =
            await BridgeContractAppService.GetReceivedReceiptInfosAsync(chainId, targetChainId, tokenId, fromIndex,
                endIndex);
        if (receipts.Count != 0)
        {
            var lib = await GetConfirmedHeightAsync(chainId);
            var count = 0;
            foreach (var receipt in receipts)
            {
                if (receipt.BlockHeight >= lib)
                {
                    break;
                }
                
                await _crossChainTransferAppService.ReceiveAsync(new CrossChainReceiveInput
                {
                    FromAddress = receipt.FromAddress,
                    ReceiptId = receipt.ReceiptId,
                    ToAddress = receipt.ToAddress,
                    FromChainId = receipt.FromChainId,
                    ToChainId = chainId,
                    ReceiveAmount = receipt.Amount,
                    ReceiveTime = receipt.BlockTime,
                    ReceiveTokenId = receipt.TokenId
                });
                count++;
            }

            result.Count = count;
        }

        return result;
    }
}