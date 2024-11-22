using System;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainTransferInput
{
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public string TransferTransactionId { get; set; }
    public DateTime TransferTime { get; set; }
    public long TransferBlockHeight { get; set; }
    public decimal TransferAmount { get; set; }
    public Guid TransferTokenId { get; set; }
    public string ReceiptId { get; set; }
    // For ton trace
    public string TraceId { get; set; }
}