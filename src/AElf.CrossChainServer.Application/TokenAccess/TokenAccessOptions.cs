using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenAccessOptions
{
    public List<string> ChainIdList { get; set; } = new();
    public List<string> OtherChainIdList { get; set; } = new();
    public string ScanBaseUrl { get; set; }
    public string ScanTokenDetailUri { get; set; }
    public string SymbolMarketBaseUrl { get; set; }
    public string SymbolMarketUserTokenListUri { get; set; }
    public string SymbolMarketUserThirdTokenListUri { get; set; }
    public string SymbolMarketPrepareBindingUri { get; set; }
    public string SymbolMarketBindingUri { get; set; }
    public string AwakenBaseUrl { get; set; }
    public string AwakenGetTokenLiquidityUri { get; set; }
    public string AwakenGetPriceUri { get; set; }
    public string HashVerifyKey { get; set; }
    public AvailableTokenConfigDto DefaultConfig { get; set; } = new();
    public Dictionary<string, AvailableTokenConfigDto> TokenConfig { get; set; } = new();
    public string LarkWebhook { get; set; }
    public Dictionary<string, string> ChainIdMap { get; set; }
    public Dictionary<string,string> SymbolMap { get; set; }
    public string DAppChainTokenContractAddress { get; set; }
}
public class AvailableTokenConfigDto
{
    public string Liquidity { get; set; } = "1000";
    public int Holders { get; set; } = 1000;
    public long MinLiquidityInUsd { get; set; } = 5000;
}