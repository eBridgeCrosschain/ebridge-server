using System.Collections.Generic;

namespace AElf.CrossChainServer
{
    public class ChainApiOptions
    {
        public Dictionary<string,string> ChainNodeApis { get; set; }
        public Dictionary<string,string> ApiKeys { get; set; }
    }
}