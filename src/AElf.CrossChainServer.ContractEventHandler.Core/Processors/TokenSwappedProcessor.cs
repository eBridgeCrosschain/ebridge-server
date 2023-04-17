using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AElf.Contracts.Bridge;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class TokenSwappedProcessor: AElfEventProcessorBase<TokenSwapped>
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly IChainAppService _chainAppService;

    public TokenSwappedProcessor(ICrossChainTransferAppService crossChainTransferAppService,
        ITokenAppService tokenAppService, IChainAppService chainAppService)
    {
        _crossChainTransferAppService = crossChainTransferAppService;
        _tokenAppService = tokenAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task HandleEventAsync(TokenSwapped eventDetailsEto, EventContext txInfoDto)
    {
        var chain = await _chainAppService.GetByAElfChainIdAsync(txInfoDto.ChainId);

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chain.Id,
            Symbol = eventDetailsEto.Symbol
        });
        
        await _crossChainTransferAppService.ReceiveAsync(new CrossChainReceiveInput()
        {
            ReceiveAmount = eventDetailsEto.Amount / (decimal)Math.Pow(10, token.Decimals),
            ReceiveTime = txInfoDto.BlockTime,
            FromChainId = eventDetailsEto.FromChainId,
            ReceiveTransactionId = txInfoDto.TransactionId,
            ToChainId = chain.Id,
            ReceiveTokenId = token.Id,
            ToAddress = eventDetailsEto.Address.ToBase58(),
            ReceiptId = eventDetailsEto.ReceiptId
        });
    }
}