using System.Collections.Generic;

namespace AElf.CrossChainServer.Chains.Ton;


public class TonApiTransactions
{
    public List<TonApiTransaction> Transactions { get; set; }
}

public class TonApiTransaction
{
    public string Hash { get; set; }
    public long Lt { get; set; }
    public TonApiAccount Account { get; set; }
    public bool Success { get; set; }
    public long Utime { get; set; }
    public string OrigStatus { get; set; }
    public string EndStatus { get; set; }
    public long TotalFees { get; set; }
    public long EndBalance { get; set; }
    public string TransactionType { get; set; }
    public string StateUpdateOld { get; set; }
    public string StateUpdateNew { get; set; }
    public TonapiMessage InMsg { get; set; }
    public List<TonapiMessage> OutMsg { get; set; }
    public string Block { get; set; }
    public string PrevTransHash { get; set; }
    public long PrevTransLt { get; set; }
    public TonapiComputePhase ComputePhase { get; set; }
    public TonapiStoragePhase StoragePhase { get; set; }
    public TonapiCreditPhase CreditPhase { get; set; }
    public TonapiActionPhase ActionPhase { get; set; }
    public string BouncePhase { get; set; }
    public bool Aborted { get; set; }
    public bool Destroyed { get; set; }
    public string Raw { get; set; }
}

public class TonApiAccount
{
    public string Address { get; set; }
    public string Name { get; set; }
    public bool IsScam { get; set; }
    public string Icon { get; set; }
    public bool IsWallet { get; set; }
}

public class TonapiMessage
{
    public string MsgType { get; set; }
    public long CreatedLt { get; set; }
    public bool IhrDisabled { get; set; }
    public bool Bounce { get; set; }
    public bool Bounced { get; set; }
    public long Value { get; set; }
    public long FwdFee { get; set; }
    public long IhrFee { get; set; }
    public TonApiAccount Destination { get; set; }
    public TonApiAccount Source { get; set; }
    public long ImportFee { get; set; }
    public long CreatedAt { get; set; }
    public string OpCode { get; set; }
    public Init Init { get; set; }
    public string Hash { get; set; }
    public string RawBody { get; set; }
    public string DecodedOpName { get; set; }
    public DecodedBody DecodedBody { get; set; }
}

public class Init
{
    public string Boc { get; set; }
    public List<string> Interfaces { get; set; }
}

public class TonapiComputePhase
{
    public bool Skipped { get; set; }
    public string SkipReason { get; set; }
    public bool Success { get; set; }
    public long GasFees { get; set; }
    public long GasUsed { get; set; }
    public long VmSteps { get; set; }
    public int ExitCode { get; set; }
    public string ExitCodeDescription { get; set; }
}

public class TonapiStoragePhase
{
    public long FeesCollected { get; set; }
    public long FeesDue { get; set; }
    public string StatusChange { get; set; }
}

public class TonapiCreditPhase
{
    public long FeesCollected { get; set; }
    public long Credit { get; set; }
}

public class TonapiActionPhase
{
    public bool Success { get; set; }
    public int ResultCode { get; set; }
    public int TotalActions { get; set; }
    public int SkippedActions { get; set; }
    public long FwdFees { get; set; }
    public long TotalFees { get; set; }
    public string ResultCodeDescription { get; set; }
}

public class DecodedBody
{
    public string Payload { get; set; }
}