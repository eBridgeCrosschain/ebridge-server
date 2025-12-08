using System.Collections.Generic;

namespace AElf.CrossChainServer.Tokens;

public class TokenSymbolMappingOptions
{
    public Dictionary<string, Dictionary<string, Dictionary<string, string>>> Mapping { get; set; }
}