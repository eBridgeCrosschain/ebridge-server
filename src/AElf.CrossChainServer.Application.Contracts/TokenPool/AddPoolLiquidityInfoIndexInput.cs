using System;

namespace AElf.CrossChainServer.TokenPool;

public class AddPoolLiquidityInfoIndexInput
{
    public Guid Id { get; set; }
    public Guid TokenId { get; set; }
    public string ChainId { get; set; }
    public decimal Liquidity { get; set; }
}