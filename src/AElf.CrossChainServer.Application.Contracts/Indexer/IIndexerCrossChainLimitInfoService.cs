using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;

namespace AElf.CrossChainServer.Indexer;

public interface IIndexerCrossChainLimitInfoService
{
    public Task<List<IndexerCrossChainLimitInfo>> GetAllCrossChainLimitInfoIndexAsync();
    
    public Task<List<IndexerCrossChainLimitInfo>> GetCrossChainLimitInfoIndexAsync(string fromChainId, string toChainId, string symbol);

}