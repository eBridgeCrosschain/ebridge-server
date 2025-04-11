using System.Collections.Generic;
using Nethereum.Contracts;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public class EvmContractSyncOptions
{
    public bool Enabled { get; set; } = true;
    public Dictionary<string, Contract> ContractAddresses { get; set; }
    public int SyncPeriod { get; set; } = 60 * 1000; // 1min

}

public class Contract
{
    public string TokenPoolContract { get; set; }
    public string BridgeInContract { get; set; } = "";
    public string BridgeOutContract { get; set; } = "";
}