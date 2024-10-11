using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Worker;

public interface IBridgeContractSyncService
{
    Task ExecuteAsync();
}

public class BridgeContractSyncService : IBridgeContractSyncService, ITransientDependency
{
    private readonly BridgeContractSyncOptions _bridgeContractSyncOptions;
    private readonly ITokenAppService _tokenAppService;
    private readonly IEnumerable<IBridgeContractSyncProvider> _bridgeContractSyncProviders;
    
    public BridgeContractSyncService(IOptionsSnapshot<BridgeContractSyncOptions> bridgeContractSyncOptions,
        ITokenAppService tokenAppService, IEnumerable<IBridgeContractSyncProvider> bridgeContractSyncProviders)
    {
        _bridgeContractSyncOptions = bridgeContractSyncOptions.Value;
        _tokenAppService = tokenAppService;
        _bridgeContractSyncProviders = bridgeContractSyncProviders.ToList();
        
    }
    
    public async Task ExecuteAsync()
    {
        foreach (var (key, value) in _bridgeContractSyncOptions.Tokens)
        {
            try
            {
                var chainId = key;
                foreach (var (transferType, tokenInfos) in value)
                {
                    var tokenIds = new List<Guid>();
                    var targetChainIds = new List<string>();
                    foreach (var token in tokenInfos)
                    {
                        var tokenInfo = await _tokenAppService.GetAsync(new GetTokenInput
                        {
                            Address = token.Address,
                            Symbol = token.Symbol,
                            ChainId = chainId
                        });

                        tokenIds.Add(tokenInfo.Id);
                        targetChainIds.Add(token.TargetChainId);
                    }

                    var provider = _bridgeContractSyncProviders.First(o => o.Type == transferType);
                    await provider.SyncAsync(chainId, tokenIds, targetChainIds);
                }
            }
            catch (Exception ex)
            {
                Log.ForContext("chainId",key).Error(ex,"Bridge contract sync failed.");
            }
        }
    }
}