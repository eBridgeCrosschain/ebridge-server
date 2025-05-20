namespace AElf.CrossChainServer.Worker;

public class WorkerSyncPeriodOptions
{
    public int AutoReceiveSyncPeriod { get; set; } = 60 * 1000; // 1min
    public int ProgressUpdatePeriod { get; set; } = 1000 * 10; // 10s
    
    public int CrossChainIndexingCleanPeriod { get; set; } = 1000 * 60 * 60; // 1h
    public int CheckAElfConfirmedTransactionPeriod { get; set; } = 1000 * 60; // 1min
    public int CheckEvmConfirmedTransactionPeriod { get; set; } = 1000 * 60; // 1min
}