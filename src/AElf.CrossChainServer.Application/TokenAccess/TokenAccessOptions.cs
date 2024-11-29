using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenAccessOptions
{
    public List<string> ChainIdList { get; set; } = new();
    public List<string> OtherChainIdList { get; set; } = new();
}