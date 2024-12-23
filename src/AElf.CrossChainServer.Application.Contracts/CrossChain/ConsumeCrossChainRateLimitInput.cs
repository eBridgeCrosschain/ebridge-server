using System;

namespace AElf.CrossChainServer.CrossChain;

public class ConsumeCrossChainRateLimitInput
{
    public string ChainId { get; set; }
    public string TargetChainId { get; set; }
    public CrossChainLimitType Type { get; set; }
    public decimal Amount { get; set; }
    public Guid TokenId { get; set; }
}