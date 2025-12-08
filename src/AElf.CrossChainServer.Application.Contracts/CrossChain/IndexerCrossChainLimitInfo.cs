using System;
using System.Collections.Generic;

namespace AElf.CrossChainServer.CrossChain;

public class IndexerCrossChainLimitInfos : IndexerCommonResult<IndexerCrossChainLimitInfos>
{
    public long TotalRecordCount { get; set; }
    public List<IndexerCrossChainLimitInfo> DataList { get; set; }
}

public class IndexerCrossChainLimitInfo : IndexerCommonResult<IndexerCrossChainLimitInfo>
{
    public string Id { get; set; }

    public string FromChainId { get; set; }

    public string ToChainId { get; set; }

    public string Symbol { get; set; }
    
    public CrossChainLimitType LimitType { get; set; }
    
    // Default Daily Limit
    public long DefaultDailyLimit { get; set; }

    // Refresh Time
    public DateTime RefreshTime { get; set; }

    // Current Daily Limit
    public long CurrentDailyLimit { get; set; }

    // Token Bucket Capacity
    public long Capacity { get; set; }

    // Refill Rate
    public long RefillRate { get; set; }

    // IsEnable
    public bool IsEnable { get; set; }

    // Current Bucket TokenAmount
    public long CurrentBucketTokenAmount { get; set; }

    // Bucket Update Time
    public DateTime BucketUpdateTime { get; set; }
}