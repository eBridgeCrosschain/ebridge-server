using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AElf.CrossChainServer.Chains.Ton;

public class TonAddressBooks
{
    // [JsonExtensionData]
    public Dictionary<string, ResponseItem> AdditionalProperties { get; set; }
}

public class ResponseItem
{
    [JsonPropertyName("domain")]
    public string Domain { get; set; }

    [JsonPropertyName("user_friendly")]
    public string UserFriendly { get; set; }
}