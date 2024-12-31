using System;
using System.Collections.Generic;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.CrossChain;

public class WalletUserDto : CrossChainServerEntity<Guid>
{
    [Keyword] public Guid UserId { get; set; }
    [Keyword] public string AppId { get; set; }
    [Keyword] public string CaHash { get; set; }
    public List<AddressInfoDto> AddressInfos { get; set; }
    public long CreateTime { get; set; }
    public long ModificationTime { get; set; }
}

public class AddressInfoDto
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public Guid Id { get; set; }
}