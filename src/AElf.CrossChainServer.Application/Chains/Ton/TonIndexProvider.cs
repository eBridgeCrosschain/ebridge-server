using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElf.CrossChainServer.Chains.Ton;

public interface ITonIndexProvider
{
    Task<List<TonTransactionDto>> GetTonTransactionAsync(GetTonTransactionInput input);
}

public class TonIndexProvider : TonClientProvider, ITonIndexProvider, ITransientDependency
{
    private readonly IObjectMapper _objectMapper;

    public TonIndexProvider(ITonIndexClientProvider indexClientProvider,
        IHttpClientFactory clientFactory, IObjectMapper objectMapper) : base(indexClientProvider,
        clientFactory)
    {
        _objectMapper = objectMapper;
    }

    public async Task<List<TonTransactionDto>> GetTonTransactionAsync(GetTonTransactionInput input)
    {
        // var path =
        //     $"/v2/blockchain/accounts/{input.ContractAddress}/transactions?after_lt={input.LatestTransactionLt}&limit=100&sort=asc";
        var path =
            $"/transactions?account={input.ContractAddress}&start_lt={input.LatestTransactionLt}&limit=100&offset=0&sort=asc";
        var tonIndexTransactions = await IndexClientProvider.GetAsync<TonIndexTransactions>(input.ChainId, path);

        return _objectMapper.Map<List<TonIndexTransaction>, List<TonTransactionDto>>(tonIndexTransactions.Transactions);
    }
}