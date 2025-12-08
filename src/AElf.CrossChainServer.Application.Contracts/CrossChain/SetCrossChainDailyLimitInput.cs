using System;

namespace AElf.CrossChainServer.CrossChain;

public class SetCrossChainDailyLimitInput
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public string TargetChainId { get; set; }
    public CrossChainLimitType Type { get; set; }
    public decimal RemainAmount { get; set; }
    public long RefreshTime { get; set; }
    public decimal DailyLimit { get; set; }
    public Guid TokenId { get; set; }
}