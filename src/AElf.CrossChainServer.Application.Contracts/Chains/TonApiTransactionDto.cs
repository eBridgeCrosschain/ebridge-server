using System.Collections.Generic;

namespace AElf.CrossChainServer.Chains;

public class TonApiTransactionDto
{
    public string Hash { get; set; }
    public long Lt { get; set; }
    public TonApiAccountDto Account { get; set; }
    public bool Success { get; set; }
    public long Utime { get; set; }
    public string OrigStatus { get; set; }
    public string EndStatus { get; set; }
    public long TotalFees { get; set; }
    public long EndBalance { get; set; }
    public string TransactionType { get; set; }
    public string StateUpdateOld { get; set; }
    public string StateUpdateNew { get; set; }
    public TonapiMessageDto InMsg { get; set; }
    public List<TonapiMessageDto> OutMsg { get; set; }
    public string Block { get; set; }
    public string PrevTransHash { get; set; }
    public long PrevTransLt { get; set; }
    public TonapiComputePhaseDto ComputePhase { get; set; }
    public TonapiStoragePhaseDto StoragePhase { get; set; }
    public TonapiCreditPhaseDto CreditPhase { get; set; }
    public TonapiActionPhaseDto ActionPhase { get; set; }
    public string BouncePhase { get; set; }
    public bool Aborted { get; set; }
    public bool Destroyed { get; set; }
    public string Raw { get; set; }
}

public class TonApiAccountDto
{
    public string Address { get; set; }
    public string Name { get; set; }
    public bool IsScam { get; set; }
    public string Icon { get; set; }
    public bool IsWallet { get; set; }
}

public class TonapiMessageDto
{
    public string MsgType { get; set; }
    public long CreatedLt { get; set; }
    public bool IhrDisabled { get; set; }
    public bool Bounce { get; set; }
    public bool Bounced { get; set; }
    public long Value { get; set; }
    public long FwdFee { get; set; }
    public long IhrFee { get; set; }
    public TonApiAccountDto Destination { get; set; }
    public TonApiAccountDto Source { get; set; }
    public long ImportFee { get; set; }
    public long CreatedAt { get; set; }
    public string OpCode { get; set; }
    public InitDto Init { get; set; }
    public string Hash { get; set; }
    public string RawBody { get; set; }
    public string DecodedOpName { get; set; }
    public DecodedBodyDto DecodedBody { get; set; }
}

public class InitDto
{
    public string Boc { get; set; }
    public List<string> Interfaces { get; set; }
}

public class TonapiComputePhaseDto
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

public class TonapiStoragePhaseDto
{
    public long FeesCollected { get; set; }
    public long FeesDue { get; set; }
    public string StatusChange { get; set; }
}

public class TonapiCreditPhaseDto
{
    public long FeesCollected { get; set; }
    public long Credit { get; set; }
}

public class TonapiActionPhaseDto
{
    public bool Success { get; set; }
    public int ResultCode { get; set; }
    public int TotalActions { get; set; }
    public int SkippedActions { get; set; }
    public long FwdFees { get; set; }
    public long TotalFees { get; set; }
    public string ResultCodeDescription { get; set; }
}

public class DecodedBodyDto
{
    public string Payload { get; set; }
}