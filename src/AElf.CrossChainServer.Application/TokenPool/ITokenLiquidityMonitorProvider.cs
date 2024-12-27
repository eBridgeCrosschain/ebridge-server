using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Notify;
using AElf.CrossChainServer.TokenAccess;
using Microsoft.Extensions.Options;

namespace AElf.CrossChainServer.TokenPool;

public interface ITokenLiquidityMonitorProvider
{
    Task MonitorTokenLiquidityAsync();
}

public class TokenLiquidityMonitorProvider : ITokenLiquidityMonitorProvider
{
    private readonly ILarkRobotNotifyProvider _larkRobotNotifyProvider;
    private const string LiquidityInsufficientAlarm = "LiquidityInsufficientAlarm";
    private readonly TokenAccessOptions _tokenAccessOptions;

    public TokenLiquidityMonitorProvider(ILarkRobotNotifyProvider larkRobotNotifyProvider, IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions)
    {
        _larkRobotNotifyProvider = larkRobotNotifyProvider;
        _tokenAccessOptions = tokenAccessOptions.Value;
    }
    private async Task SendLarkNotifyAsync(LiquidityDto dto)
    {
        await _larkRobotNotifyProvider.SendMessageAsync(new NotifyRequest
        {
            Template = LiquidityInsufficientAlarm,
            Params = new Dictionary<string, string>
            {
                [LiquidityKeys.Token] = dto.Token,
                [LiquidityKeys.LiquidityInUsd] = dto.LiquidityInUsd,
                [LiquidityKeys.Chain] = dto.Chain,
                [LiquidityKeys.PersonName] = dto.PersonName,
                [LiquidityKeys.TelegramHandler] = dto.TelegramHandler,
                [LiquidityKeys.Email] = dto.Email
            }
        });
    }

    // public Task MonitorTokenLiquidityAsync(string chainId,decimal poolLiquidity)
    // {
    //     if (poolLiquidity <= (!_tokenAccessOptions.Value.PoolConfig.ContainsKey(item.Coin)
    //             ? _tokenAccessOptions.Value.DefaultPoolConfig.Liquidity.SafeToDecimal()
    //             : _tokenAccessOptions.Value.PoolConfig[item.Coin].Liquidity.SafeToDecimal()))
    //     throw new System.NotImplementedException();
    // }
    
    private static class LiquidityKeys
    {
        public const string Token = "token";
        public const string LiquidityInUsd = "liquidityInUsd";
        public const string Chain = "chain";
        public const string PersonName = "personName";
        public const string TelegramHandler = "telegramHandler";
        public const string Email = "email";
    }

    public Task MonitorTokenLiquidityAsync()
    {
        throw new System.NotImplementedException();
    }
}


public class LiquidityDto
{
    public string Token { get; set; }
    public string LiquidityInUsd { get; set; }
    public string Chain { get; set; }
    public string PersonName { get; set; }
    public string TelegramHandler { get; set; }
    public string Email { get; set; }
}
