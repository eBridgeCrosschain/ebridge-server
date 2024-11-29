using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class CheckChainAccessStatusResultDto
{
    public List<ChainAccessInfo> OtherChainList { get; set; } = new();
    public List<ChainAccessInfo> ChainList { get; set; } = new();
}

public class ChainAccessInfo
{
    public string ChainId { get; set; }
    public ChainAccessStatus Status { get; set; }
}

public enum ChainAccessStatus
{
    Unissued = 0,
    Issued,
    Accessing,
    Accessed
}