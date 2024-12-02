using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenAccessOptions
{
    public List<string> ChainIdList { get; set; } = new();
    public List<string> OtherChainIdList { get; set; } = new();
    public string ScanBaseUrl { get; set; }
    public string ScanTokenListUri { get; set; }
    
    public string SymbolMarketBaseUrl { get; set; }
    public string SymbolMarketIssueTokenUri { get; set; }
    public string SymbolMarketGetIssueInfoUri { get; set; }
    
    public string AwakenBaseUrl { get; set; }
    public string AwakenGetTokenLiquidityUri { get; set; }

    public string WebHookUrl { get; set; }
}