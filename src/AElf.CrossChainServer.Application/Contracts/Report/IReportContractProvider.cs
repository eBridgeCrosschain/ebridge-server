using System.Threading.Tasks;

namespace AElf.CrossChainServer.Contracts.Report;

public interface IReportContractProvider
{
    Task<string> QueryOracleAsync(string chainId, string contractAddress, string pubKey,
        string targetChainId, string receiptId, string receiptHash);
}