using System;

namespace AElf.CrossChainServer.TokenPool;

public class UserLiquidityInfo : LiquidityBase
{
    public Guid TokenId { get; set; }
    public string Provider { get; set; }
}