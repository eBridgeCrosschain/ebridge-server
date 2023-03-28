using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.Tokens;
using Nethereum.Util;
using Volo.Abp.DependencyInjection;

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

    public HeterogeneousCrossChainTransferProvider(IChainAppService chainAppService,
        IOracleQueryInfoAppService oracleQueryInfoAppService, IReportInfoAppService reportInfoAppService,
        ITokenRepository tokenRepository, ITokenSymbolMappingProvider tokenSymbolMappingProvider,
        IBridgeContractAppService bridgeContractAppService, ICrossChainTransferRepository crossChainTransferRepository)
    {
        _chainAppService = chainAppService;
        _oracleQueryInfoAppService = oracleQueryInfoAppService;
        _reportInfoAppService = reportInfoAppService;
        _tokenRepository = tokenRepository;
        _tokenSymbolMappingProvider = tokenSymbolMappingProvider;
        _bridgeContractAppService = bridgeContractAppService;
        _crossChainTransferRepository = crossChainTransferRepository;
    }

    public CrossChainType CrossChainType { get; } = CrossChainType.Heterogeneous;
    
    public async Task<CrossChainTransfer> FindTransferAsync(string fromChainId, string toChainId,
        string transferTransactionId, string receiptId)
    {
        return await _crossChainTransferRepository.FindAsync(o =>
            o.FromChainId == fromChainId && o.ToChainId == toChainId && o.ReceiptId == receiptId);
    }

    public async Task<double> CalculateCrossChainProgressAsync(CrossChainTransfer transfer)
    {
        var chain = await _chainAppService.GetAsync(transfer.ToChainId);
        if (chain.Type == BlockchainType.AElf)
        {
            return await _oracleQueryInfoAppService.CalculateCrossChainProgressAsync(transfer.ReceiptId);
        }

        return await _reportInfoAppService.CalculateCrossChainProgressAsync(transfer.ReceiptId);
    }

    public async Task<string> SendReceiveTransactionAsync(CrossChainTransfer transfer)
    {
        var transferToken = await _tokenRepository.GetAsync(transfer.TransferTokenId);
        var symbol =
            _tokenSymbolMappingProvider.GetMappingSymbol(transfer.FromChainId, transfer.ToChainId,
                transferToken.Symbol);
        var swapId = await _bridgeContractAppService.GetSwapIdByTokenAsync(transfer.ToChainId, transfer.FromChainId,
            symbol);
        var amount = (new BigDecimal(transfer.TransferAmount)) * BigInteger.Pow(10, transferToken.Decimals);
        return await _bridgeContractAppService.SwapTokenAsync(transfer.ToChainId, swapId, transfer.ReceiptId,
            amount.ToString(),
            transfer.ToAddress);
    }
}