using System;
using System.Collections.Generic;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Entities;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrder : TokenApplyOrderBase
{
    public List<ChainTokenInfo> ChainTokenInfo { get; set; }
    public List<StatusChangedRecord> StatusChangedRecords { get; set; }
}

public class ChainTokenInfo : CrossChainServerEntity<Guid>
{
    public Guid OrderId { get; set; }
    public string ChainId { get; set; }
    public string ChainName { get; set; }
    public string TokenName { get; set; }
    public string Symbol { get; set; }
    public decimal TotalSupply { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string PoolAddress { get; set; }
    public string ContractAddress { get; set; }
    public string Status { get; set; }
    public BlockchainType Type { get; set; } // EVM -> otherChain
    public TokenApplyOrder Order { get; set; } //Navigation property
}

public class StatusChangedRecord : CrossChainServerEntity<Guid>
{
    public Guid OrderId { get; set; }
    public string Status { get; set; }
    public DateTime Time { get; set; }
    public TokenApplyOrder Order { get; set; } //Navigation property
}