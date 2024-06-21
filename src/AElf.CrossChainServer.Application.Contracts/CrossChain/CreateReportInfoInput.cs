using System;

namespace AElf.CrossChainServer.CrossChain;

public class CreateReportInfoInput
{
    public string ChainId { get; set; }
    public long RoundId { get; set; }
    public string Token { get; set; }
    public string TargetChainId { get; set; }
    public string ReceiptId { get; set; }
    public string ReceiptHash { get; set; }
    public long LastUpdateHeight { get; set; }
    public string ReceiptInfo { get; set; }

}