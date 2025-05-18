using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler;
using Nethereum.Util;
using Nethereum.Web3;
using Serilog;

namespace AElf.CrossChainServer.Contracts.Bridge;

public class EvmBridgeContractProvider : EvmClientProvider, IBridgeContractProvider
{
    private readonly ITokenAppService _tokenAppService;

    public EvmBridgeContractProvider(IBlockchainClientFactory<Web3> blockchainClientFactory,
        ITokenAppService tokenAppService) : base(
        blockchainClientFactory)
    {
        _tokenAppService = tokenAppService;
    }

    public async Task<DailyLimitDto> GetReceiptDailyLimitAsync(string chainId, string contractAddress, Guid tokenId,
        string targetChainId)
    {
        var token = await _tokenAppService.GetAsync(tokenId);
        var web3 = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = web3.Eth.GetContractHandler(contractAddress);
        var receiptDailyLimit = await contractHandler
            .QueryDeserializingToObjectAsync<GetDailyLimitFunctionMessage, ReceiptDailyLimitDto>(
                new GetDailyLimitFunctionMessage
                {
                    Token = token.Address,
                    TargetChainId = targetChainId
                });
        if (receiptDailyLimit != null && receiptDailyLimit.DailyLimit > 0)
        {
            return new DailyLimitDto
            {
                RefreshTime = receiptDailyLimit.RefreshTime,
                DefaultDailyLimit =
                    (decimal)((BigDecimal)receiptDailyLimit.DailyLimit / BigInteger.Pow(10, token.Decimals)),
                CurrentDailyLimit = (decimal)((BigDecimal)receiptDailyLimit.CurrentTokenAmount /
                                              BigInteger.Pow(10, token.Decimals))
            };
        }

        return new DailyLimitDto();
    }
    
    public async Task<DailyLimitDto> GetSwapDailyLimitAsync(string chainId, string contractAddress, string swapId)
    {
        var web3 = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = web3.Eth.GetContractHandler(contractAddress);
        var swapDailyLimit = await contractHandler
            .QueryDeserializingToObjectAsync<GetSwapDailyLimitFunctionMessage, SwapDailyLimitDto>(
                new GetSwapDailyLimitFunctionMessage
                {
                    SwapId = ByteStringHelper.FromHexString(swapId).ToByteArray()
                });
        if (swapDailyLimit != null && swapDailyLimit.DailyLimit > 0)
        {
            return new DailyLimitDto
            {
                RefreshTime = swapDailyLimit.RefreshTime,
                DefaultDailyLimit =
                    (decimal)((BigDecimal)swapDailyLimit.DailyLimit / BigInteger.Pow(10, 18)),
                CurrentDailyLimit = (decimal)((BigDecimal)swapDailyLimit.CurrentTokenAmount /
                                              BigInteger.Pow(10, 18))
            };
        }

        return new DailyLimitDto();
    }

    [ExceptionHandler(typeof(Exception),
        Message = "[Evm bridge contract] Get current receipt token bucket states failed.",
        ReturnDefault = ReturnDefault.New,
        LogTargets = new[] { "chainId", "contractAddress", "tokenIds", "targetChainIds" })]
    public virtual async Task<List<TokenBucketDto>> GetCurrentReceiptTokenBucketStatesAsync(string chainId,
        string contractAddress, List<Guid> tokenIds,
        List<string> targetChainIds)
    {
        var tokenAddress = new List<string>();
        var tokenDecimals = new List<int>();
        foreach (var tokenId in tokenIds)
        {
            var token = await _tokenAppService.GetAsync(tokenId);
            tokenAddress.Add(token.Address);
            tokenDecimals.Add(token.Decimals);
        }

        var web3 = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = web3.Eth.GetContractHandler(contractAddress);
        var receiptTokenBucket = await contractHandler
            .QueryDeserializingToObjectAsync<GetCurrentReceiptTokenBucketStatesFunctionMessage, ReceiptTokenBucketsDto>(
                new GetCurrentReceiptTokenBucketStatesFunctionMessage
                {
                    Token = tokenAddress,
                    TargetChainId = targetChainIds
                });
        var tokenBuckets = receiptTokenBucket.TokenBuckets.Select((t, i) =>
            GetTokenBuckets(t.TokenCapacity, t.Rate,t.IsEnabled,t.LastUpdatedTime,t.CurrentTokenAmount, tokenDecimals[i])).ToList();
        return tokenBuckets;
    }

