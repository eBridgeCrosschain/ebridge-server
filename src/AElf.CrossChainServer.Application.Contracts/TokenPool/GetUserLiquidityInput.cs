using System;
using JetBrains.Annotations;

namespace AElf.CrossChainServer.TokenPool;

public class GetUserLiquidityInput
{
    public string Provider { get; set; }
    [CanBeNull] public string ChainId { get; set; }
    [CanBeNull] public string Token { get; set; }
}