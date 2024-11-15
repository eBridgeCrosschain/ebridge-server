namespace AElf.CrossChainServer;

public static class CrossChainServerConsts
{
    public const string DbTablePrefix = "App";

    public const string DbSchema = null;

    public const string AElfMainChainId = "MainChain_AELF";

    public const int MaxReportQueryTimes = 10;
    public const int HalfOfTheProgress = 50;
    public const int FullOfTheProgress = 100;
    public const int DefaultReportTimeoutHeightThreshold = 3600;
    public const long DefaultMaxReportResendTimes = 3;
    public const long DefaultDailyLimitRefreshTime = 86400;
    public const long DefaultRateLimitSeconds = 60;

    public const string TonTransferOpCode = "0xfcaf1515";
    public const string TonReceiveOpCode = "0xc64370e5";
    public const string TonSetPoolLimitOpCode = "";
}
