using System;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainRateLimitBase : MultiChainEntity<Guid>
{
    [Keyword]
    public string TargetChainId { get; set; }
    public CrossChainLimitType Type { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Capacity { get; set; }
    public decimal Rate { get; set; }
    public bool Enable { get; set; }
}