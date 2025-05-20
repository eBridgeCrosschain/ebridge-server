using System;
using System.Collections.Generic;

namespace AElf.CrossChainServer.EvmIndexer;

public class ContractEventSubscriptionInfo
{
    public string ContractAddress { get; set; }
    public List<Type> EventTypes { get; set; } = new();
}