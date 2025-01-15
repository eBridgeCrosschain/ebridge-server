using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.BridgeContract;

namespace AElf.CrossChainServer.Worker;

public interface IBridgeContractSyncProvider
{
    public TransferType Type { get; }
    Task SyncAsync(string chainId, List<Guid> tokenIds, List<string> targetChainIds);
}