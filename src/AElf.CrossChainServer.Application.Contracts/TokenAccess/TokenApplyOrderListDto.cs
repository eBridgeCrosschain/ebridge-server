using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrderResultDto : TokenApplyOrderDto
{
    public long RejectedTime { get; set; }
    public string RejectedReason { get; set; }
    public long FailedTime { get; set; }
    public string FailedReason { get; set; }
}

public class TokenApplyOrderDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string UserAddress { get; set; }
    public string Status { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public List<ChainTokenInfoDto> ChainTokenInfo { get; set; }
    public ChainTokenInfoDto OtherChainTokenInfo { get; set; }
    public Dictionary<string, string> StatusChangedRecord { get; set; }
}

public class ChainTokenInfoDto
{
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
    public string BalanceAmount { get; set; }
    public string MinAmount { get; set; }
}

public class TokenApplyDto
{
    public string Symbol { get; set; }
    public string Address { get; set; }
    public string ChainId { get; set; }
    public string Coin { get; set; }
    public string Amount { get; set; }
}