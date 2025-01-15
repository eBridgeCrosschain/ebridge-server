using System;

namespace AElf.CrossChainServer.TokenAccess;

public class AddThirdUserTokenIssueInfoIndexInput
{
    public Guid Id { get; set; }
    public string Address { get; set; }
    public string WalletAddress { get; set; }
    public string Symbol { get; set; }
    public string ChainId { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public string TokenName { get; set; }
    public string TokenImage { get; set; }
    public string OtherChainId { get; set; }
    public string TotalSupply { get; set; }
    public string ContractAddress { get; set; }
    public string BindingId { get; set; }
    public string ThirdTokenId { get; set; }
    public string Status { get; set; }
}