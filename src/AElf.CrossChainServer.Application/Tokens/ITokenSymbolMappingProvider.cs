using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Tokens;

public interface ITokenSymbolMappingProvider
{
    string GetMappingSymbol(string fromChainId, string toChainId, string symbol);
}

public class TokenSymbolMappingProvider : ITokenSymbolMappingProvider, ITransientDependency
{
    private readonly TokenSymbolMappingOptions _tokenSymbolMappingOptions;

    public TokenSymbolMappingProvider(IOptionsSnapshot<TokenSymbolMappingOptions> tokenSymbolMappingOptions)
    {
        _tokenSymbolMappingOptions = tokenSymbolMappingOptions.Value;
    }

    public string GetMappingSymbol(string fromChainId, string toChainId, string symbol)
    {
        if (_tokenSymbolMappingOptions.Mapping.TryGetValue(fromChainId, out var items))
        {
            if (items.TryGetValue(toChainId, out var mappingSymbols))
            {
                if (mappingSymbols.TryGetValue(symbol, out var mappingSymbol))
                {
                    return mappingSymbol;
                }
            }
        }

        return symbol;
    }
}