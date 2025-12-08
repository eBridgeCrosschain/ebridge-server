using System;
using System.Threading.Tasks;

namespace AElf.CrossChainServer.CrossChain;

public interface ICrossChainIndexingInfoAppService
{
    Task CreateAsync(CreateCrossChainIndexingInfoInput input);
    Task CleanAsync(DateTime time);
    Task<int> CalculateCrossChainProgressAsync(string fromChainId, string toChainId, long height, DateTime txTime);
    Task<int> CalculateCrossChainProgressAsync(string fromChainId, string toChainId, long height);
    Task AddIndexAsync(AddCrossChainIndexingInfoIndexInput input);
    Task DeleteIndexAsync(Guid id);
}