using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Worker.EvmIndexerSync.Dtos.Limit;

[Event("DailyLimitSet")]
//     event DailyLimitSet(bytes32 dailyLimitId, uint32 refreshTime, uint256 dailyLimit, uint256 remainTokenAmount);
public class DailyLimitSetEvent : IEventDTO
{
    [Parameter("bytes32", "dailyLimitId", 1, false)]
    public byte[] DailyLimitId { get; set; }

    [Parameter("uint32", "refreshTime", 2, false)]
    public long RefreshTime { get; set; }

    [Parameter("uint256", "dailyLimit", 3, false)]
    public BigInteger DailyLimit { get; set; }

    [Parameter("uint256", "remainTokenAmount", 4, false)]
    public BigInteger RemainTokenAmount { get; set; }
}