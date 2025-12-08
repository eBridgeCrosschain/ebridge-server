using System.Collections.Generic;
using Nethereum.Hex.HexTypes;

namespace AElf.CrossChainServer.Chains;

public class FilterLogsDto
{
    public List<FilterLog> Logs { get; set; }
}

public class FilterLog
{
    public bool Removed { get; set; }
    public string Type { get; set; }
    public long LogIndex { get; set; }
    public string TransactionHash { get; set; }
    public long TransactionIndex { get; set; }
    public string BlockHash { get; set; }
    public long BlockNumber { get; set; }
    public string Address { get; set; }
    public string Data { get; set; }
    public object[] Topics { get; set; }
}
