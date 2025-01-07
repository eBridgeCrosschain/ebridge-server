namespace AElf.CrossChainServer.TokenAccess;

public class UserTokenInfoDto
{
    public string TokenName { get; set; }
    public string Symbol { get; set; }
    public string TokenImage { get; set; }
    public string LiquidityInUsd { get; set; }
    public int Holders { get; set; }
    public string Status { get; set; }
    public decimal Price { get; set; }
    public decimal TotalSupply { get; set; }
}