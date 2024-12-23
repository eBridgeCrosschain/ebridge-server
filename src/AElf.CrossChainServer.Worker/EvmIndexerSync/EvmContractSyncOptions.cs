using System.Collections.Generic;
using Nethereum.Contracts;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public class EvmContractSyncOptions
{
    public Dictionary<string, Contract> ContractAddresses { get; set; }
}

public class Contract
{
    public string TokenPoolContract { get; set; }
}