using System;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.CrossChainServer.Tokens;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.CrossChainServer.Chains.Ton
{
    public class TonClientProvider : IBlockchainClientProvider
    {
        protected readonly ITonIndexClientProvider IndexClientProvider;
        private readonly IHttpClientFactory _clientFactory;

        public TonClientProvider(ITonIndexClientProvider indexClientProvider,
            IHttpClientFactory clientFactory)
        {
            IndexClientProvider = indexClientProvider;
            _clientFactory = clientFactory;
        }

        public BlockchainType ChainType { get; } = BlockchainType.Tvm;

        public async Task<TokenDto> GetTokenAsync(string chainId, string address, string symbol)
        {
            var path = $"/jetton/masters?address={address}&limit=1&offset=0";
            var jettonMaster = await IndexClientProvider.GetAsync<JettonMasterDto>(chainId, path);
            if (jettonMaster.JettonMasters[0].JettonContent.Uri != null)
            {
                var jetton = await GetJettonAsync(jettonMaster.JettonMasters[0].JettonContent.Uri);
                symbol = jetton.Symbol;
            }
            else
            {
                symbol = jettonMaster.JettonMasters[0].JettonContent.Symbol;
            }
            
            return new TokenDto
            {
                ChainId = chainId,
                Address = address,
                Decimals = int.Parse(jettonMaster.JettonMasters[0].JettonContent.Decimals),
                Symbol = symbol
            };
        }

        private async Task<JettonDto> GetJettonAsync(string uri)
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync(uri);
            return await response.Content.DeserializeSnakeCaseAsync<JettonDto>();
        }

        public Task<BlockDto> GetBlockByHeightAsync(string chainId, long height, bool includeTransactions = false)
        {
            throw new NotImplementedException();
        }

        public async Task<long> GetChainHeightAsync(string chainId)
        {
            throw new NotImplementedException();
        }
        
        public async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
        {
            throw new NotImplementedException();
        }
        
        public Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string transactionId)
        {
            throw new NotImplementedException();
        }

        public Task<MerklePathDto> GetMerklePathAsync(string chainId, string txId)
        {
            throw new NotImplementedException();
        }

        public Task<FilterLogsDto> GetContractLogsAsync(string chainId, string contractAddress, long startHeight, long endHeight)
        {
            throw new NotImplementedException();
        }

        public Task<FilterLogsAndEventsDto<TEventDTO>> GetContractLogsAndParseAsync<TEventDTO>(string chainId, string contractAddress, long startHeight, long endHeight,
            string logSignature) where TEventDTO : IEventDTO, new()
        {
            throw new NotImplementedException();
        }
    }
}