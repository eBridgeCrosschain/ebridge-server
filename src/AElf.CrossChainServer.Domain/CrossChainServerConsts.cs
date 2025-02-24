using Volo.Abp.Identity;

namespace AElf.CrossChainServer;

public static class CrossChainServerConsts
{
    public const string DbTablePrefix = "App";

    public const string DbSchema = null;

    public const string AElfMainChainId = "MainChain_AELF";
    public const string AElfMainChain = "AELF";

    public const int MaxReportQueryTimes = 10;
    public const int HalfOfTheProgress = 50;
    public const int FullOfTheProgress = 100;
    public const int DefaultReportTimeoutHeightThreshold = 3600;
    public const long DefaultMaxReportResendTimes = 3;
    public const long DefaultDailyLimitRefreshTime = 86400;
    public const long DefaultRateLimitSeconds = 60;

    public const string TonTransferredOpCode = "0xfcaf1515";
    public const string TonReceivedOpCode = "0x9e47031d";
    public const string TonDailyLimitChangedOpCode = "0x40839634";
    public const string TonDailyLimitConsumedOpCode = "0xc3de3da2";
    public const string TonRateLimitConsumedOpCode = "0x7a170c15";
    public const string TonRateLimitChangedOpCode = "0xef662842";
    public const int TonReleasedEventId = 1031208282;
    public const string At = "@";
    public const string SuccessHttpCode = "20000";
    public const int DefaultEvmTokenDecimal = 18;
    public const string AddressZero = "0x0000000000000000000000000000000000000000";
    public const string TonCoinAddress = "EQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAM9c";
    public const int TonCoinTokenDecimal = 9;
    public const string TonCoinTokenSymbol = "TON";


}
