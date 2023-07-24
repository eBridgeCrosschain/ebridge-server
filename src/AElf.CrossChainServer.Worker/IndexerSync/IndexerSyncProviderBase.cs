using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Settings;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Json;

namespace AElf.CrossChainServer.Worker.IndexerSync;

public abstract class IndexerSyncProviderBase : IIndexerSyncProvider, ITransientDependency
{
    protected readonly IGraphQLClient GraphQlClient;
    protected readonly ISettingManager SettingManager;
    protected readonly IJsonSerializer JsonSerializer;
    protected readonly IIndexerAppService IndexerAppService;
    protected readonly IChainAppService ChainAppService;

    public ILogger<IndexerSyncProviderBase> Logger { get; set; }

    private const int MaxRequestCount = 1000;

    protected IndexerSyncProviderBase(IGraphQLClient graphQlClient, ISettingManager settingManager,
        IJsonSerializer jsonSerializer, IIndexerAppService indexerAppService, IChainAppService chainAppService)
    {
        GraphQlClient = graphQlClient;
        SettingManager = settingManager;
        JsonSerializer = jsonSerializer;
        IndexerAppService = indexerAppService;
        ChainAppService = chainAppService;
        Logger = NullLogger<IndexerSyncProviderBase>.Instance;
    }

    public async Task ExecuteAsync(string chainId, int syncDelayHeight = 0, string typePrefix = null)
    {
        var syncHeight = await GetSyncHeightAsync(chainId, typePrefix);
        var currentIndexHeight = await GetIndexBlockHeightAsync(chainId);
        var endHeight = Math.Min(syncHeight + MaxRequestCount, currentIndexHeight - syncDelayHeight);
        var chain = await ChainAppService.GetAsync(chainId);
        var height = await HandleDataAsync(ChainHelper.ConvertChainIdToBase58(chain.AElfChainId), syncHeight + 1,
            endHeight);

        await SetSyncHeightAsync(chainId, typePrefix, height);
    }

    protected async Task<T> QueryDataAsync<T>(GraphQLRequest request)
    {
        var data = await GraphQlClient.SendQueryAsync<T>(request);
        if (data.Errors == null || data.Errors.Length == 0)
        {
            return data.Data;
        }

        Logger.LogError("Query indexer failed. errors: {Errors}",
            string.Join(",", data.Errors.Select(e => e.Message).ToList()));
        throw new Exception("Query indexer failed. ");
    }

    private async Task<long> GetIndexBlockHeightAsync(string chainId)
    {
        return await IndexerAppService.GetLatestIndexHeightAsync(chainId);
    }

    private async Task<long> GetSyncHeightAsync(string chainId, string typePrefix)
    {
        var settingKey = GetSettingKey(typePrefix);
        var setting = await SettingManager.GetOrNullAsync(chainId, settingKey);
        return setting == null ? 0 : long.Parse(setting);
    }

    private async Task SetSyncHeightAsync(string chainId, string typePrefix, long height)
    {
        var settingKey = GetSettingKey(typePrefix);
        await SettingManager.SetAsync(chainId, settingKey, height.ToString());
    }
    
    private string GetSettingKey(string typePrefix)
    {
        return string.IsNullOrWhiteSpace(typePrefix)? SyncType : $"{typePrefix}-{SyncType}";
    }

    protected abstract string SyncType { get; }

    protected abstract Task<long> HandleDataAsync(string aelfChainId, long startHeight, long endHeight);
}

