using System;

namespace AElf.CrossChainServer.Worker.IndexerSync;

public class GraphQLDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public DateTime BlockTime { get; set; }
}