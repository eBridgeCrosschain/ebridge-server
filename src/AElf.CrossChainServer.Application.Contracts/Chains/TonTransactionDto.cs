using System.Collections.Generic;

namespace AElf.CrossChainServer.Chains;

public class TonTransactionDto
{
    public string Account { get; set; }
    public TonBlockIdDto BlockRef { get; set; }
    public string Hash { get; set; }
    public TonMessageDto InMsg { get; set; }
    public long McBlockSeqno { get; set; }
    public string Lt { get; set; }
    public int Now { get; set; }
    public string OrigStatus { get; set; }
    public List<TonMessageDto> OutMsgs { get; set; }
    public string PrevTransHash { get; set; }
    public string PrevTransLt { get; set; }
    public string TotalFees { get; set; }
    public string TraceId { get; set; }
}

public class TonBlockIdDto
{
    public long Seqno { get; set; }
    public string Shard { get; set; }
    public int Workchain { get; set; }
}

public class TonMessageDto
{
    public bool Bounce { get; set; }
    public bool Bounced { get; set; }
    public string CreatedAt { get; set; }
    public string CreatedLt { get; set; }
    public string Destination { get; set; }
    public string FwdFee { get; set; }
    public string Hash { get; set; }
    public bool IhrDisabled { get; set; }
    public string IhrFee { get; set; }
    public string ImportFee { get; set; }
    public TonMessageContentDto InitState { get; set; }
    public TonMessageContentDto MessageContent { get; set; }
    public string Opcode { get; set; }
    public string Source { get; set; }
    public string Value { get; set; }
}

public class TonMessageContentDto
{
    public string Body { get; set; }
    public TonDecodedContentDto Decoded { get; set; }
    public string Hash { get; set; }
}

public class TonDecodedContentDto
{
    public string Comment { get; set; }
    public string Type { get; set; }
}