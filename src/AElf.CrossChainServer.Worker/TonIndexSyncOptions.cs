using System.Collections.Generic;

namespace AElf.CrossChainServer.Worker;

public class TonIndexSyncOptions
{
    // ChainId -> ContractAddress
    public Dictionary<string, List<string>> ContractAddress { get; set; } = new();
}