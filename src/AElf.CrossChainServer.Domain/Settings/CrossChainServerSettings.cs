namespace AElf.CrossChainServer.Settings;

public static class CrossChainServerSettings
{
    private const string Prefix = "CrossChainServer";

    //Add your own setting names here. Example:
    public const string CrossChainTransferIndexerSync = Prefix + ".IndexerSync.CrossChainTransfer";
    public const string CrossChainIndexingIndexerSync = Prefix + ".IndexerSync.CrossChainIndexing";
    public const string OracleQueryIndexerSync = Prefix + ".IndexerSync.OracleQuery";
    public const string ReportIndexerSync = Prefix + ".IndexerSync.Report";
    public const string TonIndexTransactionSync = Prefix + ".TonIndex.Transaction";
}
