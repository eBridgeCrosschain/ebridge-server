using System;

namespace AElf.CrossChainServer.TokenPool;

public class PoolLiquidityInfoInput
{
    public string ChainId { get; set; }
    public Guid TokenId { get; set; }
    public decimal Liquidity { get; set; }
}