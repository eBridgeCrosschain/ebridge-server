using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync.Dtos.Limit;

[Event("TokenDailyLimitConsumed")]
//     event TokenDailyLimitConsumed(bytes32 dailyLimitId, address tokenAddress, uint256 amount);
public class TokenDailyLimitConsumedEvent : IEventDTO
{
    [Parameter("bytes32", "dailyLimitId", 1, false)]
    public byte[] DailyLimitId { get; set; }
    [Parameter("address", "tokenAddress", 2, false)]
    public string TokenAddress { get; set; }
    [Parameter("uint256", "amount", 3, false)]
    public BigInteger Amount { get; set; }
}