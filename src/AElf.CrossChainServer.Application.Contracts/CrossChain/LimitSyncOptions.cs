using System.Collections.Generic;

namespace AElf.CrossChainServer.CrossChain;

public class LimitSyncOptions
{
    public bool IsSyncEnabled { get; set; }
    public Dictionary<string,List<LimitInfo>> LimitInfos { get; set; }
}

public class LimitInfo
{
    public string TokenAddress { get; set; }
    public string TargetChainId { get; set; }
    public string SwapId { get; set; }
}