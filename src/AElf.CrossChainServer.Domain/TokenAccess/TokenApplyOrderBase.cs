using System;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrderBase : CrossChainServerEntity<Guid>
{
    [Keyword]
    public string Symbol { get; set; }
    [Keyword]
    public string UserAddress { get; set; }

    [Keyword] public string ChainIds { get; set; } = "[]";
    [Keyword]
    public string PoolAddressList { get; set; }
    public TokenApplyOrderStatus Status { get; set; }
    public long UpdateTime { get; set; }
    [Keyword] public string OtherChainId { get; set; }
    [Keyword] public string OtherChainPoolAddress { get; set; }
}