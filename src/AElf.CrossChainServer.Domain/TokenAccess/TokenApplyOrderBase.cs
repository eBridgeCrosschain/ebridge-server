using System;
using System.Collections.Generic;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrderBase : CrossChainServerEntity<Guid>
{
    [Keyword] public override Guid Id { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string UserAddress { get; set; }
    [Keyword] public string Status { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public List<ChainTokenInfo> ChainTokenInfo { get; set; }
    public ChainTokenInfo OtherChainTokenInfo { get; set; }
    public Dictionary<string, string> StatusChangedRecord { get; set; }
    public Dictionary<string, string> ExtensionInfo { get; set; }
}

// public class ChainTokenInfo
// {
//     [Keyword] public string ChainId { get; set; }
//     [Keyword] public string ChainName { get; set; }
//     [Keyword] public string TokenName { get; set; }
//     [Keyword] public string Symbol { get; set; }
//     public decimal TotalSupply { get; set; }
//     public int Decimals { get; set; }
//     [Text(Index = false)] public string Icon { get; set; }
//     [Keyword] public string PoolAddress { get; set; }
//     [Keyword] public string ContractAddress { get; set; }
//     [Keyword] public string Status { get; set; }
// }