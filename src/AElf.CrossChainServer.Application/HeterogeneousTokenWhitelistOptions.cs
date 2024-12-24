using System.Collections.Generic;

namespace AElf.CrossChainServer;

public class HeterogeneousTokenWhitelistOptions
{
    public List<string> Tokens { get; set; }
    public Dictionary<string,List<string>> Chains { get; set; }
}