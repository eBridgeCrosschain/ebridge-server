using AElf.Client.Dto;

namespace AElf.CrossChainServer.Chains;

public class TransactionResultDto
{
    public string ChainId { get; set; }
    public bool IsMined { get; set; }
    public bool IsFailed { get; set; }
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
    public TransactionDto Transaction { get; set; }
    public LogEventDto[] Logs { get; set; }
}