using System.Collections.Generic;
using AElf.CrossChainServer.BridgeContract;

namespace AElf.CrossChainServer.CrossChain;

public class EvmTokensOptions
{
    /// <summary>
    /// ChainId -> Token Dic
    /// </summary>
    public Dictionary<string, List<TokenInfo>> Tokens { get; set; } = new();
}

public class TokenInfo
{
    public string Address { get; set; }
    public string TargetChainId { get; set; }
}