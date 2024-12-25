using System;
using AElf.CrossChainServer.Entities;

namespace AElf.CrossChainServer.TokenAccess;

public class UserTokenOwner : CrossChainServerEntity<Guid>
{
    public string Address { get; set; }
    public string TokenName { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string Icon { get; set; }
    public string Owner { get; set; }
    public string ChainId { get; set; }
    public decimal TotalSupply { get; set; }
    public string LiquidityInUsd { get; set; }
    public int Holders { get; set; }
    public string PoolAddress { get; set; }
    public string ContractAddress { get; set; }
    public string Status { get; set; }
}