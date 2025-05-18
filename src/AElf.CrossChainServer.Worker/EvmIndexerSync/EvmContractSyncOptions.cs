using System.Collections.Generic;
using Nethereum.Contracts;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public class EvmContractSyncOptions
{
    public bool Enabled { get; set; } = true;
    public Dictionary<string, IndexerInfo> IndexerInfos { get; set; }
    public int SyncPeriod { get; set; } = 3 * 1000; // 1min

}

public class IndexerInfo
{
    public string WsUrl { get; set; }
    public string TokenPoolContract { get; set; } = "";
    public string BridgeInContract { get; set; } = "";
    public string BridgeOutContract { get; set; } = "";
    public string LimiterContract { get; set; } = "";
    public int PingDelay { get; set; } = 5000;

}