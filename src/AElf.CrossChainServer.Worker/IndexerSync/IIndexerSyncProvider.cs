using System.Threading.Tasks;

namespace AElf.CrossChainServer.Worker.IndexerSync;

public interface IIndexerSyncProvider
{
    bool RequiresRealTime { get; }

    Task ExecuteAsync(string chainId, int syncDelayHeight = 0, string typePrefix = null,bool isConfirmed = false);
}