using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace AElf.CrossChainServer.Contracts.Report;

[RemoteService(IsEnabled = false)]
public class ReportContractAppService : CrossChainServerAppService, IReportContractAppService
{
    private readonly IReportContractProvider _reportContractProvider;
    private readonly ReportContractOptions _reportContractOptions;
    private readonly AccountOptions _accountOptions;

    public ReportContractAppService(IReportContractProvider reportContractProvider,
        IOptionsSnapshot<ReportContractOptions> oracleOptions, IOptionsSnapshot<AccountOptions> accountOptions)
    {
        _reportContractProvider = reportContractProvider;
        _reportContractOptions = oracleOptions.Value;
        _accountOptions = accountOptions.Value;
    }

    public async Task<string> QueryOracleAsync(string chainId, string targetChainId, string receiptId,
        string receiptHash,string receiptInfo)
    {
        var privateKey = _accountOptions.PrivateKeys[chainId];
        var contractAddress = _reportContractOptions.ContractAddresses[chainId];
        return await _reportContractProvider.QueryOracleAsync(chainId, contractAddress, privateKey, targetChainId,
            receiptId, receiptHash, receiptInfo);
    }
}