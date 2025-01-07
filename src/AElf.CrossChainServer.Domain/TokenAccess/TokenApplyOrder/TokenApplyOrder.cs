using System;
using System.Collections.Generic;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Entities;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrder : TokenApplyOrderBase
{
    public string ChainId { get; set; }
    public string ChainName { get; set; }
    public string TokenName { get; set; }
    public decimal TotalSupply { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string PoolAddress { get; set; }
    public string ContractAddress { get; set; }
    public List<StatusChangedRecord> StatusChangedRecords { get; set; }
    
    public TokenApplyOrder()
    {
        StatusChangedRecords = new List<StatusChangedRecord>();
    }
}

public class StatusChangedRecord : CrossChainServerEntity<Guid>
{
    public Guid OrderId { get; set; }
    public string Status { get; set; }
    public DateTime Time { get; set; }
    public TokenApplyOrder Order { get; set; } //Navigation property
}