using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.CrossChain;

public interface IAetherLinkProvider
{
    Task<int> CalculateCrossChainProgressAsync(AetherLinkCrossChainStatusInput input);
}

public class AetherLinkProvider : IAetherLinkProvider
{
    private readonly IHttpProvider _httpProvider;
    private readonly AetherLinkOption _aetherLinkOption;
    private ApiInfo _getCrossChainStatusUri => new(HttpMethod.Get, _aetherLinkOption.CrossChainStatusUri);

    public AetherLinkProvider(IHttpProvider httpProvider, IOptionsSnapshot<AetherLinkOption> aetherLinkOption)
    {
        _httpProvider = httpProvider;
        _aetherLinkOption = aetherLinkOption.Value;
    }

    public async Task<int> CalculateCrossChainProgressAsync(AetherLinkCrossChainStatusInput input)
    {
        var result = await _httpProvider.InvokeAsync<AetherLinkCommonResult<AetherLinkCrossChainStatusResultDto>>(_aetherLinkOption.BaseUrl, 
            _getCrossChainStatusUri, null,ConvertInputToDictionary(input));
        if (!result.Success || result.Data == null)
        {
            Log.Error("Get status from aetherlink failed.");
            return 0;
        }

        var crossChainStatus = result.Data.Status;
        Log.Debug("Get cross chain status {crossChainStatus} from aetherlink {traceId},{txId}.", crossChainStatus,input.TraceId,input.TransactionId);
        return 25 * (1 + crossChainStatus);
    }

    private Dictionary<string, string> ConvertInputToDictionary(AetherLinkCrossChainStatusInput input)
    {
        var jsonString = JsonConvert.SerializeObject(input);
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
    }
}