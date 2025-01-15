using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.Chains
{
    public class ChainDto: EntityDto<string>
    {
        public string Name { get; set; }
        /// <summary>
        /// 0: AElf, 1: Evm, 2:TVM
        /// </summary>
        public BlockchainType Type { get; set; }
        public bool IsMainChain { get; set; }
        public int AElfChainId { get; set; }
        public string BlockChain { get; set; }
    }
}