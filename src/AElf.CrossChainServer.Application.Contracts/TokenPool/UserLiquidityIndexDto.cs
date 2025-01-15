using System;
using AElf.CrossChainServer.Tokens;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenPool;

public class UserLiquidityIndexDto : EntityDto<Guid>
{
    public string Provider { get; set; }
    public string ChainId { get; set; }
    public TokenDto TokenInfo { get; set; }
    public decimal Liquidity { get; set; }
}