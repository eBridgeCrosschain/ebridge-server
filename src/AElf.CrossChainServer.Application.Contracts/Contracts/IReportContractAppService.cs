using System.Threading.Tasks;

namespace AElf.CrossChainServer.Contracts;

public interface IReportContractAppService
{
    Task<string> QueryOracleAsync(string chainId, string targetChainId, string receiptId, string receiptHash,long amount,string targetAddress);
}