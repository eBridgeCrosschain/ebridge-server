using System;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.CrossChain;

public class ReportInfo : MultiChainEntity<Guid>
{
    public long RoundId { get; set; }
    [Keyword]
    public string Token { get; set; }
    [Keyword]
    public string TargetChainId { get; set; }
    [Keyword]
    public string ReceiptId { get; set; }
    [Keyword]
    public string ReceiptHash { get; set; }
    public ReportStep Step { get; set; }
    public int QueryTimes { get; set; }
    public long TransmitHeight { get; set; }
    public long LastUpdateHeight { get; set; }
    public string QueryTransactionId { get; set; }
    public int ResendTimes { get; set; } = 0;
    public string ReceiptInfo { get; set; }
}