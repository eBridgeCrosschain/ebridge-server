using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrderIndex : TokenApplyOrderBase, IIndexBuild
{
    public ChainTokenInfoIndex ChainTokenInfo { get; set; }
    public Dictionary<string, string> StatusChangedRecord { get; set; }
}

public class ChainTokenInfoIndex
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string ChainName { get; set; }
    [Keyword] public string TokenName { get; set; }
    public decimal TotalSupply { get; set; }
    public int Decimals { get; set; }
    [Text(Index = false)] public string Icon { get; set; }
    [Keyword] public string PoolAddress { get; set; }
    [Keyword] public string ContractAddress { get; set; }
    [Keyword] public string Status { get; set; }
}