using Nethereum.Contracts;
using TronNet.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Chains.TronFunctionMessage;

[Nethereum.ABI.FunctionEncoding.Attributes.Function("symbol", "tuple[]")]
public class SymbolFunctionMessage: FunctionMessage
{
}
        
[TronNet.ABI.FunctionEncoding.Attributes.FunctionOutput]
public class SymbolDto : IFunctionOutputDTO
{
    [TronNet.ABI.FunctionEncoding.Attributes.Parameter("string", 1)]
    public string Symbol { get; set; }
}