
namespace AElf.CrossChainServer.TokenAccess;

public class TokenInfoDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public long Decimals { get; set; }
    public string Address { get; set; }
    public string IssueChainId { get; set; }
    public bool IsNativeToken { get; set; }

}