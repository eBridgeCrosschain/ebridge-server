using System;
using System.Collections.Generic;
using AElf.CrossChainServer.Chains;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrderEto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string UserAddress { get; set; }
    public string Status { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public List<ChainTokenInfoDto> ChainTokenInfo { get; set; }
    public List<StatusChangedRecordDto> StatusChangedRecords { get; set; }
}

public class ChainTokenInfoDto
{
    public Guid Id { get; set; }
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
}

public class StatusChangedRecordDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Status { get; set; }
    public DateTime Time { get; set; }
}