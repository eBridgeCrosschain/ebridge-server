using System;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrderBase : CrossChainServerEntity<Guid>
{
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string UserAddress { get; set; }
    [Keyword] public string Status { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
}