using System.Threading.Tasks;

namespace AElf.CrossChainServer.Indexer;

public interface IEvmIndexerAppService
{
    Task<long> GetCurrentBlockNumberAsync(string chainId);
}