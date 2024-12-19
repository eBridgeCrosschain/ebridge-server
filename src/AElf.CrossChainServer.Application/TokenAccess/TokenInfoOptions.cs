using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenInfoOptions : Dictionary<string, SupportChainInfo>
{
}

public class SupportChainInfo
{
    public List<string> Deposit { get; set; } = new();
    public List<string> Withdraw { get; set; } = new();
    public List<string> Transfer { get; set; } = new();
}