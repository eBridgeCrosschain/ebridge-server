using System;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainTransferBase : CrossChainServerEntity<Guid>
{
    public CrossChainType Type { get; set; }
    [Keyword]
    public string FromChainId { get; set; }
    [Keyword]
    public string ToChainId { get; set; }
    [Keyword]
    public string FromAddress { get; set; }
    [Keyword]
    public string ToAddress { get; set; }
    [Keyword]
    public string TransferTransactionId { get; set; }
    [Keyword]
    public string ReceiveTransactionId { get; set; }
    [Keyword]
    public string InlineTransferTransactionId { get; set; }
    public DateTime TransferTime { get; set; }
    public long TransferBlockHeight { get; set; }
    public DateTime ReceiveTime { get; set; }
    public long ReceiveBlockHeight { get; set; }
    public decimal TransferAmount { get; set; }
    public decimal ReceiveAmount { get; set; }
    public CrossChainStatus Status { get; set; }
    [Keyword]
    public string ReceiptId { get; set; }
    public int Progress { get; set; }
    public DateTime ProgressUpdateTime { get; set; }
    public int ReceiveTransactionAttemptTimes { get; set; }
    [Keyword]
    public string TraceId { get; set; }

    public ReceiptStatus TransferStatus { get; set; }
    public ReceiptStatus ReceiveStatus { get; set; }
}