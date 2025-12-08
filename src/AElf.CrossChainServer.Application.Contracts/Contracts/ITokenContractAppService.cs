using System.Threading.Tasks;
using AElf.Types;

namespace AElf.CrossChainServer.Contracts;

public interface ITokenContractAppService
{
    Task<string> CrossChainReceiveTokenAsync(string chainId, int fromChainId, long parentChainHeight,
        string transferTransaction, MerklePath merklePath);
}