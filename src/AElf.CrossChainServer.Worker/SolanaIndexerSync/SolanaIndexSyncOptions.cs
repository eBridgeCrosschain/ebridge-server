using System.Collections.Generic;

namespace AElf.CrossChainServer.Worker;

public class SolanaIndexSyncOptions
{
    public bool IsEnable { get; set; } = true;
    public Dictionary<string, ContractAddress> ContractAddress { get; set; } = new();
    public int SyncPeriod { get; set; } = 30 * 1000;
    public int QueryDelayTime { get; set; } = 1000;
}

public class ContractAddress
{
    public string BridgeContract { get; set; }
    public string TokenPoolContract { get; set; }
}