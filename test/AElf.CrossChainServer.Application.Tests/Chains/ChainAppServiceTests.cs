using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.Chains;

public class ChainAppServiceTests : CrossChainServerApplicationTestBase
{
    private readonly IChainAppService _chainAppService;
    private readonly IChainRepository _chainRepository;
    private readonly INESTRepository<ChainIndex, string> _chainIndexRepository;

    public ChainAppServiceTests()
    {
        _chainAppService = GetRequiredService<IChainAppService>();
        _chainRepository = GetRequiredService<IChainRepository>();
        _chainIndexRepository = GetRequiredService<INESTRepository<ChainIndex, string>>();
    }

    [Fact]
    public async Task GetTest()
    {
        var chain = await _chainAppService.GetAsync("MainChain_AELF");
        chain.Id.ShouldBe("MainChain_AELF");
        chain.Name.ShouldBe("Main Chain AELF");

        chain = await _chainAppService.GetByNameAsync("Main Chain AELF");
        chain.Id.ShouldBe("MainChain_AELF");
        chain.Name.ShouldBe("Main Chain AELF");
        
        chain = await _chainAppService.GetByAElfChainIdAsync(9992731);
        chain.Id.ShouldBe("MainChain_AELF");
        chain.Name.ShouldBe("Main Chain AELF");
        
        await _chainIndexRepository.AddOrUpdateAsync(new ChainIndex
        {
            Id = "MainChain_AELF",
            Name = "Main Chain AELF",
            Type = BlockchainType.AElf
        });
        
        await _chainIndexRepository.AddOrUpdateAsync(new ChainIndex
        {
            Id = "SideChain_tDVV",
            Name = "Side Chain tDVV",
            Type = BlockchainType.AElf
        });
        
        await _chainIndexRepository.AddOrUpdateAsync(new ChainIndex
        {
            Id = "Ethereum",
            Name = "Ethereum",
            Type = BlockchainType.Evm
        });
        
        await _chainIndexRepository.AddOrUpdateAsync(new ChainIndex
        {
            Id = "Tron",
            Name = "Tron",
            Type = BlockchainType.Tron
        });
        
        var chains = await _chainAppService.GetListAsync(new GetChainsInput());
        chains.Items.Count.ShouldBe(4);
        
        chains = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.AElf
        });
        chains.Items.Count.ShouldBe(2);
        
        chains = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.Evm
        });
        chains.Items.Count.ShouldBe(1);
        
        chains = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.Tron
        });
        chains.Items.Count.ShouldBe(1);
    }
}