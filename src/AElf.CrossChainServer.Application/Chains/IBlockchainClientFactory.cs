namespace AElf.CrossChainServer.Chains
{
    public interface IBlockchainClientFactory<T> 
        where T : class
    {
        T GetClient(string chainId);
    }
}