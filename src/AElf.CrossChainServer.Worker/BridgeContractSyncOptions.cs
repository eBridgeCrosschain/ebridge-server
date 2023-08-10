using System.Collections.Generic;
using AElf.CrossChainServer.BridgeContract;

namespace AElf.CrossChainServer.Worker;

public class BridgeContractSyncOptions
{
    /// <summary>
    /// ChainId -> Token Dic
    /// </summary>
    public Dictionary<string, Dictionary<TransferType, List<TokenInfo>>> Tokens { get; set; } = new();
    
    public int SyncDelayHeight { get; set; } = 50;
    public string ConfirmedSyncKeyPrefix { get; set; } = "Confirmed";
    public int ConfirmedSyncDelayHeight { get; set; } = 2000;
}

public class TokenInfo
{
    public string Address { get; set; }
    public string Symbol { get; set; }
    public string TargetChainId { get; set; }
}