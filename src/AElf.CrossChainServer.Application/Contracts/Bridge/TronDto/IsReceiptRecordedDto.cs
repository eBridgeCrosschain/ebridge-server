using TronNet.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Contracts.Bridge.TronDto;

[FunctionOutput]
public class IsReceiptRecordedDto: IFunctionOutputDTO
{
    [Parameter("bool", "", 1)]
    public bool IsReceiptRecorded { get; set; }
}