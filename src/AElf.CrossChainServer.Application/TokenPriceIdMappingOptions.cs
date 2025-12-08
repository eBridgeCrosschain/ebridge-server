using System.Collections.Generic;

namespace AElf.CrossChainServer;

public class TokenPriceIdMappingOptions
{
    public Dictionary<string,string> CoinIdMapping { get; set; } = new();
}