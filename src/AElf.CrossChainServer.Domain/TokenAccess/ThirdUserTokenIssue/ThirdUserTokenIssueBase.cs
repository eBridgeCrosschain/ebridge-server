using System;
using AElf.CrossChainServer.Entities;
using Nest;

namespace AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;

public class ThirdUserTokenIssueBase : CrossChainServerEntity<Guid>
{
    [Keyword] public string Address { get; set; }
    // evm chain address
    [Keyword] public string WalletAddress { get; set; }
    [Keyword] public string Symbol { get; set; }
    // aelf chain id
    [Keyword] public string ChainId { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    [Keyword] public string TokenName { get; set; }
    [Text(Index = false)] public string TokenImage { get; set; }
    [Keyword] public string OtherChainId { get; set; }
    public string TotalSupply { get; set; }
    public string ContractAddress { get; set; }
    [Keyword] public string BindingId { get; set; }
    [Keyword] public string ThirdTokenId { get; set; }
    public string Status { get; set; }
}