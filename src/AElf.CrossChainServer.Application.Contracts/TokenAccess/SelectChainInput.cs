using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class SelectChainInput
{
    public string Symbol { get; set; }
    public List<string> OtherChainIds { get; set; }
    public List<string> ChainIds { get; set; }
    public string Address { get; set; }
}