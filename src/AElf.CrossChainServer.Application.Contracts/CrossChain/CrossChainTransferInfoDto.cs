using System;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainTransferInfoDto : GraphQLDto
{
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public string TransferTransactionId { get; set; }
    public string ReceiveTransactionId { get; set; }
    public DateTime TransferTime { get; set; }
    public long TransferBlockHeight { get; set; }
    public DateTime ReceiveTime { get; set; }
    public long TransferAmount { get; set; }
    public long ReceiveAmount { get; set; }
    public string ReceiptId { get; set; }
    public string TransferTokenSymbol { get; set; }
    public string ReceiveTokenSymbol { get; set; }
    public TransferType TransferType { get; set; }
    public CrossChainType CrossChainType { get; set; }
}

public enum TransferType
{
    Transfer,
    Receive
}

public class GraphQLDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public DateTime BlockTime { get; set; }
}