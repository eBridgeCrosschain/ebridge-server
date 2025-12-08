using System;
using AElf.CrossChainServer.Entities;
using AElf.CrossChainServer.Tokens;
using Nest;

namespace AElf.CrossChainServer.TokenPool;

public class LiquidityBase : CrossChainServerEntity<Guid>
{
    [Keyword] public string ChainId { get; set; }
    public decimal Liquidity { get; set; }
}