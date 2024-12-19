using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenOptions
{
    public Dictionary<string, List<TokenConfig>> Deposit { get; set; }
    public Dictionary<string, List<TokenConfig>> Withdraw { get; set; }
    public List<TokenConfig> Transfer { get; set; } = new();
    public List<TokenSwapConfig> DepositSwap { get; set; }
}

public class TokenConfig
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string ContractAddress { get; set; }
}

public class TokenSwapConfig
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string ContractAddress { get; set; }
    public List<ToTokenConfig> ToTokenList { get; set;}
}

public class ToTokenConfig
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public List<string> ChainIdList { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
}