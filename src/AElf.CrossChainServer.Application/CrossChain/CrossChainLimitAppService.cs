using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using Nest;
using Volo.Abp;

namespace AElf.CrossChainServer.CrossChain;

[RemoteService(IsEnabled = false)]
public class CrossChainLimitAppService : CrossChainServerAppService, ICrossChainLimitAppService
{
    private readonly ICrossChainDailyLimitRepository _crossChainDailyLimitRepository;
    private readonly ICrossChainRateLimitRepository _crossChainRateLimitRepository;
    private readonly INESTRepository<CrossChainRateLimitIndex, Guid> _crossChainRateLimitIndexRepository;
    private readonly INESTRepository<CrossChainDailyLimitIndex, Guid> _crossChainDailyLimitIndexRepository;
    private readonly ITokenRepository _tokenRepository;

    public CrossChainLimitAppService(ICrossChainDailyLimitRepository crossChainDailyLimitRepository,
        ICrossChainRateLimitRepository crossChainRateLimitRepository,
        INESTRepository<CrossChainRateLimitIndex, Guid> crossChainRateLimitIndexRepository,
        INESTRepository<CrossChainDailyLimitIndex, Guid> crossChainDailyLimitIndexRepository,
        ITokenRepository tokenRepository)
    {
        _crossChainDailyLimitRepository = crossChainDailyLimitRepository;
        _crossChainRateLimitRepository = crossChainRateLimitRepository;
        _crossChainRateLimitIndexRepository = crossChainRateLimitIndexRepository;
        _crossChainDailyLimitIndexRepository = crossChainDailyLimitIndexRepository;
        _tokenRepository = tokenRepository;
    }

    public async Task SetCrossChainRateLimitAsync(SetCrossChainRateLimitInput input)
    {
        var limit = await _crossChainRateLimitRepository.FindAsync(o =>
            o.ChainId == input.ChainId && o.TargetChainId == input.TargetChainId && o.TokenId == input.TokenId && o.Type == input.Type);
        
        if (limit == null)
        {
            limit = ObjectMapper.Map<SetCrossChainRateLimitInput, CrossChainRateLimit>(input);
            await _crossChainRateLimitRepository.InsertAsync(limit);
        }
        else
        {
            limit.CurrentAmount = input.CurrentAmount;
            limit.Capacity = input.Capacity;
            limit.Rate = input.Rate;
            limit.IsEnable = input.IsEnable;
            await _crossChainRateLimitRepository.UpdateAsync(limit);
        }
    }

    public async Task SetCrossChainRateLimitIndexAsync(SetCrossChainRateLimitInput input)
    {
        var limit = ObjectMapper.Map<SetCrossChainRateLimitInput, CrossChainRateLimitIndex>(input);
        limit.Token = await _tokenRepository.GetAsync(input.TokenId);
        await _crossChainRateLimitIndexRepository.AddOrUpdateAsync(limit);
    }

    public async Task ConsumeCrossChainRateLimitAsync(ConsumeCrossChainRateLimitInput input)
    {
        var limit = await _crossChainRateLimitRepository.GetAsync(o =>
            o.ChainId == input.ChainId && o.TargetChainId == input.TargetChainId && o.TokenId == input.TokenId && o.Type == input.Type);
        if (limit.IsEnable)
        {
            limit.CurrentAmount -= input.Amount;
            await _crossChainRateLimitRepository.UpdateAsync(limit);
        }
    }

    public async Task<List<CrossChainRateLimitDto>> GetCrossChainRateLimitsAsync()
    {
        var list = await _crossChainRateLimitIndexRepository.GetListAsync();
        return ObjectMapper.Map<List<CrossChainRateLimitIndex>, List<CrossChainRateLimitDto>>(list.Item2);
    }

    public async Task SetCrossChainDailyLimitAsync(SetCrossChainDailyLimitInput input)
    {
        var limit = await _crossChainDailyLimitRepository.FindAsync(o =>
            o.ChainId == input.ChainId && o.TargetChainId == input.TargetChainId && o.TokenId == input.TokenId && o.Type == input.Type);
        
        if (limit == null)
        {
            limit = ObjectMapper.Map<SetCrossChainDailyLimitInput, CrossChainDailyLimit>(input);
            await _crossChainDailyLimitRepository.InsertAsync(limit);
        }
        else
        {
            limit.RemainAmount = input.RemainAmount;
            limit.RefreshTime = input.RefreshTime;
            limit.DailyLimit = input.DailyLimit;
            await _crossChainDailyLimitRepository.UpdateAsync(limit);
        }
    }
    
    public async Task SetCrossChainDailyLimitIndexAsync(SetCrossChainDailyLimitInput input)
    {
        var limit = ObjectMapper.Map<SetCrossChainDailyLimitInput, CrossChainDailyLimitIndex>(input);
        limit.Token = await _tokenRepository.GetAsync(input.TokenId);
        await _crossChainDailyLimitIndexRepository.AddOrUpdateAsync(limit);
    }
    
    public async Task ConsumeCrossChainDailyLimitAsync(ConsumeCrossChainDailyLimitInput input)
    {
        var limit = await _crossChainDailyLimitRepository.GetAsync(o =>
            o.ChainId == input.ChainId && o.TargetChainId == input.TargetChainId && o.TokenId == input.TokenId && o.Type == input.Type);
        limit.RemainAmount -= input.Amount;
        await _crossChainDailyLimitRepository.UpdateAsync(limit);
    }
}