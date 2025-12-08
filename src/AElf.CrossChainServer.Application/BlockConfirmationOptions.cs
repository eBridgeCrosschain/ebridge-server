using System.Collections.Generic;

namespace AElf.CrossChainServer;

public class BlockConfirmationOptions
{
    public Dictionary<string, long> ConfirmationCount { get; set; } = new();
}