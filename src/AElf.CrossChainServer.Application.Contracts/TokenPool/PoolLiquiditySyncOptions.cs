using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenPool;

public class PoolLiquiditySyncOptions
{
    public bool IsSyncEnabled { get; set; }
    public Dictionary<string,List<string>> Token { get; set; }
}