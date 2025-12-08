using System.Collections.Generic;

namespace AElf.CrossChainServer.Notify;

public class NotifyRequest
{
    // Message template
    public string Template { get; set; }
    
    // Message Parameters
    public Dictionary<string, string> Params { get; set; }

}