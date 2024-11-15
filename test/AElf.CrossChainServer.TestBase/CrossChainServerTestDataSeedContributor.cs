using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer;

public class CrossChainServerTestDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IChainRepository _chainRepository;

    public CrossChainServerTestDataSeedContributor(IChainRepository chainRepository)
    {
        _chainRepository = chainRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        /* Seed additional test data... */
        await _chainRepository.InsertAsync(new Chain
        {
            Id = "Ethereum",
            Type = BlockchainType.Evm,
            Name = "Ethereum",
            IsMainChain = true,
            AElfChainId = 0
        });
        
        await _chainRepository.InsertAsync(new Chain()
        {
            Id = "MainChain_AELF",
            Type = BlockchainType.AElf,
            Name = "Main Chain AELF",
            IsMainChain = true,
            AElfChainId = 9992731
        });
        
        await _chainRepository.InsertAsync(new Chain()
        {
            Id = "SideChain_tDVV",
            Type = BlockchainType.AElf,
            Name = "Side Chain tDVV",
            IsMainChain = false,
            AElfChainId = 1866392
        });
        
        await _chainRepository.InsertAsync(new Chain()
        {
            Id = "SideChain_tDVW",
            Type = BlockchainType.AElf,
            Name = "Side Chain tDVW",
            IsMainChain = false,
            AElfChainId = 1931928
        });
        
        await _chainRepository.InsertAsync(new Chain()
        {
            Id = "Ton",
            Type = BlockchainType.Tvm,
            Name = "Ton",
            IsMainChain = true,
            AElfChainId = 0
        });
    }
}
