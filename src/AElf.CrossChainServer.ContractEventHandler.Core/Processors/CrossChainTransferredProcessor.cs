using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AElf.Contracts.MultiToken;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;
using CrossChainTransferInput = AElf.CrossChainServer.CrossChain.CrossChainTransferInput;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class CrossChainTransferredProcessor: AElfEventProcessorBase<CrossChainTransferred>
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly IChainAppService _chainAppService;

    public CrossChainTransferredProcessor(ICrossChainTransferAppService crossChainTransferAppService,
        ITokenAppService tokenAppService, IChainAppService chainAppService)
    {
        _crossChainTransferAppService = crossChainTransferAppService;
        _tokenAppService = tokenAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task HandleEventAsync(CrossChainTransferred eventDetailsEto, EventContext txInfoDto)
    {
        var chain = await _chainAppService.GetByAElfChainIdAsync(txInfoDto.ChainId);
        var toChain = await _chainAppService.GetByAElfChainIdAsync(eventDetailsEto.ToChainId);
        if (toChain == null)
        {
            return;
        }

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId =chain.Id,
            Symbol = eventDetailsEto.Symbol
        });
        
        await _crossChainTransferAppService.TransferAsync(new CrossChainTransferInput
        {
            TransferAmount = eventDetailsEto.Amount / (decimal)Math.Pow(10, token.Decimals),
            FromAddress = eventDetailsEto.From.ToBase58(),
            ToAddress = eventDetailsEto.To.ToBase58(),
            TransferTokenId = token.Id,
            FromChainId = chain.Id,
            ToChainId = toChain.Id,
            TransferBlockHeight = txInfoDto.BlockNumber,
            TransferTime = txInfoDto.BlockTime,
            TransferTransactionId = txInfoDto.TransactionId
        });
    }
}