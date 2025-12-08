using System.Collections.Generic;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace AElf.CrossChainServer.Contracts.Bridge;

[Function("getReceiptDailyLimit", "tuple")]
public class GetDailyLimitFunctionMessage : FunctionMessage
{
    [Parameter("address", "token", 1)]
    public string Token { get; set; }
    
    [Parameter("string", "targetChainId", 2)]
    public string TargetChainId { get; set; }
}

[FunctionOutput]
public class ReceiptDailyLimitDto : IFunctionOutputDTO
{
    [Parameter("uint256", "tokenAmount", 1)]
    public BigInteger CurrentTokenAmount { get; set; }
    [Parameter("uint32", "refreshTime", 2)]
    public long RefreshTime { get; set; }
    [Parameter("uint256", "defaultTokenAmount", 3)]
    public BigInteger DailyLimit { get; set; }
}