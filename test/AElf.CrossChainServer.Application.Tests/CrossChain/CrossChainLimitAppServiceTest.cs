using System;
using System.Threading.Tasks;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainLimitAppServiceTest: CrossChainServerApplicationTestBase
{
    private readonly ICrossChainLimitAppService _crossChainLimitAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly INESTRepository<CrossChainRateLimitIndex, Guid> _crossChainRateLimitIndexRepository;
    private readonly INESTRepository<CrossChainDailyLimitIndex, Guid> _crossChainDailyLimitIndexRepository;

    public CrossChainLimitAppServiceTest()
    {
        _crossChainRateLimitIndexRepository = GetRequiredService<INESTRepository<CrossChainRateLimitIndex, Guid>>();
        _crossChainDailyLimitIndexRepository = GetRequiredService<INESTRepository<CrossChainDailyLimitIndex, Guid>>();
        _crossChainLimitAppService = GetRequiredService<ICrossChainLimitAppService>();
        _tokenAppService = GetRequiredService<ITokenAppService>();
    }

    [Fact]
    public async Task RateLimitTest()
    {
        var token = await _tokenAppService.CreateAsync(new TokenCreateInput
        {
            ChainId = "Ton",
            Decimals = 6,
            Symbol = "USDT",
            Address = "USDTAddress"
        });

        var setInput = new SetCrossChainRateLimitInput
        {
            ChainId = "Ton",
            Type = CrossChainLimitType.Receipt,
            TargetChainId = "AELF",
            Capacity = 100,
            IsEnable = true,
            Rate = 20,
            CurrentAmount = 20,
            TokenId = token.Id
        };

        await _crossChainLimitAppService.SetCrossChainRateLimitAsync(setInput);

        var limit = await _crossChainRateLimitIndexRepository.GetListAsync();
        limit.Item2.Count.ShouldBe(1);
        limit.Item2[0].ChainId.ShouldBe(setInput.ChainId);
        limit.Item2[0].Type.ShouldBe(setInput.Type);
        limit.Item2[0].TargetChainId.ShouldBe(setInput.TargetChainId);
        limit.Item2[0].Capacity.ShouldBe(setInput.Capacity);
        limit.Item2[0].IsEnable.ShouldBe(setInput.IsEnable);
        limit.Item2[0].Rate.ShouldBe(setInput.Rate);
        limit.Item2[0].CurrentAmount.ShouldBe(setInput.CurrentAmount);
        limit.Item2[0].Token.Id.ShouldBe(token.Id);
        
        setInput = new SetCrossChainRateLimitInput
        {
            ChainId = "Ton",
            Type = CrossChainLimitType.Receipt,
            TargetChainId = "AELF",
            Capacity = 200,
            IsEnable = true,
            Rate = 20,
            CurrentAmount = 50,
            TokenId = token.Id
        };

        await _crossChainLimitAppService.SetCrossChainRateLimitAsync(setInput);

        limit = await _crossChainRateLimitIndexRepository.GetListAsync();
        limit.Item2.Count.ShouldBe(1);
        limit.Item2[0].ChainId.ShouldBe(setInput.ChainId);
        limit.Item2[0].Type.ShouldBe(setInput.Type);
        limit.Item2[0].TargetChainId.ShouldBe(setInput.TargetChainId);
        limit.Item2[0].Capacity.ShouldBe(setInput.Capacity);
        limit.Item2[0].IsEnable.ShouldBe(setInput.IsEnable);
        limit.Item2[0].Rate.ShouldBe(setInput.Rate);
        limit.Item2[0].CurrentAmount.ShouldBe(setInput.CurrentAmount);
        limit.Item2[0].Token.Id.ShouldBe(token.Id);
        
        await _crossChainLimitAppService.ConsumeCrossChainRateLimitAsync(new ConsumeCrossChainRateLimitInput
        {
            ChainId = "Ton",
            Type = CrossChainLimitType.Receipt,
            TargetChainId = "AELF",
            TokenId = token.Id,
            Amount = 10
        });
        
        limit = await _crossChainRateLimitIndexRepository.GetListAsync();
        limit.Item2[0].CurrentAmount.ShouldBe(40);
    }

    [Fact]
    public async Task DailyLimitTest()
    {
        var token = await _tokenAppService.CreateAsync(new TokenCreateInput
        {
            ChainId = "Ton",
            Decimals = 6,
            Symbol = "USDT",
            Address = "USDTAddress"
        });

        var setInput = new SetCrossChainDailyLimitInput
        {
            ChainId = "Ton",
            Type = CrossChainLimitType.Receipt,
            TargetChainId = "AELF",
            TokenId = token.Id,
            RefreshTime = 100,
            DailyLimit = 100,
            RemainAmount = 20
        };

        await _crossChainLimitAppService.SetCrossChainDailyLimitAsync(setInput);

        var limit = await _crossChainDailyLimitIndexRepository.GetListAsync();
        limit.Item2.Count.ShouldBe(1);
        limit.Item2[0].ChainId.ShouldBe(setInput.ChainId);
        limit.Item2[0].Type.ShouldBe(setInput.Type);
        limit.Item2[0].TargetChainId.ShouldBe(setInput.TargetChainId);
        limit.Item2[0].RefreshTime.ShouldBe(setInput.RefreshTime);
        limit.Item2[0].DailyLimit.ShouldBe(setInput.DailyLimit);
        limit.Item2[0].RemainAmount.ShouldBe(setInput.RemainAmount);
        limit.Item2[0].Token.Id.ShouldBe(token.Id);
        
        setInput = new SetCrossChainDailyLimitInput
        {
            ChainId = "Ton",
            Type = CrossChainLimitType.Receipt,
            TargetChainId = "AELF",
            RefreshTime = 200,
            DailyLimit = 200,
            RemainAmount = 50,
            TokenId = token.Id
        };

        await _crossChainLimitAppService.SetCrossChainDailyLimitAsync(setInput);

        limit = await _crossChainDailyLimitIndexRepository.GetListAsync();
        limit.Item2.Count.ShouldBe(1);
        limit.Item2[0].ChainId.ShouldBe(setInput.ChainId);
        limit.Item2[0].Type.ShouldBe(setInput.Type);
        limit.Item2[0].TargetChainId.ShouldBe(setInput.TargetChainId);
        limit.Item2[0].RefreshTime.ShouldBe(setInput.RefreshTime);
        limit.Item2[0].DailyLimit.ShouldBe(setInput.DailyLimit);
        limit.Item2[0].RemainAmount.ShouldBe(setInput.RemainAmount);
        limit.Item2[0].Token.Id.ShouldBe(token.Id);
        
        await _crossChainLimitAppService.ConsumeCrossChainDailyLimitAsync(new ConsumeCrossChainDailyLimitInput
        {
            ChainId = "Ton",
            Type = CrossChainLimitType.Receipt,
            TargetChainId = "AELF",
            TokenId = token.Id,
            Amount = 10
        });
        
        limit = await _crossChainDailyLimitIndexRepository.GetListAsync();
        limit.Item2[0].RemainAmount.ShouldBe(40);
    }
}