using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class NetworkOptions
{
    public Dictionary<string, List<string>> NetworkPattern { get; set; }
    public List<string> WithdrawFeeNetwork { get; set; }
    public Dictionary<string, List<NetworkConfig>> NetworkMap { get; set; }

    public decimal WithdrawLimit24H { get; set; } = 10_0000;
}

public class NetworkConfig
{
    public NetworkInfo NetworkInfo { get; set; }
    public DepositInfo DepositInfo { get; set; }
    public WithdrawInfo WithdrawInfo { get; set; }
    public List<string> SupportType { get; set; }
    public List<string> SupportChain { get; set; }
    public List<string> SupportWhiteList { get; set; }
}

public class NetworkInfo
{
    public string Network { get; set; }
    public string Name { get; set; }
    public string MultiConfirm { get; set; }
    public decimal MultiConfirmSeconds { get; set; }
    public string ContractAddress { get; set; }
    public decimal BlockGenerationSeconds { get; set; }
    public decimal AveragePendingSeconds { get; set; }
    public string MinShowVersion { get; set; }
    public string ExplorerUrl { get; set; }
    public string Status { get; set; }
}

public class DepositInfo
{
    public bool IsOpen { get; set; } = true;
    public string MinDeposit { get; set; }
    public string MaxDeposit { get; set; }
    public string MultiConfirm { get; set; }
    public decimal MultiConfirmSeconds { get; set; }
    public string ContractAddress { get; set; }
    public List<string> ExtraNotes { get; set; }
    public List<string> SwapExtraNotes { get; set; }
}

public class WithdrawInfo
{
    public bool IsOpen { get; set; } = true;
    public string MinWithdraw { get; set; }
    public decimal WithdrawFee { get; set; }

    public bool SpecialWithdrawFeeDisplay { get; set; } = false;

    public string SpecialWithdrawFee { get; set; }
    public decimal WithdrawLocalFee { get; set; }
    public string WithdrawLocalFeeUnit { get; set; }
    public string WithdrawLimit24h { get; set; }
    public decimal MultiConfirmSeconds { get; set; }
    public int Decimals { get; set; }
}