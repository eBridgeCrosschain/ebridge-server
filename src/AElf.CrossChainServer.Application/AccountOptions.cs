using System.Collections.Generic;

namespace AElf.CrossChainServer;

public class AccountOptions
{
    public Dictionary<string, string> PrivateKeysForCall { get; set; }
    public Dictionary<string, string> PublicKeys { get; set; }

}