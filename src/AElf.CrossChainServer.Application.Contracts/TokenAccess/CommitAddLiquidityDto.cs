namespace AElf.CrossChainServer.TokenAccess;

public class CommitAddLiquidityDto
{
    public string OrderId { get; set; }
    public string ChainId { get; set; }
    public bool Success { get; set; }
}