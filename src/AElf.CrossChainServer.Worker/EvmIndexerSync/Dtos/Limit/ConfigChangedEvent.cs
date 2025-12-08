using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync.Dtos.Limit;
[Event("ConfigChanged")]
// event ConfigChanged(bytes32 bucketId, bool isEnabled, uint128 tokenCapacity, uint128 rate,uint128 currentTokenAmount);
public class ConfigChangedEvent : IEventDTO
{
    [Parameter("bytes32", "bucketId", 1, false)]
    public byte[] BucketId { get; set; }

    [Parameter("bool", "isEnabled", 2, false)]
    public bool IsEnabled { get; set; }

    [Parameter("uint128", "tokenCapacity", 3, false)]
    public BigInteger TokenCapacity { get; set; }

    [Parameter("uint128", "rate", 4, false)]
    public BigInteger Rate { get; set; }
    
    [Parameter("uint128", "currentTokenAmount", 5, false)]
    public BigInteger CurrentTokenAmount { get; set; }
}