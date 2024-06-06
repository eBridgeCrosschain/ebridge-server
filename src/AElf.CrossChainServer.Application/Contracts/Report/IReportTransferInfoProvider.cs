using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;
using Nethereum.Util;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Contracts.Report;

public interface IReportTransferInfoProvider
{
    Task<(string, string)> GetCrossChainTransferInfoAsync(string chainId,string targetChainId,string receiptId);
}

public class ReportTransferInfoProvider : IReportTransferInfoProvider,ITransientDependency
{
    private readonly IEnumerable<ICrossChainTransferProvider> _crossChainTransferProviders;
    private readonly IChainAppService _chainAppService;
    private readonly ITokenAppService _tokenAppService;

    public ReportTransferInfoProvider(IEnumerable<ICrossChainTransferProvider> crossChainTransferProviders, IChainAppService chainAppService, ITokenAppService tokenAppService)
    {
        _crossChainTransferProviders = crossChainTransferProviders.ToList();
        _chainAppService = chainAppService;
        _tokenAppService = tokenAppService;
    }

    public async Task<(string, string)> GetCrossChainTransferInfoAsync(string chainId,string targetChainId,string receiptId)
    {
        var crossChainType = await GetCrossChainTypeAsync(chainId, targetChainId);
        var transferInfo = await GetCrossChainTransferProvider(crossChainType)
            .FindTransferAsync(chainId, targetChainId, null, receiptId);
        var token = await _tokenAppService.GetAsync(transferInfo.TransferTokenId);
        var amount = (new BigDecimal(transferInfo.TransferAmount)) * BigInteger.Pow(10, token.Decimals);
        return (amount.ToString(), transferInfo.ToAddress);
    }

    private async Task<CrossChainType> GetCrossChainTypeAsync(string fromChainId, string toChainId)
    {
        var fromChain = await _chainAppService.GetAsync(fromChainId);
        var toChain = await _chainAppService.GetAsync(toChainId);

        if (fromChain == null || toChain == null)
        {
            return CrossChainType.Homogeneous;
        }

        return fromChain.Type == toChain.Type ? CrossChainType.Homogeneous : CrossChainType.Heterogeneous;
    }

    private ICrossChainTransferProvider GetCrossChainTransferProvider(CrossChainType crossChainType)
    {
        return _crossChainTransferProviders.First(o => o.CrossChainType == crossChainType);
    }
}