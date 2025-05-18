using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.TokenPool;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace AElf.CrossChainServer.Contracts.Bridge;

[RemoteService(IsEnabled = false)]
public class BridgeContractAppService : CrossChainServerAppService, IBridgeContractAppService
{
    private readonly IBridgeContractProviderFactory _bridgeContractProviderFactory;
    private readonly BridgeContractOptions _bridgeContractOptions;

    public BridgeContractAppService(IBridgeContractProviderFactory bridgeContractProviderFactory,
        IOptionsSnapshot<BridgeContractOptions> options)
    {
        _bridgeContractProviderFactory = bridgeContractProviderFactory;
        _bridgeContractOptions = options.Value;
    }

    public async Task<DailyLimitDto> GetDailyLimitAsync(string chainId, Guid tokenId, string targetChainId)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new DailyLimitDto();
        }

        return await provider.GetReceiptDailyLimitAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].LimiterContract, tokenId, targetChainId);
    }

    public async Task<DailyLimitDto> GetSwapDailyLimitAsync(string chainId, string swapId)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new DailyLimitDto();
        }

        return await provider.GetSwapDailyLimitAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].LimiterContract, swapId);
    }

    public async Task<List<TokenBucketDto>> GetCurrentReceiptTokenBucketStatesAsync(string chainId, List<Guid> tokenIds,
        List<string> targetChainIds)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new List<TokenBucketDto>();
        }

        return await provider.GetCurrentReceiptTokenBucketStatesAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].LimiterContract, tokenIds, targetChainIds);
    }

    public async Task<List<TokenBucketDto>> GetCurrentSwapTokenBucketStatesAsync(string chainId, List<Guid> tokenIds,
        List<string> fromChainIds)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new List<TokenBucketDto>();
        }

        return await provider.GetCurrentSwapTokenBucketStatesAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].LimiterContract, tokenIds, fromChainIds);
    }

    public async Task<List<PoolLiquidityDto>> GetPoolLiquidityAsync(string chainId, string contractAddress,
        List<Guid> tokenIds)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new List<PoolLiquidityDto>();
        }

        return await provider.GetPoolLiquidityAsync(chainId, contractAddress, tokenIds);
    }
}