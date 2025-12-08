using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainTransferIndex : CrossChainTransferBase, IIndexBuild
{
    public Token TransferToken { get; set; }
    public Token ReceiveToken { get; set; }
}