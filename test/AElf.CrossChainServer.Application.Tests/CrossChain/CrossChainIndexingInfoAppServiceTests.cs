using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainIndexingInfoAppServiceTests: CrossChainServerApplicationTestBase
{
    private readonly ICrossChainIndexingInfoAppService _crossChainIndexingInfoAppService;
    private readonly INESTRepository<CrossChainIndexingInfoIndex, Guid> _nestRepository;
    private readonly ICrossChainIndexingInfoRepository _crossChainIndexingInfoRepository;

    public CrossChainIndexingInfoAppServiceTests()
    {
        _crossChainIndexingInfoAppService = GetRequiredService<ICrossChainIndexingInfoAppService>();
        _nestRepository = GetRequiredService<INESTRepository<CrossChainIndexingInfoIndex, Guid>>();
        _crossChainIndexingInfoRepository = GetRequiredService<ICrossChainIndexingInfoRepository>();
    }

    [Fact]
    public async Task CalculateCrossChainProgressTest()
    {
        var progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF", "SideChain_tDVV",
                100);
        progress.ShouldBe(0);

        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 99000,
            BlockTime = DateTime.UtcNow.AddMinutes(-5),
            IndexBlockHeight = 60,
            IndexChainId = "MainChain_AELF",
        });

        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 80,
            IndexChainId = "MainChain_AELF",
        });

        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF", "SideChain_tDVV",
                100);
        progress.ShouldBe(50);
    }
    
    [Fact]
    public async Task Calculate_IndexNotExist_Test()
    {
        var progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF", "SideChain_tDVV",
                100);
        progress.ShouldBe(0);

        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 80,
            IndexChainId = "MainChain_AELF",
        });
        
        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF", "SideChain_tDVV",
                100);
        progress.ShouldBe(100);
    }

    [Fact]
    public async Task Calculate_IsAELF_Test()
    {
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () => await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("Ethereum", "SideChain_tDVV", 100));
        exception.Message.ShouldContain("parameter chainId is not valid");
    }

    [Fact]
    public async Task Homogeneous_MainToSide_Test()
    {
        var progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF", "SideChain_tDVV", 100, DateTime.UtcNow);
        progress.ShouldBe(0);
        
        // 9992731->1866392
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 99000,
            BlockTime = DateTime.UtcNow.AddMinutes(-5),
            IndexBlockHeight = 60,
            IndexChainId = "MainChain_AELF",
        });

        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 80,
            IndexChainId = "MainChain_AELF",
        });
        
        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF", "SideChain_tDVV", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(50);
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 100,
            IndexChainId = "MainChain_AELF",
        });
        
        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("MainChain_AELF", "SideChain_tDVV", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(100);
    }

    [Fact]
    public async Task Homogeneous_SideToMain_Test()
    {
        var progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("SideChain_tDVV", "MainChain_AELF", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(0);
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "MainChain_AELF",
            BlockHeight = 100,
            BlockTime = DateTime.UtcNow.AddMinutes(-5),
            IndexBlockHeight = 20,
            IndexChainId = "SideChain_tDVV"
        });

        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "MainChain_AELF",
            BlockHeight = 200,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 60,
            IndexChainId = "SideChain_tDVV"
        });
        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("SideChain_tDVV", "MainChain_AELF", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(25);
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "MainChain_AELF",
            BlockHeight = 300,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 110,
            IndexChainId = "SideChain_tDVV"
        });
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 120,
            BlockTime = DateTime.UtcNow.AddSeconds(-15),
            IndexBlockHeight = 200,
            IndexChainId = "MainChain_AELF"
        });
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 120,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 250,
            IndexChainId = "MainChain_AELF"
        });
        
        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("SideChain_tDVV", "MainChain_AELF", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(75);
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 150,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 300,
            IndexChainId = "MainChain_AELF"
        });
        
        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("SideChain_tDVV", "MainChain_AELF", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(100);
    }

    [Fact]
    public async Task Homogeneous_SideToSide_Test()
    {
        // 1866392->1931928
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "MainChain_AELF",
            BlockHeight = 99000,
            BlockTime = DateTime.UtcNow.AddMinutes(-5),
            IndexBlockHeight = 60,
            IndexChainId = "SideChain_tDVV"
        });

        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "MainChain_AELF",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 80,
            IndexChainId = "SideChain_tDVV"
        });
        
        var progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("SideChain_tDVV", "SideChain_tDVW", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(25);
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "MainChain_AELF",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 100,
            IndexChainId = "SideChain_tDVV"
        });
        
        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("SideChain_tDVV", "SideChain_tDVW", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(50);
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVW",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow.AddSeconds(-5),
            IndexBlockHeight = 90000,
            IndexChainId = "MainChain_AELF"
        });
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVW",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 95000,
            IndexChainId = "MainChain_AELF"
        });
        
        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("SideChain_tDVV", "SideChain_tDVW", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(50);
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 100000,
            IndexChainId = "MainChain_AELF"
        });
        
        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("SideChain_tDVV", "SideChain_tDVW", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(75);
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVW",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 100000,
            IndexChainId = "MainChain_AELF"
        });
        
        progress =
            await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync("SideChain_tDVV", "SideChain_tDVW", 100, DateTime.UtcNow.AddSeconds(-10));
        progress.ShouldBe(100);
    }

    [Fact]
    public async Task CleanTest()
    {
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "MainChain_AELF",
            BlockHeight = 99000,
            BlockTime = DateTime.UtcNow.AddMinutes(-5),
            IndexBlockHeight = 60,
            IndexChainId = "SideChain_tDVV"
        });
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "MainChain_AELF",
            BlockHeight = 99001,
            BlockTime = DateTime.UtcNow.AddMinutes(-4),
            IndexBlockHeight = 61,
            IndexChainId = "SideChain_tDVV"
        });
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "MainChain_AELF",
            BlockHeight = 99002,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 62,
            IndexChainId = "SideChain_tDVV"
        });

        var list = await _crossChainIndexingInfoRepository.GetListAsync();
        list.Count.ShouldBe(3);
        
        await _crossChainIndexingInfoAppService.CleanAsync(DateTime.UtcNow.AddMinutes(-1));
        
        list = await _crossChainIndexingInfoRepository.GetListAsync();
        list.Count.ShouldBe(1);
        list[0].IndexBlockHeight.ShouldBe(62);
    }

    [Fact]
    public async Task Create_Repeat_Test()
    {
        var input = new CreateCrossChainIndexingInfoInput
        {
            ChainId = "MainChain_AELF",
            BlockHeight = 99000,
            BlockTime = DateTime.UtcNow.AddMinutes(-5),
            IndexBlockHeight = 60,
            IndexChainId = "SideChain_tDVV"
        };
        
        await _crossChainIndexingInfoAppService.CreateAsync(input);
        await _crossChainIndexingInfoAppService.CreateAsync(input);
        
        var list = await _crossChainIndexingInfoRepository.GetListAsync();
        list.Count.ShouldBe(1);
    }
}