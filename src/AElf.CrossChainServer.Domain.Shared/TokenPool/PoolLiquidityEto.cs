using System;

namespace AElf.CrossChainServer.TokenPool;

public class PoolLiquidityEto
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public decimal Liquidity { get; set; }
    public Guid TokenId { get; set; }
}