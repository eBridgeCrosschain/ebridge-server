using System.Numerics;
using Nethereum.Contracts;
using TronNet.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Chains.TronFunctionMessage;

[Nethereum.ABI.FunctionEncoding.Attributes.Function("decimals", "tuple[]")]
public class DecimalsFunctionMessage: FunctionMessage
{
}
        
[TronNet.ABI.FunctionEncoding.Attributes.FunctionOutput]
public class DecimalsDto : IFunctionOutputDTO
{
    [TronNet.ABI.FunctionEncoding.Attributes.Parameter("uint256", 1)]
    public BigInteger Decimals { get; set; }
}
