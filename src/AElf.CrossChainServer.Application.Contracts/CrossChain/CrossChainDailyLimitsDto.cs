namespace AElf.CrossChainServer.CrossChain;

public class CrossChainDailyLimitsDto
{
    public string Token { get; set; }
    public decimal Allowance { get; set; }

    public CrossChainDailyLimitsDto(string token, decimal allowance)
    {
        Token = token;
        Allowance = allowance;
    }
}