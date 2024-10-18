using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.ExceptionHandler;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;

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
            var chainId = key;
                foreach (var (transferType, tokenInfos) in value)
                {
                    var tokenIds = new List<Guid>();
                    var targetChainIds = new List<string>();
                    foreach (var token in tokenInfos)
                    {
                        var tokenInfo = await GetTokenInfoAsync(chainId, token.Address, token.Symbol);
                        if (tokenInfo == null)
                        {
                            continue;
                        }
                        tokenIds.Add(tokenInfo.Id);
                        targetChainIds.Add(token.TargetChainId);
                    }

                    var provider = _bridgeContractSyncProviders.First(o => o.Type == transferType);
                    await provider.SyncAsync(chainId, tokenIds, targetChainIds);
                }
        }
    }
    
    [ExceptionHandler(typeof(Exception), typeof(EntityNotFoundException),Message = "Token not found.",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException))]
    public virtual async Task<TokenDto> GetTokenInfoAsync(string chainId, string address, string symbol)
    {
        var tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
        {
            Address = address,
            Symbol = symbol,
            ChainId = chainId
        });
        return tokenDto;
    }
}