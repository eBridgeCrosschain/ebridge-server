using System.Threading.Tasks;
using AElf.Standards.ACS7;

namespace AElf.CrossChainServer.Contracts;

public interface ICrossChainContractAppService
{
    Task<CrossChainMerkleProofContext> GetBoundParentChainHeightAndMerklePathByHeightAsync(string chainId, long height);
}