using System;

namespace AElf.CrossChainServer.TokenPool;

public class UserLiquidityInfoInput
{
    public string ChainId { get; set; }
    public Guid TokenId { get; set; }
    public string Provider { get; set; }
    public decimal Liquidity { get; set; }
}