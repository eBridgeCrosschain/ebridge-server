using System;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainReceiveInput
{
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public string TransferTransactionId { get; set; }
    public string ReceiveTransactionId { get; set; }
    public DateTime ReceiveTime { get; set; }
    public string ReceiptId { get; set; }
    public Guid ReceiveTokenId { get; set; }
    public decimal ReceiveAmount { get; set; }
    public string TraceId { get; set; }
    public ReceiptStatus ReceiveStatus { get; set; }
    public long ReceiveBlockHeight { get; set; }
}