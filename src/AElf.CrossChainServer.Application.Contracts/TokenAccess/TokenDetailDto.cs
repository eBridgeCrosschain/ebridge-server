using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenDetailResultDto
{
    public string Code { get; set; }
    public TokenDetailDto Data { get; set; }
}

public class TokenDetailDto
{
    public decimal Price { get; set; }
    public TokenBaseInfo Token { get; set; }
    public string TokenContractAddress { get; set; }
    public int MergeHolders { get; set; }
    public List<string> ChainIds { get; set; }
}

public class TokenBaseInfo
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string ImageUrl { get; set; }
    public int Decimals { get; set; }
}