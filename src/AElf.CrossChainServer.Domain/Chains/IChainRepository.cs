using Volo.Abp.Domain.Repositories;

namespace AElf.CrossChainServer.Chains
{
    public interface IChainRepository : IRepository<Chain, string>
    {
        
    }
}