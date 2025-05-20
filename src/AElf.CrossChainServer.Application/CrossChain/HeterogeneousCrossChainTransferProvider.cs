using System;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.ExceptionHandler;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler;
using Nethereum.Util;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;

namespace AElf.CrossChainServer.CrossChain;

public class HeterogeneousCrossChainTransferProvider : ICrossChainTransferProvider, ITransientDependency
{
    private readonly IChainAppService _chainAppService;
    private readonly ICrossChainTransferRepository _crossChainTransferRepository;
    private readonly IAetherLinkProvider _aetherLinkProvider;

    public HeterogeneousCrossChainTransferProvider(IChainAppService chainAppService,
        ICrossChainTransferRepository crossChainTransferRepository,
        IAetherLinkProvider aetherLinkProvider)
    {
        _chainAppService = chainAppService;
        _crossChainTransferRepository = crossChainTransferRepository;
        _aetherLinkProvider = aetherLinkProvider;
    }

    public CrossChainType CrossChainType { get; } = CrossChainType.Heterogeneous;

    [UnitOfWork]
    public async Task<CrossChainTransfer> FindTransferAsync(string fromChainId, string toChainId,
        string transferTransactionId, string receiptId)
    {
        Log.Debug("Find transfer from chain {fromChainId} to chain {toChainId} with receipt id {receiptId}.",
            fromChainId, toChainId, receiptId);
        return await _crossChainTransferRepository.FindAsync(o =>
            o.FromChainId == fromChainId && o.ToChainId == toChainId && o.ReceiptId == receiptId);
    }

    public async Task<int> CalculateCrossChainProgressAsync(CrossChainTransfer transfer)
    {
        var chain = await _chainAppService.GetAsync(transfer.ToChainId);
        var fromChain = await _chainAppService.GetAsync(transfer.FromChainId);
        if (chain == null || fromChain == null)
        {
            return 0;
        }

        Log.Debug("Calculate cross chain progress {txId}", transfer.TransferTransactionId);
        return await _aetherLinkProvider.CalculateCrossChainProgressAsync(new AetherLinkCrossChainStatusInput
        {
            TransactionId = transfer.TransferTransactionId
        });
    }

    public Task<string> SendReceiveTransactionAsync(CrossChainTransfer transfer)
    {
        throw new NotImplementedException();
    }
}