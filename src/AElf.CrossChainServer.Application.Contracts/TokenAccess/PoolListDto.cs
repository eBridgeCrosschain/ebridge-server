using AElf.CrossChainServer.Tokens;

namespace AElf.CrossChainServer.TokenAccess;

public class PoolInfoDto
{
    public TokenDto Token { get; set; }
    public string ChainId { get; set; }
    public decimal? MyTvlInUsd { get; set; }
    public decimal TotalTvlInUsd { get; set; }
    public decimal TokenPrice { get; set; }
    
}