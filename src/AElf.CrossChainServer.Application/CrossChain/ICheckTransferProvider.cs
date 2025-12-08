using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.ExceptionHandler;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nethereum.Util;
using Serilog;
using Volo.Abp.Domain.Entities;

namespace AElf.CrossChainServer.CrossChain;

public interface ICheckTransferProvider
{
    Task<bool> CheckTokenExistAsync(string fromChainId, string toChainId, Guid tokenId);
}

public class CheckTransferProvider : ICheckTransferProvider
{
    private readonly IIndexerCrossChainLimitInfoService _indexerCrossChainLimitInfoService;
    private readonly IChainAppService _chainAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly ITokenSymbolMappingProvider _tokenSymbolMappingProvider;


    public CheckTransferProvider(
        IIndexerCrossChainLimitInfoService indexerCrossChainLimitInfoService, IChainAppService chainAppService,
        ITokenAppService tokenAppService, ITokenSymbolMappingProvider tokenSymbolMappingProvider)
    {
        _indexerCrossChainLimitInfoService = indexerCrossChainLimitInfoService;
        _chainAppService = chainAppService;
        _tokenAppService = tokenAppService;
        _tokenSymbolMappingProvider = tokenSymbolMappingProvider;
    }

    [ExceptionHandler(typeof(Exception), Message = "Check transfer: get token info error.",
        ReturnDefault = ReturnDefault.Default,LogTargets = new[]{"fromChainId","toChainId","tokenId"})]
    public virtual async Task<TokenDto> GetTokenInfoAsync(string fromChainId, string toChainId, Guid tokenId)
    {
        var transferToken = await _tokenAppService.GetAsync(tokenId);
        var symbol =
            _tokenSymbolMappingProvider.GetMappingSymbol(fromChainId, toChainId, transferToken.Symbol);

        var token = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = toChainId,
            Symbol = symbol
        });
        return token;
    }

    public async Task<bool> CheckTokenExistAsync(string fromChainId, string toChainId, Guid tokenId)
    {
        var token = await GetTokenInfoAsync(fromChainId, toChainId, tokenId);
        return token != null;
    }
}