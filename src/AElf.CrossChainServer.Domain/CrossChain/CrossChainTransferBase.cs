using System;
using AElf.CrossChainServer.Entities;
using Nest;
using Volo.Abp.Domain.Entities;

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
    public DateTime TransferTime { get; set; }
    public long TransferBlockHeight { get; set; }
    public DateTime ReceiveTime { get; set; }
    public decimal TransferAmount { get; set; }
    public decimal ReceiveAmount { get; set; }
    public CrossChainStatus Status { get; set; }
    [Keyword]
    public string ReceiptId { get; set; }
    public double Progress { get; set; }
    public DateTime ProgressUpdateTime { get; set; }
}