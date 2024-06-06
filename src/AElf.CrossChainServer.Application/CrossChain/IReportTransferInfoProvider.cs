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
    private readonly ICrossChainTransferRepository _crossChainTransferRepository;
    private readonly ITokenAppService _tokenAppService;

    public ReportTransferInfoProvider(IChainAppService chainAppService, ITokenAppService tokenAppService, ICrossChainTransferRepository crossChainTransferRepository)
    {
        _tokenAppService = tokenAppService;
        _crossChainTransferRepository = crossChainTransferRepository;
    }

    public async Task<(string, string)> GetCrossChainTransferInfoAsync(string chainId,string targetChainId,string receiptId)
    {
        var transferInfo = await _crossChainTransferRepository.FindAsync(o =>
            o.FromChainId == chainId && o.ToChainId == targetChainId && o.ReceiptId == receiptId);
        var token = await _tokenAppService.GetAsync(transferInfo.TransferTokenId);
        var amount = (new BigDecimal(transferInfo.TransferAmount)) * BigInteger.Pow(10, token.Decimals);
        return (amount.ToString(), transferInfo.ToAddress);
    }
}