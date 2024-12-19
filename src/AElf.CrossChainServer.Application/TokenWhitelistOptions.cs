using System.Collections.Generic;

namespace AElf.CrossChainServer;

public class TokenWhitelistOptions
{
    /// <summary>
    /// token -> chainId -> tokenInfo
    /// </summary>
    public Dictionary<string, Dictionary<string, TokenInfo>> TokenWhitelist { get; set; } = new();
}

public class TokenInfo
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public long Decimals { get; set; }
    public string Address { get; set; }
    public string IssueChainId { get; set; }
}