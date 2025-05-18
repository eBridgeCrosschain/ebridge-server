using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Serilog;
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
    private readonly IBridgeContractAppService _bridgeContractAppService;
    private readonly IChainAppService _chainAppService;
    private readonly IBlockchainAppService _blockchainAppService;
    private readonly ISettingManager _settingManager;
    private readonly LimitSyncOptions _limitSyncOptions;
    private readonly ITokenAppService _tokenAppService;

    public CrossChainLimitAppService(ICrossChainDailyLimitRepository crossChainDailyLimitRepository,
        ICrossChainRateLimitRepository crossChainRateLimitRepository,
        INESTRepository<CrossChainRateLimitIndex, Guid> crossChainRateLimitIndexRepository,
        INESTRepository<CrossChainDailyLimitIndex, Guid> crossChainDailyLimitIndexRepository,
        ITokenRepository tokenRepository, IBridgeContractAppService bridgeContractAppService,
        IChainAppService chainAppService, IBlockchainAppService blockchainAppService, ISettingManager settingManager,
        IOptionsSnapshot<LimitSyncOptions> limitSyncOptions, ITokenAppService tokenAppService)
    {
        _crossChainDailyLimitRepository = crossChainDailyLimitRepository;
        _crossChainRateLimitRepository = crossChainRateLimitRepository;
        _crossChainRateLimitIndexRepository = crossChainRateLimitIndexRepository;
        _crossChainDailyLimitIndexRepository = crossChainDailyLimitIndexRepository;
        _tokenRepository = tokenRepository;
        _bridgeContractAppService = bridgeContractAppService;
        _chainAppService = chainAppService;
        _blockchainAppService = blockchainAppService;
        _settingManager = settingManager;
        _tokenAppService = tokenAppService;
        _limitSyncOptions = limitSyncOptions.Value;
    }

    public async Task InitLimitAsync()
    {
        var evmChainList = await _chainAppService.GetListAsync(new GetChainsInput
        {
            Type = BlockchainType.Evm
        });
        foreach (var chain in evmChainList.Items)
        {
            Log.Debug("Sync limit info from chain {chainId}.", chain.Id);
            // Step 1: Retrieve the current height of the EVM chain and insert a new EVM limit sync height.  
            var currentChainHeight = await _blockchainAppService.GetChainHeightAsync(chain.Id);
            await _settingManager.SetAsync(chain.Id,
                GetSettingKey(CrossChainServerSettings.EvmDailyLimitSetIndexerSync, null),
                currentChainHeight.ToString());
            await _settingManager.SetAsync(chain.Id,
                GetSettingKey(CrossChainServerSettings.EvmDailyLimitConsumedIndexerSync, null),
                currentChainHeight.ToString());
            await _settingManager.SetAsync(chain.Id,
                GetSettingKey(CrossChainServerSettings.EvmRateLimitSetIndexerSync, null),
                currentChainHeight.ToString());
            await _settingManager.SetAsync(chain.Id,
                GetSettingKey(CrossChainServerSettings.EvmRateLimitConsumedIndexerSync, null),
                currentChainHeight.ToString());
            // Step 2: Query the EVM contract to sync the liquidity of configured tokens - getBalance.  
            // receipt limit
            var limitsInfos = _limitSyncOptions.LimitInfos[chain.Id];
            var tokenIdList = new List<Guid>();
            var targetChainIdList = new List<string>();
            var aelfTargetChainIdList = new List<string>();
            foreach (var limit in limitsInfos)
            {
                var aelfChain = await _chainAppService.GetByAElfChainIdAsync(ChainHelper.ConvertBase58ToChainId(limit.TargetChainId));
                var token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId = chain.Id,
                    Address = limit.TokenAddress
                });
                tokenIdList.Add(token.Id);
                targetChainIdList.Add(aelfChain.Id);
                aelfTargetChainIdList.Add(limit.TargetChainId);
                var receiptDailyLimit =
                    await _bridgeContractAppService.GetDailyLimitAsync(chain.Id, token.Id, aelfChain.Id);
                await SetCrossChainDailyLimitAsync(new SetCrossChainDailyLimitInput
                {
                    ChainId = chain.Id,
                    DailyLimit = receiptDailyLimit.DefaultDailyLimit,
                    RefreshTime = receiptDailyLimit.RefreshTime,
                    RemainAmount = receiptDailyLimit.CurrentDailyLimit,
                    TokenId = token.Id,
                    TargetChainId = limit.TargetChainId,
                    Type = CrossChainLimitType.Receipt
                });
                var swapDailyLimit =
                    await _bridgeContractAppService.GetSwapDailyLimitAsync(chain.Id, limit.SwapId);
                await SetCrossChainDailyLimitAsync(new SetCrossChainDailyLimitInput
                {
                    ChainId = chain.Id,
                    DailyLimit = swapDailyLimit.DefaultDailyLimit,
                    RefreshTime = swapDailyLimit.RefreshTime,
                    RemainAmount = swapDailyLimit.CurrentDailyLimit,
                    TokenId = token.Id,
                    TargetChainId = limit.TargetChainId,
                    Type = CrossChainLimitType.Swap
                });
            }

            var rateLimit =
                await _bridgeContractAppService.GetCurrentReceiptTokenBucketStatesAsync(chain.Id, tokenIdList,
                    targetChainIdList);
            for (var i = 0; i < rateLimit.Count; i++)
            {
                await SetCrossChainRateLimitAsync(new SetCrossChainRateLimitInput
                {
                    ChainId = chain.Id,
                    CurrentAmount = rateLimit[i].CurrentTokenAmount,
                    Capacity = rateLimit[i].Capacity,
                    Rate = rateLimit[i].RefillRate,
                    TokenId = tokenIdList[i],
                    TargetChainId = aelfTargetChainIdList[i],
                    Type = CrossChainLimitType.Receipt
                });
            }
            var swapRateLimit =
                await _bridgeContractAppService.GetCurrentSwapTokenBucketStatesAsync(chain.Id, tokenIdList,
                    targetChainIdList);
            for (var i = 0; i < swapRateLimit.Count; i++)
            {
                await SetCrossChainRateLimitAsync(new SetCrossChainRateLimitInput
                {
                    ChainId = chain.Id,
                    CurrentAmount = swapRateLimit[i].CurrentTokenAmount,
                    Capacity = swapRateLimit[i].Capacity,
                    Rate = swapRateLimit[i].RefillRate,
                    TokenId = tokenIdList[i],
                    TargetChainId = aelfTargetChainIdList[i],
                    Type = CrossChainLimitType.Swap
                });
            }
        }

        Log.Information("Finish to sync limit info from chain.");
    }

    private string GetSettingKey(string syncType, string typePrefix)
    {
        return string.IsNullOrWhiteSpace(typePrefix) ? syncType : $"{typePrefix}-{syncType}";
    }

    public async Task SetCrossChainRateLimitAsync(SetCrossChainRateLimitInput input)
    {
        var limit = await _crossChainRateLimitRepository.FindAsync(o =>
            o.ChainId == input.ChainId && o.TargetChainId == input.TargetChainId && o.TokenId == input.TokenId &&
            o.Type == input.Type);

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
            o.ChainId == input.ChainId && o.TargetChainId == input.TargetChainId && o.TokenId == input.TokenId &&
            o.Type == input.Type);
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
            o.ChainId == input.ChainId && o.TargetChainId == input.TargetChainId && o.TokenId == input.TokenId &&
            o.Type == input.Type);

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
        Log.Debug("ConsumeCrossChainDailyLimitAsync, chainId: {chainId}, targetChainId: {targetChainId}, tokenId: {tokenId}, type: {type}, amount: {amount}",
            input.ChainId, input.TargetChainId, input.TokenId, input.Type, input.Amount);
        var limit = await _crossChainDailyLimitRepository.GetAsync(o =>
            o.ChainId == input.ChainId && o.TargetChainId == input.TargetChainId && o.TokenId == input.TokenId &&
            o.Type == input.Type);
        limit.RemainAmount -= input.Amount;
        await _crossChainDailyLimitRepository.UpdateAsync(limit);
    }
}