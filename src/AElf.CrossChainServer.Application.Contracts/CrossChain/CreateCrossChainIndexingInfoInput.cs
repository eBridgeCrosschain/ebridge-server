using System;

namespace AElf.CrossChainServer.CrossChain;

public class CreateCrossChainIndexingInfoInput : InputBase
{
    public long BlockHeight { get; set; }
    public DateTime BlockTime { get; set; }
    public string IndexChainId { get; set; }
    public long IndexBlockHeight { get; set; }
}