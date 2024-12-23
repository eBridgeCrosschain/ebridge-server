using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenWhitelistDto
{
    public Dictionary<string, Dictionary<string, TokenInfoDto>> Data { get; set; } = new();
}

public class TokenInfoDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public long Decimals { get; set; }
    public string Address { get; set; }
    public string IssueChainId { get; set; }
}