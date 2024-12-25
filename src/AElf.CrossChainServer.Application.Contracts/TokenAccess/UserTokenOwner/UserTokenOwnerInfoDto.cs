namespace AElf.CrossChainServer.TokenAccess;

public class UserTokenOwnerInfoDto
{
    public string Address { get; set; }
    public string TokenName { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string Owner { get; set; }
    public string ChainId { get; set; }
    public decimal TotalSupply { get; set; }
    // liquidity from awaken
    public string LiquidityInUsd { get; set; }
    // holders from scan
    public int Holders { get; set; }
    public string PoolAddress { get; set; }
    // multiToken contract address
    public string ContractAddress { get; set; }
    // can find - issued (include main chain and dapp chain)
    public string Status { get; set; }
}