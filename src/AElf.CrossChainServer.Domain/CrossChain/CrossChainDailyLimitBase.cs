using System;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainDailyLimitBase : MultiChainEntity<Guid>
{
    [Keyword]
    public string TargetChainId { get; set; }
    public CrossChainLimitType Type { get; set; }
    public decimal RemainAmount { get; set; }
    public long RefreshTime { get; set; }
    public decimal DailyLimit { get; set; }
}