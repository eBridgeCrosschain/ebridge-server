using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainDailyLimitIndex : CrossChainDailyLimitBase, IIndexBuild
{
    public Token Token { get; set; }
}