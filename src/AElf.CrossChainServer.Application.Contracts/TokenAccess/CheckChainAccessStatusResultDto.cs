using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class CheckChainAccessStatusResultDto
{
    public List<ChainAccessInfo> ChainList { get; set; } = new();
}

public class ChainAccessInfo
{
    public string ChainId { get; set; }
    public string ChainName { get; set; }
    public string Status { get; set; }
    public bool Checked { get; set; }
    public string TokenName { get; set; }
    public string Symbol { get; set; }
    public decimal TotalSupply { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string PoolAddress { get; set; }
    public string ContractAddress { get; set; }
    public string BindingId { get; set; }
    public string ThirdTokenId { get; set; }
}
