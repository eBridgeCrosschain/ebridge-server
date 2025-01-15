using System;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainRateLimitEto
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public string TargetChainId { get; set; }
    public CrossChainLimitType Type { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Capacity { get; set; }
    public decimal Rate { get; set; }
    public bool IsEnable { get; set; }
    public Guid TokenId { get; set; }
}