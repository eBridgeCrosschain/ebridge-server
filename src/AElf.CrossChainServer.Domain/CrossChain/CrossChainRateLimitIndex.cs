using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainRateLimitIndex : CrossChainRateLimitBase, IIndexBuild
{
    public Token Token { get; set; }
}