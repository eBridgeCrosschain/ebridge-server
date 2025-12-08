using System.Collections.Generic;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainLimitsOptions
{
    private const int DefaultWeightValue = 999;
    public ChainIdInfo ChainIdInfo { get; set; }
    //key is fromChainId-toChainId, value is Weight
    public Dictionary<string, int> ChainSortRules { get; set; } = new Dictionary<string, int>();
    //key is token, value is Weight
    public Dictionary<string, int> TokenSortRules { get; set; } = new Dictionary<string, int>();

    public int GetChainSortWeight(string fromChainId, string toChainId)
    {
        string key = fromChainId + "-" + toChainId;
        if (!ChainSortRules.TryGetValue(key, out var value))
        {
            value = DefaultWeightValue;
        }
        return value;
    }
    
    public int GetTokenSortWeight(string token)
    {
        if (!TokenSortRules.TryGetValue(token, out var value))
        {
            value = DefaultWeightValue;
        }
        return value;
    }
}

public class ChainIdInfo
{
    //ChainId with high priority(if it has repeated ChainId)
    public string TokenFirstChainId { get; set; }
    
    public List<string> ToChainIds { get; set; }
}
