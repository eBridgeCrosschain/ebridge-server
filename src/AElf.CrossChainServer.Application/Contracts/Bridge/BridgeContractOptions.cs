using System.Collections.Generic;

namespace AElf.CrossChainServer.Contracts.Bridge;

public class BridgeContractOptions
{
    public Dictionary<string,BridgeContractAddress> ContractAddresses { get; set; }
}

public class BridgeContractAddress
{
    public string BridgeInContract { get; set; }
    public string BridgeOutContract { get; set; }

    public string LimiterContract { get; set; }
    public string TokenPoolContract { get; set; }

}