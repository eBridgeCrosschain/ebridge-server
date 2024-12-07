using System;
using AElf.CrossChainServer.Tokens;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainTransferIndexDto: EntityDto<Guid>
{
    /// <summary>
    /// 0: Homogeneous, 1: Heterogeneous
    /// </summary>
    public CrossChainType Type { get; set; }
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public string TransferTransactionId { get; set; }
    public string ReceiveTransactionId { get; set; }
    public string InlineTransferTransactionId { get; set; }
    public long TransferTime { get; set; }
    public long TransferBlockHeight { get; set; }
    public long ReceiveTime { get; set; }
    public decimal TransferAmount { get; set; }
    public decimal ReceiveAmount { get; set; }
    public TokenDto TransferToken { get; set; }
    public TokenDto ReceiveToken { get; set; }
    /// <summary>
    /// 0: Transferred, 1: Indexed, 2: Received
    /// </summary>
    public CrossChainStatus Status { get; set; }
    public string ReceiptId { get; set; }
    public int Progress { get; set; }
    public long ProgressUpdateTime { get; set; }

    public bool TransferNeedToBeApproved { get; set; }
}