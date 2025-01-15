
using System.Collections.Generic;

namespace AElf.CrossChainServer.Chains.Ton;

public class JettonMasterDto
{
    public List<JettonMaster> JettonMasters { get; set; }
}

public class JettonMaster
{
    public string Address { get; set; }
    public JettonContent JettonContent { get; set; }
}

public class JettonContent
{
    public string Decimals { get; set; }
    public string Uri { get; set; }
    public string Symbol { get; set; } = null;
}