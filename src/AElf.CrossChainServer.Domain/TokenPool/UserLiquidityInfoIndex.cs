using Nest;
using Token = AElf.CrossChainServer.Tokens.Token;

namespace AElf.CrossChainServer.TokenPool;

public class UserLiquidityInfoIndex : LiquidityBase
{
    public Token TokenInfo { get; set; }
    [Keyword]
    public string Provider { get; set; }
}