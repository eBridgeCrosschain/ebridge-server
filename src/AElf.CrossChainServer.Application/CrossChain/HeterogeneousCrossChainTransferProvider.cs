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
    private readonly IOracleQueryInfoAppService _oracleQueryInfoAppService;
    private readonly IReportInfoAppService _reportInfoAppService;
    private readonly ITokenRepository _tokenRepository;
    private readonly ITokenSymbolMappingProvider _tokenSymbolMappingProvider;
    private readonly IBridgeContractAppService _bridgeContractAppService;
    private readonly ICrossChainTransferRepository _crossChainTransferRepository;
    private readonly IAetherLinkProvider _aetherLinkProvider;

    public HeterogeneousCrossChainTransferProvider(IChainAppService chainAppService,
        IOracleQueryInfoAppService oracleQueryInfoAppService, IReportInfoAppService reportInfoAppService,
        ITokenRepository tokenRepository, ITokenSymbolMappingProvider tokenSymbolMappingProvider,
        IBridgeContractAppService bridgeContractAppService, ICrossChainTransferRepository crossChainTransferRepository,
        IAetherLinkProvider aetherLinkProvider)
    {
        _chainAppService = chainAppService;
        _oracleQueryInfoAppService = oracleQueryInfoAppService;
        _reportInfoAppService = reportInfoAppService;
        _tokenRepository = tokenRepository;
        _tokenSymbolMappingProvider = tokenSymbolMappingProvider;
        _bridgeContractAppService = bridgeContractAppService;
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
        // other chain -> aelf
        if (chain.Type == BlockchainType.AElf)
        {
            if (fromChain.Type == BlockchainType.Tvm)
            {
                Log.Debug("Calculate cross chain progress from ton to aelf.{traceId}",transfer.TraceId);
                return await _aetherLinkProvider.CalculateCrossChainProgressAsync(new AetherLinkCrossChainStatusInput
                {
                    // SourceChainId = fromChain.AElfChainId,
                    TraceId = transfer.TraceId
                });
            }
            else if(fromChain.Type == BlockchainType.Svm)
            {
                Log.Debug("Calculate cross chain progress from solana to aelf.{txId}",transfer.TransferTransactionId);
                return await _aetherLinkProvider.CalculateCrossChainProgressAsync(new AetherLinkCrossChainStatusInput
                {
                    TransactionId = transfer.TransferTransactionId
                });
            }
            return await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync(transfer.ToChainId,transfer.ReceiptId);
        }
        // aelf -> ton
        if (chain.Type == BlockchainType.Tvm)
        {
            Log.Debug("Calculate cross chain progress from aelf to ton.{txId}",transfer.TransferTransactionId);
            return await _aetherLinkProvider.CalculateCrossChainProgressAsync(new AetherLinkCrossChainStatusInput
            {
                // SourceChainId = fromChain.AElfChainId,
                TransactionId = transfer.TransferTransactionId
            });
        }
        else if (chain.Type == BlockchainType.Svm)
        {
            Log.Debug("Calculate cross chain progress from aelf to solana.{txId}",transfer.ReceiveTransactionId);
            return await _aetherLinkProvider.CalculateCrossChainProgressAsync(new AetherLinkCrossChainStatusInput
            {
                TransactionId = transfer.ReceiveTransactionId
            });
        }
        // aelf ->ethereum
        return await _reportInfoAppService.CalculateCrossChainProgressAsync(transfer.FromChainId, transfer.ReceiptId);
    }
    
    // [ExceptionHandler(typeof(Exception),typeof(InvalidOperationException),typeof(WebException), Message = "Send receive transaction failed.", 
    //     ReturnDefault = ReturnDefault.Default, LogTargets = ["transfer"])]
    public virtual async Task<string> SendReceiveTransactionAsync(CrossChainTransfer transfer)
    {
        var transferToken = await _tokenRepository.GetAsync(transfer.TransferTokenId);
        var symbol =
            _tokenSymbolMappingProvider.GetMappingSymbol(transfer.FromChainId, transfer.ToChainId,
                transferToken.Symbol);
        var swapId = await _bridgeContractAppService.GetSwapIdByTokenAsync(transfer.ToChainId, transfer.FromChainId,
            symbol);
        if (string.IsNullOrEmpty(swapId))
        {
            return "";
        }
        var amount = (new BigDecimal(transfer.TransferAmount)) * BigInteger.Pow(10, transferToken.Decimals);
        return await _bridgeContractAppService.SwapTokenAsync(transfer.ToChainId, swapId, transfer.ReceiptId,
            amount.ToString(),
            transfer.ToAddress);
    }
}