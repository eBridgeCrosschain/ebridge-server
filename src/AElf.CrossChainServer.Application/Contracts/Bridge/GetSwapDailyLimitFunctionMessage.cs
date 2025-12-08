using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace AElf.CrossChainServer.Contracts.Bridge;
[Function("getSwapDailyLimit", "tuple")]
public class GetSwapDailyLimitFunctionMessage : FunctionMessage
{
    [Parameter("bytes32", "swapId", 1)]
    public byte[] SwapId { get; set; }
}

[FunctionOutput]
public class SwapDailyLimitDto : IFunctionOutputDTO
{
    [Parameter("uint256", "tokenAmount", 1)]
    public BigInteger CurrentTokenAmount { get; set; }
    [Parameter("uint32", "refreshTime", 2)]
    public long RefreshTime { get; set; }
    [Parameter("uint256", "defaultTokenAmount", 3)]
    public BigInteger DailyLimit { get; set; }
}