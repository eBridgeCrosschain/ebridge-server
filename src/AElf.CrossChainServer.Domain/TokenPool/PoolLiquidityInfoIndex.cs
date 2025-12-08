using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;

namespace AElf.CrossChainServer.TokenPool;

public class PoolLiquidityInfoIndex : LiquidityBase,IIndexBuild
{
    public Token TokenInfo { get; set; }
}