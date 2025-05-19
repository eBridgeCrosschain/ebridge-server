using System.Collections.Generic;
using AElf.CrossChainServer.BridgeContract;

namespace AElf.CrossChainServer.Worker;

public class BridgeContractSyncOptions
{
    /// <summary>
    /// ChainId -> Token Dic
    /// </summary>
    public int SyncDelayHeight { get; set; } = 10;
    public string ConfirmedSyncKeyPrefix { get; set; } = "Confirmed";
    public int ConfirmedSyncDelayHeight { get; set; } = 20;
    
}