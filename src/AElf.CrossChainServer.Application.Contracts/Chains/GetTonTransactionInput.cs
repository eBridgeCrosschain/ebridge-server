namespace AElf.CrossChainServer.Chains;

public class GetTonTransactionInput
{
    public string ChainId { get; set; }
    public string ContractAddress { get; set; }
    public string LatestTransactionLt { get; set; }
}