using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace AElf.CrossChainServer.EvmIndexer;

public interface IEvmEventProcessor<TEventDto> where TEventDto : IEventDTO, new()
{
    Task HandleAsync(string chainId, EventLog<TEventDto> eventLog);
}