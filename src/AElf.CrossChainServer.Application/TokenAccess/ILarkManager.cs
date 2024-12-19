using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using Newtonsoft.Json;
using Serilog;

namespace AElf.CrossChainServer.TokenAccess;

public interface ILarkManager
{
    Task SendMessageAsync(string message);
}

public class LarkManager : ILarkManager
{
    private readonly TokenAccessOptions _tokenAccessOptions;
    private readonly IHttpProvider _httpProvider;
    private ApiInfo _webHookUri => new(HttpMethod.Post, _tokenAccessOptions.LarkWebhook);
    public async Task SendMessageAsync(string message)
    {
        var payload = new
        {
            msg_type = "text",
            content = new
            {
                text = message
            }
        };
        var jsonPayload = JsonConvert.SerializeObject(payload);
        var response = await _httpProvider.InvokeAsync<string>("", _webHookUri, null, null, jsonPayload);
        Log.Information("Send lark message :{message}, response:{response}", message, response);
    }
}