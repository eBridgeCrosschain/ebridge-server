using System.Collections.Generic;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync;

public class TokenLimitSwapInfoOptions
{
    public Dictionary<string, SwapTokenInfo> SwapTokenInfos { get; set; } = new();
}

public class SwapTokenInfo
{
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string TokenAddress { get; set; }
    public int LimitType { get; set; } // Receipt:0,Swap:1
}