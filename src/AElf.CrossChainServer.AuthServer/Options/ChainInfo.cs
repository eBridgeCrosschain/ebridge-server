namespace AElf.CrossChainServer.Auth.Options;

public class ChainInfo
{
    public string ChainId { get; set; }
    public string BaseUrl { get; set; }
    public string ContractAddress { get; set; }
    public string ContractAddress2 { get; set; }
    public string PublicKey { get; set; }
    public bool IsMainChain { get; set; }
}