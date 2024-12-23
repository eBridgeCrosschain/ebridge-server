using JetBrains.Annotations;

namespace AElf.CrossChainServer.TokenAccess;

public class PoolOverviewDto
{
    public decimal TotalTvlInUsd { get; set; }
    public decimal? MyTotalTvlInUsd { get; set; }
    public long PoolCount { get; set; }
    public long TokenCount { get; set; }
}