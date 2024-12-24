using System.Collections.Generic;
using Nethereum.Contracts;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public class EvmContractSyncOptions
{
    public Dictionary<string, Contract> ContractAddresses { get; set; }
    public int SyncPeriod { get; set; } = 60 * 1000; // 1min

}

public class Contract
{
    public string TokenPoolContract { get; set; }
}