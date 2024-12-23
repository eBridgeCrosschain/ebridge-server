using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrderIndex : TokenApplyOrderBase, IIndexBuild
{
    public List<ChainTokenInfoIndex> ChainTokenInfo { get; set; }
    public ChainTokenInfoIndex OtherChainTokenInfo { get; set; }
    public Dictionary<string, string> StatusChangedRecord { get; set; }
}

public class ChainTokenInfoIndex
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string ChainName { get; set; }
    [Keyword] public string TokenName { get; set; }
    [Keyword] public string Symbol { get; set; }
    public decimal TotalSupply { get; set; }
    public int Decimals { get; set; }
    [Text(Index = false)] public string Icon { get; set; }
    [Keyword] public string PoolAddress { get; set; }
    [Keyword] public string ContractAddress { get; set; }
    [Keyword] public string Status { get; set; }
}