using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class AvailableTokensDto
{
    public List<AvailableTokenDto> TokenList { get; set; } = new();
}

public class AvailableTokenDto
{
    public string TokenName { get; set; }
    public string Symbol { get; set; }
    public string LiquidityInUsd { get; set; }
    public int Holders { get; set; }
}