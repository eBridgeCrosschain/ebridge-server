namespace AElf.CrossChainServer.TokenAccess;

public static class CommonConstant
{
    public const string Space = " ";
    public const string EmptyString = "";
    public const string Dot = ".";
    public const string Hyphen = "-";
    public const string Colon = ":";
    public const string Underline = "_";
    public const string Comma = ",";
    public const string At = "@";
    public const string Slash = "/";
    public const string V = "v";
    public const string WithdrawRequestErrorKey = "WithdrawRequestErrorKey";
    public const string WithdrawThirdPartErrorKey = "WithdrawThirdPartError";
    public const string SignatureClientName = "Signature";
    public const string ThirdPartSignUrl = "/api/app/signature/thirdPart";
    public const string PendingStatus = "pending";
    public const string SuccessStatus = "success";
    public const string Withdraw = "withdraw";
    public const string Deposit = "deposit";
    public const string ETransferTokenPoolContractName = "ETransfer.Contracts.TokenPool";
    public const string ETransferReleaseToken = "ReleaseToken";
    public const string ETransferSwapToken = "SwapToken";

    public const string DepositOrderLostAlarm = "DepositOrderLostAlarm";
    public const string DepositOrderCoinNotSupportAlarm = "DepositOrderCoinNotSupportAlarm";

    public const string PortKeyAppId = "PortKey";
    public const string NightElfAppId = "NightElf";

    public static class DefaultConst
    {
        public const int ThirdPartDigitals = 4;
        public const int ElfDecimals = 8;
        public const decimal DefaultMinThirdPartFee = 0.1M;
        public const string TokenPoolContractName = "ETransfer.Contracts.TokenPool";
        public const string CaContractName = "Portkey.Contracts.CA";
        public const string CaContractName2 = "Portkey.Contracts.CA2";
        public const string ManagerForwardCall = "ManagerForwardCall";
        public const string TransferToken = "TransferToken";
        public const string PortKeyVersion = "v1";
        public const string PortKeyVersion2 = "v2";
        public const string LimitLogs = "candlesticks,from";
    }

    public static class ChainId
    {
        public const string AElfMainChain = "AELF";
        public const string AElfSideChainTdvv = "tDVV";
    }

    public static class Network
    {
        public const string AElf = "AELF";
        public const string ETH = "ETH";
    }

    public static class NetworkStatus
    {
        public const string Health = "Health";
        public const string Offline = "Offline";
    }

    public static class Symbol
    {
        public const string Elf = "ELF";
        public const string USD = "USD";
        public const string USDT = "USDT";
        public const string SGR = "SGR-1";
    }


    public static class TransactionState
    {
        public const string Mined = "MINED";
        public const string Pending = "PENDING";
        public const string NotExisted = "NOTEXISTED";
        public const string Failed = "FAILED";
        public const string NodeValidationFailed = "NODEVALIDATIONFAILED";
    }

    public static class ThirdPartResponseCode
    {
        public const string DuplicateRequest = "12009";
    }
}