using System.Collections.Generic;
using System.Numerics;
using TronNet.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Contracts.Bridge.TronDto;

[FunctionOutput]
public class GetReceivedReceiptInfosDto: IFunctionOutputDTO
{
    [Parameter("tuple[]", "_receipts", 1)]
    public List<ReceivedReceiptDto> Receipts { get; set; }
}

[FunctionOutput]
public class ReceivedReceiptDto : IFunctionOutputDTO
{
    [Parameter("address", "asset", 1)]
    public string Asset { get; set; }
    [Parameter("address", "targetAddress", 2)]
    public string TargetAddress { get; set; }
    [Parameter("uint256", "amount", 3)]
    public BigInteger Amount { get; set; }
    [Parameter("uint256", "blockHeight", 4)]
    public BigInteger BlockHeight { get; set; }
    [Parameter("uint256", "blockTime", 5)]
    public BigInteger BlockTime { get; set; }
    [Parameter("string", "fromChainId", 6)]
    public string FromChainId { get; set; }
    [Parameter("string", "receiptId", 7)]
    public string ReceiptId { get; set; }
}