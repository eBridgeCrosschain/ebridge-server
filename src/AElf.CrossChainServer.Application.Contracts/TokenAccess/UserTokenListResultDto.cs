using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class UserTokenListResultDto
{
    public string Code { get; set; }
    public UserTokenDataDto Data { get; set; }
}

public class UserTokenDataDto {
    public int TotalCount { get; set; }
    public List<UserTokenItemDto> Items { get; set; }
}

public class UserTokenItemDto
{
    public string TokenName { get; set; }
    public string Symbol { get; set; }
    public string TokenImage { get; set; }
    public string Issuer { get; set; }
    public string Owner { get; set; }
    public int Decimals { get; set; }
    public long TotalSupply { get; set; }
    public long CurrentSupply { get; set; }
    public string IssueChain { get; set; }
    public long IssueChainId { get; set; }
    public string OriginIssueChain { get; set; }
    public string TokenAction { get; set; }
}