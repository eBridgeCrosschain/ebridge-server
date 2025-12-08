using System.Collections.Generic;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainRateLimitsDto
{
    public string FromChain { get; set; }
    public string ToChain { get; set; }
    public List<RateLimitInfo> ReceiptRateLimitsInfo { get; set; }
    public List<RateLimitInfo> SwapRateLimitsInfo { get; set; }
}

public class RateLimitInfo
{
    public string Token { get; set; }
    public decimal Capacity { get; set; }
    public decimal RefillRate { get; set; }
    
    public int MaximumTimeConsumed { get; set; }
}


