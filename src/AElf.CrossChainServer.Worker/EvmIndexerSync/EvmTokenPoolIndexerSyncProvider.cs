using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Serilog;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public class EvmTokenPoolIndexerSyncProvider : EvmIndexerSyncProviderBase
{
    private readonly EvmContractSyncOptions _evmContractSyncOptions;
    private readonly IPoolLiquidityInfoAppService _poolLiquidityInfoAppService;
    private readonly IUserLiquidityInfoAppService _userLiquidityInfoAppService;
    private readonly ITokenAppService _tokenAppService;

    private const string LockedEvent = "Locked(address,address,string,uint256)";
    private const string ReleasedEvent = "Released(address,address,string,uint256)";
    private const string LiquidityAddedEvent = "LiquidityAdded(address,address,uint256)";
    private const string LiquidityRemovedEvent = "LiquidityRemoved(address,address,uint256)";
    private static string LockedEventSignature => LockedEvent.GetEventSignature();
    private static string ReleasedEventSignature => ReleasedEvent.GetEventSignature();
    private static string LiquidityAddedEventSignature => LiquidityAddedEvent.GetEventSignature();
    private static string LiquidityRemovedEventSignature => LiquidityRemovedEvent.GetEventSignature();

    public EvmTokenPoolIndexerSyncProvider(ISettingManager settingManager,
        IOptionsSnapshot<EvmContractSyncOptions> evmContractSyncOptions,
        IPoolLiquidityInfoAppService poolLiquidityInfoAppService, ITokenAppService tokenAppService,
        IUserLiquidityInfoAppService userLiquidityInfoAppService) : base(settingManager)
    {
        _poolLiquidityInfoAppService = poolLiquidityInfoAppService;
        _tokenAppService = tokenAppService;
        _userLiquidityInfoAppService = userLiquidityInfoAppService;
        _evmContractSyncOptions = evmContractSyncOptions.Value;
    }

    protected override string SyncType { get; } = CrossChainServerSettings.EvmPoolLiquidityIndexerSync;

    protected override async Task<long> HandleDataAsync(string chainId, long startHeight, long endHeight)
    {
        Log.ForContext("chainId", chainId).Debug(
            "Start to sync pool liquidity info {chainId} from {StartHeight} to {EndHeight}",
            chainId, startHeight, endHeight);
        var tokenPoolContractAddress = _evmContractSyncOptions.ContractAddresses[chainId].TokenPoolContract;
        var logs = await GetContractLogsAsync(chainId, tokenPoolContractAddress, startHeight, endHeight);
        foreach (var log in logs.Logs)
        {
            if (log.Type != CrossChainServerConsts.MinedEvmTransaction)
            {
                return log.BlockNumber;
            }

            if (log.Removed)
            {
                continue;
            }

            var logSignature = log.Topics[0]?.ToString()?.Substring(2);
            if (string.IsNullOrEmpty(logSignature))
            {
                continue;
            }
            await ProcessLogEventAsync(chainId, log, logSignature);
        }

        return logs.Logs.Last().BlockNumber;
    }

    private async Task ProcessLogEventAsync(string chainId, FilterLog log, string logSignature)
    {
        if (logSignature == LockedEventSignature)
        {
            await HandleLockedEventAsync(chainId, log);
        }
        else if (logSignature == ReleasedEventSignature)
        {
            await HandleReleasedEventAsync(chainId, log);
        }
        else if (logSignature == LiquidityAddedEventSignature)
        {
            await HandleLiquidityAddedEventAsync(chainId, log);
        }
        else if (logSignature == LiquidityRemovedEventSignature)
        {
            await HandleLiquidityRemovedEventAsync(chainId, log);
        }
    }
    
    private async Task HandleEventAsync(
        string chainId,
        FilterLog log,
        string logType,
        Func<PoolLiquidityInfoInput, Task> poolLiquidityAction,
        Func<UserLiquidityInfoInput, Task> userLiquidityAction = null)
    {
        // Parse common fields
        var address = ParseAddress(log.Topics[1].ToString());
        var tokenAddress = ParseAddress(log.Topics[2].ToString());
        var amount = ParseAmount(log.Topics[3].ToString());

        Log.ForContext("chainId", chainId).Debug(
            "Handle {logType} event {chainId}, address1 {address1}, token {token}, amount {amount}",
            logType, chainId, address, tokenAddress, amount);

        // Retrieve token details
        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = chainId,
            Address = tokenAddress
        });

        // Prepare liquidity input
        var liquidityAmount = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals));
        var poolLiquidityInfo = new PoolLiquidityInfoInput
        {
            ChainId = chainId,
            TokenId = token.Id,
            Liquidity = liquidityAmount
        };

        // Perform pool liquidity action
        await poolLiquidityAction(poolLiquidityInfo);

        // Optionally handle user liquidity
        if (userLiquidityAction != null)
        {
            var userLiquidityInfo = new UserLiquidityInfoInput
            {
                ChainId = chainId,
                Provider = address,
                TokenId = token.Id,
                Liquidity = liquidityAmount
            };
            await userLiquidityAction(userLiquidityInfo);
        }
    }

    private async Task HandleLockedEventAsync(string chainId, FilterLog log)
    {
        await HandleEventAsync(
            chainId,
            log,
            "locked",
            _poolLiquidityInfoAppService.AddLiquidityAsync
        );
    }

    private async Task HandleReleasedEventAsync(string chainId, FilterLog log)
    {
        await HandleEventAsync(
            chainId,
            log,
            "released",
            _poolLiquidityInfoAppService.RemoveLiquidityAsync
        );
    }

    private async Task HandleLiquidityAddedEventAsync(string chainId, FilterLog log)
    {
        await HandleEventAsync(
            chainId,
            log,
            "liquidity added",
            _poolLiquidityInfoAppService.AddLiquidityAsync,
            _userLiquidityInfoAppService.AddUserLiquidityAsync
        );
    }

    private async Task HandleLiquidityRemovedEventAsync(string chainId, FilterLog log)
    {
        await HandleEventAsync(
            chainId,
            log,
            "liquidity removed",
            _poolLiquidityInfoAppService.RemoveLiquidityAsync,
            _userLiquidityInfoAppService.RemoveUserLiquidityAsync
        );
    }

    // private async Task HandleLockedEventAsync(string chainId, FilterLog log)
    // {
    //     var sender = ParseAddress(log.Topics[1].ToString());
    //     var tokenAddress =ParseAddress(log.Topics[2].ToString());
    //     var amount = ParseAmount(log.Topics[3].ToString());
    //     Log.ForContext("chainId", chainId).Debug(
    //         "Handle locked event {chainId}, sender {from}, token {token}, amount {amount}",
    //         chainId, sender, tokenAddress, amount);
    //     // Handle locked event
    //     var token = await _tokenAppService.GetAsync(new GetTokenInput
    //     {
    //         ChainId = chainId,
    //         Address = tokenAddress
    //     });
    //     await _poolLiquidityInfoAppService.AddLiquidityAsync(new PoolLiquidityInfoInput
    //     {
    //         ChainId = chainId,
    //         TokenId = token.Id,
    //         Liquidity = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals))
    //     });
    // }
    //
    // private async Task HandleReleasedEventAsync(string chainId, FilterLog log)
    // {
    //     var sender = ParseAddress(log.Topics[1].ToString());
    //     var tokenAddress =ParseAddress(log.Topics[2].ToString());
    //     var amount = ParseAmount(log.Topics[3].ToString());
    //     Log.ForContext("chainId", chainId).Debug(
    //         "Handle release event {chainId}, sender {from}, token {token}, amount {amount}",
    //         chainId, sender, tokenAddress, amount);
    //     // Handle released event
    //     var token = await _tokenAppService.GetAsync(new GetTokenInput
    //     {
    //         ChainId = chainId,
    //         Address = tokenAddress
    //     });
    //     await _poolLiquidityInfoAppService.RemoveLiquidityAsync(new PoolLiquidityInfoInput
    //     {
    //         ChainId = chainId,
    //         TokenId = token.Id,
    //         Liquidity = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals))
    //     });
    // }
    //
    // private async Task HandleLiquidityAddedEventAsync(string chainId, FilterLog log)
    // {
    //     var provider = ParseAddress(log.Topics[1].ToString());
    //     var tokenAddress =ParseAddress(log.Topics[2].ToString());
    //     var amount = ParseAmount(log.Topics[3].ToString());
    //     Log.ForContext("chainId", chainId).Debug(
    //         "Handle liquidity added event {chainId}, provider {from}, token {token}, amount {amount}",
    //         chainId, provider, tokenAddress, amount);
    //     // Handle liquidity added event
    //     var token = await _tokenAppService.GetAsync(new GetTokenInput
    //     {
    //         ChainId = chainId,
    //         Address = tokenAddress
    //     });
    //     await _poolLiquidityInfoAppService.AddLiquidityAsync(new PoolLiquidityInfoInput
    //     {
    //         ChainId = chainId,
    //         TokenId = token.Id,
    //         Liquidity = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals))
    //     });
    //     await _userLiquidityInfoAppService.AddUserLiquidityAsync(new UserLiquidityInfoInput
    //     {
    //         ChainId = chainId,
    //         Provider = provider,
    //         TokenId = token.Id,
    //         Liquidity = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals))
    //     });
    // }
    //
    // private async Task HandleLiquidityRemovedEventAsync(string chainId, FilterLog log)
    // {
    //     var provider = ParseAddress(log.Topics[1].ToString());
    //     var tokenAddress =ParseAddress(log.Topics[2].ToString());
    //     var amount = ParseAmount(log.Topics[3].ToString());
    //     Log.ForContext("chainId", chainId).Debug(
    //         "Handle liquidity added event {chainId}, provider {from}, token {token}, amount {amount}",
    //         chainId, provider, tokenAddress, amount);
    //     // Handle liquidity removed event
    //     var token = await _tokenAppService.GetAsync(new GetTokenInput
    //     {
    //         ChainId = chainId,
    //         Address = tokenAddress
    //     });
    //     await _poolLiquidityInfoAppService.RemoveLiquidityAsync(new PoolLiquidityInfoInput
    //     {
    //         ChainId = chainId,
    //         TokenId = token.Id,
    //         Liquidity = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals))
    //     });
    //     await _userLiquidityInfoAppService.RemoveUserLiquidityAsync(new UserLiquidityInfoInput
    //     {
    //         ChainId = chainId,
    //         Provider = provider,
    //         TokenId = token.Id,
    //         Liquidity = (decimal)((BigDecimal)amount / BigInteger.Pow(10, token.Decimals))
    //     });
    // }

    public static string ParseAddress(string topic)
    {
        return "0x" + topic.Substring(topic.Length - 40);
    }

    public static BigInteger ParseAmount(string topic)
    {
        return BigInteger.Parse(topic.Substring(2), System.Globalization.NumberStyles.HexNumber);
    }
}