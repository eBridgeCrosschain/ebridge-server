using System;
using AElf.CrossChainServer.Entities;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenInvokeDto : CrossChainServerEntity<Guid>
{
    public Guid UserTokenIssueId { get; set; } = Guid.Empty;
    public string BindingId { get; set; }
    public string ThirdTokenId { get; set; }
}