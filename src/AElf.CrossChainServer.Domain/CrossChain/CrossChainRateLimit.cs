using System;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainRateLimit : CrossChainRateLimitBase
{
    public Guid TokenId { get; set; }
}