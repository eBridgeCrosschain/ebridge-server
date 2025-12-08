using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class ChainIdMapOptions
{
    public Dictionary<string, string> Chain { get; set; } = new();
}