using System;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrderBaseDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string UserAddress { get; set; }
    public string ChainIds { get; set; } = "[]";
    public string PoolAddressList { get; set; }
    public TokenApplyOrderStatus Status { get; set; }
    public long UpdateTime { get; set; }
    public string OtherChainId { get; set; }
    public string OtherChainPoolAddress { get; set; }
}