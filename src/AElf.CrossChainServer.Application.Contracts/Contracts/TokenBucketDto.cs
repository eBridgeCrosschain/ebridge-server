namespace AElf.CrossChainServer.Contracts;

public class TokenBucketDto
{
    public decimal Capacity { get; set; }
    public decimal RefillRate { get; set; }
    public int MaximumTimeConsumed { get; set; }
    public decimal CurrentTokenAmount { get; set; }
    public bool IsEnabled { get; set; }
    public long LastUpdatedTime { get; set; }
}