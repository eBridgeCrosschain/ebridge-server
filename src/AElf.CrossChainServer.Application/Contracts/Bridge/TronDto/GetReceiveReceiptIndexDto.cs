using System.Collections.Generic;
using System.Numerics;
using TronNet.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Contracts.Bridge.TronDto;

[FunctionOutput]
public class GetReceiveReceiptIndexDto: IFunctionOutputDTO
{
    [Parameter("uint256[]", "indexes", 1)]
    public List<BigInteger> Indexes { get; set; }
}