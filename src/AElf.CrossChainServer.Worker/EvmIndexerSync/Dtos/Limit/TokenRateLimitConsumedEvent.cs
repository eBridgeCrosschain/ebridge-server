using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync.Dtos.Limit;
//     event TokenRateLimitConsumed(bytes32 bucketId, address tokenAddress, uint256 amount);
[Event("TokenRateLimitConsumed")]
public class TokenRateLimitConsumedEvent : IEventDTO
{
    [Parameter("bytes32", "bucketId", 1, false)]
    public byte[] BucketId { get; set; }
    [Parameter("address", "tokenAddress", 2, false)]
    public string TokenAddress { get; set; }
    [Parameter("uint256", "amount", 3, false)]
    public BigInteger Amount { get; set; }
}