using AElf.CrossChainServer.Entities;
using JetBrains.Annotations;
using Nest;

namespace AElf.CrossChainServer.Chains
{
    public class Chain : CrossChainServerEntity<string>
    {
        [Keyword]
        [NotNull] 
        public string Name { get; set; }

        public BlockchainType Type { get; set; }

        public bool IsMainChain { get; set; }

        public int AElfChainId { get; set; }

        [Keyword]
        public string BlockChain { get; set; }
    }
}