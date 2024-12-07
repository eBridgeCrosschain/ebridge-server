using System;

namespace AElf.CrossChainServer.CrossChain;

[Serializable]
public class CrossChainTransferEto
{
    public Guid Id { get; set; }
    public CrossChainType Type { get; set; }
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public string TransferTransactionId { get; set; }
    public string ReceiveTransactionId { get; set; }
    public string InlineTransferTransactionId { get; set; }
    public DateTime TransferTime { get; set; }
    public long TransferBlockHeight { get; set; }
    public DateTime ReceiveTime { get; set; }
    public decimal TransferAmount { get; set; }
    public decimal ReceiveAmount { get; set; }
    public Guid TransferTokenId { get; set; }
    public Guid ReceiveTokenId { get; set; }
    public CrossChainStatus Status { get; set; }
    public string ReceiptId { get; set; }
    public double Progress { get; set; }
    public DateTime ProgressUpdateTime { get; set; }
    public bool TransferNeedToBeApproved { get; set; }
}