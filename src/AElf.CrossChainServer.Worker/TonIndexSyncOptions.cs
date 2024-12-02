using System.Collections.Generic;

namespace AElf.CrossChainServer.Worker;

public class TonIndexSyncOptions
{
    // ChainId -> ContractAddress
    // public Dictionary<string, List<string>> ContractAddress { get; set; } = new();
    public Dictionary<string, ContractInfo> ContractAddress { get; set; } = new();
    public int SyncPeriod { get; set; } = 10 * 1000; // 10s
    public int QueryDelayTime { get; set; } = 1000; // 1s 
}

public class ContractInfo
{
    public string BridgeContract { get; set; }
    public List<BridgePoolContractInfo> BridgePoolContract { get; set; }
}

public class BridgePoolContractInfo
{
    public string TokenAddress { get; set; }
    public string PoolAddress { get; set; }
}
