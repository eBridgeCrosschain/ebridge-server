using System;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainIndexingInfo : MultiChainEntity<Guid>
{
    public long BlockHeight { get; set; }
    public DateTime BlockTime { get; set; }
    [Keyword]
    public string IndexChainId { get; set; }
    public long IndexBlockHeight { get; set; }
}