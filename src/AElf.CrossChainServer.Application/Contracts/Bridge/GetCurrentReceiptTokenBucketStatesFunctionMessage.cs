using System.Collections.Generic;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System.Numerics;

namespace AElf.CrossChainServer.Contracts.Bridge;

[Function("getCurrentReceiptTokenBucketStates", "tuple[]")]
public class GetCurrentReceiptTokenBucketStatesFunctionMessage : FunctionMessage
{
    [Parameter("address[]", "tokens", 1)] public List<string> Token { get; set; }

    [Parameter("string[]", "targetChainIds", 2)]
    public List<string> TargetChainId { get; set; }
}

[FunctionOutput]
public class ReceiptTokenBucketsDto : IFunctionOutputDTO
{
    [Parameter("tuple[]", "_tokenBuckets", 1)]
    public List<ReceiptTokenBucketDto> TokenBuckets { get; set; }
}

[FunctionOutput]
public class ReceiptTokenBucketDto : IFunctionOutputDTO
{
    [Parameter("uint128", "currentTokenAmount", 1)]
    public BigInteger CurrentTokenAmount { get; set; }

    [Parameter("uint32", "lastUpdatedTime", 2)]
    public long LastUpdatedTime { get; set; }

    [Parameter("bool", "isEnabled", 3)] 
    public bool IsEnabled { get; set; }

    [Parameter("uint128", "tokenCapacity", 4)]
    public BigInteger TokenCapacity { get; set; }

    [Parameter("uint128", "rate", 5)]
    public BigInteger Rate { get; set; }
}