using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenDetailResultDto
{
    public string Code { get; set; }
    public TokenDetailDto Data { get; set; }
}

public class TokenDetailDto
{
    public string TokenContractAddress { get; set; }
    public int Holders { get; set; }
    public List<string> ChainIds { get; set; }
}