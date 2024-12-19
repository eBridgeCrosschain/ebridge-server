using System.Collections.Generic;
using System.Linq;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenOwnerListDto
{
    public List<TokenOwnerDto> TokenOwnerList { get; set; } = new();
}

public class TokenOwnerDto
{
    public string TokenName { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string Owner { get; set; }
    public List<string> ChainIds { get; set; } = new();
    public decimal TotalSupply { get; set; }
    public string LiquidityInUsd { get; set; }
    public int Holders { get; set; }
    public string PoolAddress { get; set; }
    public string ContractAddress { get; set; }
    public string Status { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is TokenOwnerDto t)
        {
            return Symbol == t.Symbol && new HashSet<string>(ChainIds).SetEquals(t.ChainIds);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Symbol.GetHashCode() ^
               new HashSet<string>(ChainIds).Aggregate(0, (acc, chainId) => acc ^ chainId.GetHashCode());
    }
}