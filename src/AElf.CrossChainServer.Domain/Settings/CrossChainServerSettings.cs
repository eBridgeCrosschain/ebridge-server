namespace AElf.CrossChainServer.Settings;

public static class CrossChainServerSettings
{
    private const string Prefix = "CrossChainServer";

    //Add your own setting names here. Example:
    public const string CrossChainTransferIndexerSync = Prefix + ".IndexerSync.CrossChainTransfer";
    public const string CrossChainIndexingIndexerSync = Prefix + ".IndexerSync.CrossChainIndexing";
    public const string OracleQueryIndexerSync = Prefix + ".IndexerSync.OracleQuery";
    public const string ReportIndexerSync = Prefix + ".IndexerSync.Report";
    public const string PoolLiquidityIndexerSync = Prefix + ".IndexerSync.PoolLiquidity";
    public const string UserLiquidityIndexerSync = Prefix + ".IndexerSync.UserLiquidity";
    public const string EvmPoolLiquidityIndexerSync = Prefix + ".EvmIndexerSync.PoolLiquidity";
    public const string EvmUserLiquidityIndexerSync = Prefix + ".EvmIndexerSync.UserLiquidity";
    public const string TonIndexTransactionSync = Prefix + ".TonIndex.Transaction";
    public const string EvmNewReceiptIndexerSync = Prefix + ".EvmIndexerSync.NewReceipt";
    public const string EvmTokenSwappedIndexerSync = Prefix + ".EvmIndexerSync.TokenSwapped";
    public const string EvmDailyLimitSetIndexerSync = Prefix + ".EvmIndexerSync.DailyLimitSet";
    public const string EvmRateLimitSetIndexerSync = Prefix + ".EvmIndexerSync.RateLimitSet";
    public const string EvmDailyLimitConsumedIndexerSync = Prefix + ".EvmIndexerSync.DailyLimitConsumed";
    public const string EvmRateLimitConsumedIndexerSync = Prefix + ".EvmIndexerSync.RateLimitConsumed";
}
