using System;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.TokenAccess.UserTokenOwner;

public class UserTokenOwnerBase : CrossChainServerEntity<Guid>
{
    [Keyword] public string Address { get; set; }
    public string TokenName { get; set; }
    [Keyword] public string Symbol { get; set; }
    public int Decimals { get; set; }
    [Text(Index = false)] public string Icon { get; set; }
    [Keyword] public string Owner { get; set; }
    [Keyword] public string ChainId { get; set; }

    public decimal TotalSupply { get; set; }

    // liquidity from awaken
    public string LiquidityInUsd { get; set; }

    // holders from scan
    public int Holders { get; set; }

    [Keyword] public string PoolAddress { get; set; }

    // multiToken contract address
    [Keyword] public string ContractAddress { get; set; }

    // can find - issued (include main chain and dapp chain)
    public string Status { get; set; }
}