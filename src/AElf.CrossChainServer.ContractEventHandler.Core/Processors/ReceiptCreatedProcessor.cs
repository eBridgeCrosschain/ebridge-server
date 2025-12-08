using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AElf.Contracts.Bridge;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class ReceiptCreatedProcessor : AElfEventProcessorBase<ReceiptCreated>
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly IChainAppService _chainAppService;

    public ReceiptCreatedProcessor(ITokenAppService tokenAppService,
        ICrossChainTransferAppService crossChainTransferAppService, IChainAppService chainAppService)
    {
        _tokenAppService = tokenAppService;
        _crossChainTransferAppService = crossChainTransferAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task HandleEventAsync(ReceiptCreated eventDetailsEto, EventContext txInfoDto)
    {
        var chain = await _chainAppService.GetByAElfChainIdAsync(txInfoDto.ChainId);
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chain.Id,
            Symbol = eventDetailsEto.Symbol
        });

        
        await _crossChainTransferAppService.TransferAsync(new CrossChainTransferInput
        {
            TransferAmount = eventDetailsEto.Amount / (decimal)Math.Pow(10, token.Decimals),
            FromAddress = eventDetailsEto.Owner.ToBase58(),
            ToAddress = eventDetailsEto.TargetAddress,
            TransferTokenId = token.Id,
            FromChainId = chain.Id,
            ToChainId = eventDetailsEto.TargetChainId,
            TransferBlockHeight = txInfoDto.BlockNumber,
            TransferTime = txInfoDto.BlockTime,
            TransferTransactionId = txInfoDto.TransactionId,
            ReceiptId = eventDetailsEto.ReceiptId
        });
    }
}