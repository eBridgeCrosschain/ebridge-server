using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenApplyOrderDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; }
    public string UserAddress { get; set; }
    public string Status { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public ChainTokenInfoResultDto ChainTokenInfo { get; set; }
    public Dictionary<string, string> StatusChangedRecord { get; set; }
}

public class ChainTokenInfoResultDto
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
    public decimal DailyLimit { get; set; }
    public decimal RateLimit { get; set; }
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