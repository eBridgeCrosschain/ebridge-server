using System;
using System.Collections.Generic;
using AElf.CrossChainServer.Entities;

namespace AElf.CrossChainServer.TokenAccess;

public class UserTokenOwnerDto : CrossChainServerEntity<Guid>
{
    public List<TokenOwnerDto> TokenOwnerList { get; set; } = new();
    public string Address { get; set; }
}