using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AElf.Contracts.MultiToken;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class CrossChainReceivedProcessor: AElfEventProcessorBase<CrossChainReceived>
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly IChainAppService _chainAppService;

    public CrossChainReceivedProcessor(ICrossChainTransferAppService crossChainTransferAppService,
        ITokenAppService tokenAppService, IChainAppService chainAppService)
    {
        _crossChainTransferAppService = crossChainTransferAppService;
        _tokenAppService = tokenAppService;
        _chainAppService = chainAppService;
    }

    protected override async Task HandleEventAsync(CrossChainReceived eventDetailsEto, EventContext txInfoDto)
    {
        var chain = await _chainAppService.GetByAElfChainIdAsync(txInfoDto.ChainId);
        var fromChain = await _chainAppService.GetByAElfChainIdAsync(eventDetailsEto.FromChainId);
        if (fromChain == null)
        {
            return;
        }

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId =chain.Id,
            Symbol = eventDetailsEto.Symbol
        });

        await _crossChainTransferAppService.ReceiveAsync(new CrossChainReceiveInput()
        {
            ReceiveAmount = eventDetailsEto.Amount / (decimal)Math.Pow(10, token.Decimals),
            ReceiveTime = txInfoDto.BlockTime,
            FromChainId = fromChain.Id,
            ReceiveTransactionId = txInfoDto.TransactionId,
            ToChainId = chain.Id,
            TransferTransactionId = eventDetailsEto.TransferTransactionId.ToHex(),
            ReceiveTokenId = token.Id,
            FromAddress = eventDetailsEto.From.ToBase58(),
            ToAddress = eventDetailsEto.To.ToBase58()
        });
    }
}