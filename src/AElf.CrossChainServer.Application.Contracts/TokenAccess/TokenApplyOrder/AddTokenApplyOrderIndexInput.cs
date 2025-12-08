using System;
using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class AddTokenApplyOrderIndexInput
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string UserAddress { get; set; }
    public string Status { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public string ChainId { get; set; }
    public string ChainName { get; set; }
    public string TokenName { get; set; }
    public decimal TotalSupply { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string PoolAddress { get; set; }
    public string ContractAddress { get; set; }
    public List<StatusChangedRecordDto> StatusChangedRecords { get; set; }
}