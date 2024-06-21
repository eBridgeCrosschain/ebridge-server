using System;

namespace AElf.CrossChainServer.CrossChain;

public class UpdateReportInfoIndexInput
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public long RoundId { get; set; }
    public string Token { get; set; }
    public string TargetChainId { get; set; }
    public string ReceiptId { get; set; }
    public string ReceiptHash { get; set; }
    public ReportStep Step { get; set; }
    public int QueryTimes { get; set; }
    public long TransmitHeight { get; set; }
    public long LastUpdateHeight { get; set; }
    public string QueryTransactionId { get; set; }
    public string ReceiptInfo { get; set; }
}