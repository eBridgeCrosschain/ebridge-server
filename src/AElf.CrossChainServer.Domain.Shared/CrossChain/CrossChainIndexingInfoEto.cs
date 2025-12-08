using System;

namespace AElf.CrossChainServer.CrossChain;

[Serializable]
public class CrossChainIndexingInfoEto
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public long BlockHeight { get; set; }
    public DateTime BlockTime { get; set; }
    public string IndexChainId { get; set; }
    public long IndexBlockHeight { get; set; }
}