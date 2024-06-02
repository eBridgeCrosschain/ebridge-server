using System.Collections.Generic;
using System.Numerics;
using TronNet.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Contracts.Bridge.TronDto;

[FunctionOutput]
public class GetReceiptInfosDto: IFunctionOutputDTO
{
    [Parameter("tuple[]", "_receipts", 1)]
    public List<ReceiptDto> Receipts { get; set; }
}

[FunctionOutput]
public class ReceiptDto : IFunctionOutputDTO
{
    [Parameter("address", "asset", 1)]
    public string Asset { get; set; }
    [Parameter("address", "owner", 2)]
    public string Owner { get; set; }
    [Parameter("uint256", "amount", 3)]
    public BigInteger Amount { get; set; }
    [Parameter("uint256", "blockHeight", 4)]
    public BigInteger BlockHeight { get; set; }
    [Parameter("uint256", "blockTime", 5)]
    public BigInteger BlockTime { get; set; }
    [Parameter("string", "targetChainId", 6)]
    public string TargetChainId { get; set; }
    [Parameter("string", "targetAddress", 7)]
    public string TargetAddress { get; set; }
    [Parameter("string", "receiptId", 8)]
    public string ReceiptId { get; set; }
}