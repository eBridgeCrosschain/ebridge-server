using System.Collections.Generic;

namespace AElf.CrossChainServer.Worker;

public class TonIndexSyncOptions
{
    // ChainId -> ContractAddress
    public Dictionary<string, List<string>> ContractAddress { get; set; } = new();
    public int SyncPeriod { get; set; } = 10 * 1000; // 10s
    public int QueryDelayTime { get; set; } = 1000; // 1s 
}