using System;

namespace AElf.CrossChainServer.TokenPool;

public class UserLiquidityEto
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public decimal Liquidity { get; set; }
    public string Provider { get; set; }
    public Guid TokenId { get; set; }
}