    [ExceptionHandler(typeof(Exception), Message = "[Evm bridge contract] Get current swap token bucket states failed.",
        ReturnDefault = ReturnDefault.New,
        LogTargets = new[] { "chainId", "contractAddress", "tokenIds", "fromChainIds" })]
    public virtual async Task<List<TokenBucketDto>> GetCurrentSwapTokenBucketStatesAsync(string chainId,
        string contractAddress,
        List<Guid> tokenIds, List<string> fromChainIds)
    {
        var tokenAddress = new List<string>();
        var tokenDecimals = new List<int>();
        foreach (var tokenId in tokenIds)
        {
            var token = await _tokenAppService.GetAsync(tokenId);
            tokenAddress.Add(token.Address);
            tokenDecimals.Add(token.Decimals);
        }

        var web3 = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = web3.Eth.GetContractHandler(contractAddress);
        var swapTokenBucket = await contractHandler
            .QueryDeserializingToObjectAsync<GetCurrentSwapTokenBucketStatesFunctionMessage, SwapTokenBucketsDto>(
                new GetCurrentSwapTokenBucketStatesFunctionMessage
                {
                    Token = tokenAddress,
                    FromChainId = fromChainIds
                });
        var tokenBuckets = swapTokenBucket.SwapTokenBuckets.Select((t, i) =>
            GetTokenBuckets(t.TokenCapacity, t.Rate,t.IsEnabled,t.LastUpdatedTime,t.CurrentTokenAmount, tokenDecimals[i])).ToList();
        return tokenBuckets;
    }

    public async Task<List<PoolLiquidityDto>> GetPoolLiquidityAsync(string chainId, string contractAddress,
        List<Guid> tokenIds)
    {
        var result = new List<PoolLiquidityDto>();
        foreach (var tokenId in tokenIds)
        {
            var token = await _tokenAppService.GetAsync(tokenId);
            var web3 = BlockchainClientFactory.GetClient(chainId);
            var balance = await web3.Eth.ERC20.GetContractService(token.Address).BalanceOfQueryAsync(contractAddress);
            Log.Debug("Get pool liquidity, chainId: {chainId}, tokenAddress: {token}, balance: {balance}", chainId,
                token.Address, balance);
            var liquidity = (decimal)((BigDecimal)balance / BigInteger.Pow(10, token.Decimals));
            result.Add(new PoolLiquidityDto
            {
                ChainId = chainId,
                Liquidity = liquidity,
                TokenId = tokenId
            });
        }

        return result;
    }

    private TokenBucketDto GetTokenBuckets(BigInteger capacity, BigInteger rate, bool isEnabled,long lastUpdatedTime, BigInteger currentTokenAmount,int tokenDecimal)
    {
        if (capacity == 0 || rate == 0)
        {
            return new TokenBucketDto();
        }

        var tokenCapacity = (decimal)(new BigDecimal(capacity) / BigInteger.Pow(10, tokenDecimal));
        var refillRate = (decimal)(new BigDecimal(rate) / BigInteger.Pow(10, tokenDecimal));
        var currentAmount = (decimal)(new BigDecimal(currentTokenAmount) / BigInteger.Pow(10, tokenDecimal));
        var maximumTimeConsumed =
            (int)Math.Ceiling(tokenCapacity / refillRate / CrossChainServerConsts.DefaultRateLimitSeconds);
        return new TokenBucketDto
        {
            Capacity = tokenCapacity,
            RefillRate = refillRate,
            MaximumTimeConsumed = maximumTimeConsumed,
            CurrentTokenAmount = currentAmount,
            IsEnabled = isEnabled,
            LastUpdatedTime = lastUpdatedTime
        };
    }
}