using System.Collections.Generic;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace AElf.CrossChainServer.Chains;

public class FilterLogsAndEventsDto<TEventDTO>  where TEventDTO : IEventDTO, new()
{
    public List<EventLogs<TEventDTO>> Events  { get; set; }
}

public class EventLogs<TEventDTO> where TEventDTO : IEventDTO, new()
{
    public TEventDTO Event  { get; set; }

    public FilterLog Log  { get; set; }
}