using System.Net.Http;
using System.Threading.Tasks;
using AElf.CrossChainServer.HttpClient;
using AElf.CrossChainServer.Notify;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Volo.Abp.DependencyInjection;


namespace AElf.CrossChainServer.TokenAccess;

public interface ILarkRobotNotifyProvider
{
    Task<bool> SendMessageAsync(NotifyRequest notifyRequest);
}

public class LarkRobotNotifyProvider : ILarkRobotNotifyProvider,ITransientDependency
{
    private readonly LarkNotifyTemplateOptions _larkNotifyTemplateOptions;
    private readonly IHttpProvider _httpProvider;

    public LarkRobotNotifyProvider(IOptionsSnapshot<LarkNotifyTemplateOptions> larkNotifyTemplateOptions,
        IHttpProvider httpProvider)
    {
        _larkNotifyTemplateOptions = larkNotifyTemplateOptions.Value;
        _httpProvider = httpProvider;
    }

    public async Task<bool> SendMessageAsync(NotifyRequest notifyRequest)
    {
        var templateExists =
            _larkNotifyTemplateOptions.Templates.TryGetValue(notifyRequest.Template, out var templates);
        if (!templateExists)
        {
            Log.Error("Template {Template} not found", notifyRequest.Template);
            return false;
        }

        var notifyContent = StringHelper.ReplaceObjectWithDict(templates.LarkGroup, notifyRequest.Params);
        var cardMessage = LarkMessageBuilder.CardMessageBuilder()
            .WithSignature(notifyContent.Secret)
            .WithTitle(notifyContent.Title, notifyContent.TitleTemplate)
            .AddMarkdownContents(notifyContent.Contents)
            .Build();

        var resp = await _httpProvider.InvokeAsync<LarkRobotResponse<Empty>>(HttpMethod.Post,
            notifyContent.WebhookUrl,
            body: JsonConvert.SerializeObject(cardMessage, HttpProvider.DefaultJsonSettings));
        return resp.Success;
    }
}

public class LarkRobotResponse<T>
{
    public int Code { get; set; }
    public T Data { get; set; }
    public string Msg { get; set; }
    public bool Success => Code == 0;
}