using Nest;

namespace AElf.CrossChainServer.Entities
{
    public class MultiChainEntity<TKey> : CrossChainServerEntity<TKey>, IMultiChain
    {
        [Keyword]
        public virtual string ChainId { get; set; }
    }
}