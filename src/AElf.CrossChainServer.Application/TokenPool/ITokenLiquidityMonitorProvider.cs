using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Notify;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.Options;

namespace AElf.CrossChainServer.TokenPool;

public interface ITokenLiquidityMonitorProvider
{
    Task MonitorTokenLiquidityAsync(string chainId, Guid tokenId, decimal poolLiquidity);
}

public class TokenLiquidityMonitorProvider : ITokenLiquidityMonitorProvider
{
    private readonly ILarkRobotNotifyProvider _larkRobotNotifyProvider;
    private const string LiquidityInsufficientAlarm = "LiquidityInsufficientAlarm";
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly ITokenApplyOrderRepository _tokenApplyOrderRepository;
    private readonly IUserAccessTokenInfoRepository _userAccessTokenInfoRepository;
    private readonly IAggregatePriceProvider _aggregatePriceProvider;
    private readonly ITokenAppService _tokenAppService;

    public TokenLiquidityMonitorProvider(ILarkRobotNotifyProvider larkRobotNotifyProvider,
        IOptionsSnapshot<TokenAccessOptions> tokenAccessOptions, IAggregatePriceProvider aggregatePriceProvider,
        ITokenApplyOrderRepository tokenApplyOrderRepository,
        IUserAccessTokenInfoRepository userAccessTokenInfoRepository, ITokenAppService tokenAppService)
    {
        _larkRobotNotifyProvider = larkRobotNotifyProvider;
        _aggregatePriceProvider = aggregatePriceProvider;
        _tokenApplyOrderRepository = tokenApplyOrderRepository;
        _userAccessTokenInfoRepository = userAccessTokenInfoRepository;
        _tokenAppService = tokenAppService;
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

    public async Task MonitorTokenLiquidityAsync(string chainId, Guid tokenId, decimal poolLiquidity)
    {
        var token = await _tokenAppService.GetAsync(tokenId);
        var symbol = token.Symbol;
        decimal alarmThreshold = 0;
        alarmThreshold = _tokenAccessOptions.TokenConfig.TryGetValue(symbol, out var value)
            ? value.MinLiquidityInUsdForAlarm
            : _tokenAccessOptions.DefaultConfig.MinLiquidityInUsdForAlarm;

        var poolLiquidityInUsd = await _aggregatePriceProvider.GetPriceAsync(symbol);
        string personName = null;
        string telegramHandler = null;
        string email = null;

        var apply = await _tokenApplyOrderRepository.GetListAsync(p => p.Symbol == symbol);
        if (apply.Count > 0)
        {
            var address = apply.First().UserAddress;
            var userAccess =
                await _userAccessTokenInfoRepository.GetAsync(o => o.Address == address && o.Symbol == symbol);
            personName = userAccess?.PersonName;
            telegramHandler = userAccess?.TelegramHandler;
            email = userAccess?.Email;
        }

        if (poolLiquidityInUsd <= alarmThreshold)
        {
            await SendLarkNotifyAsync(new LiquidityDto
            {
                Token = symbol,
                LiquidityInUsd = poolLiquidityInUsd.ToString(),
                Chain = chainId,
                PersonName = personName,
                TelegramHandler = telegramHandler,
                Email = email
            });
        }
    }

    private static class LiquidityKeys
    {
        public const string Token = "token";
        public const string LiquidityInUsd = "liquidityInUsd";
        public const string Chain = "chain";
        public const string PersonName = "personName";
        public const string TelegramHandler = "telegramHandler";
        public const string Email = "email";
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