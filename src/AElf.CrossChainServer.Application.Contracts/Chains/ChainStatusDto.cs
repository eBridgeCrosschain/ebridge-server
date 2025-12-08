namespace AElf.CrossChainServer.Chains;

public class ChainStatusDto
{
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
    public long ConfirmedBlockHeight { get; set; }
}