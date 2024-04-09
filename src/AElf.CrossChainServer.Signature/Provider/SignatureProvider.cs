using System.Text;
using AElf.CrossChainServer.Signature.Options;
using AElf.CrossChainServer.Signature.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using IHttpProvider = AElf.CrossChainServer.Signature.Http.IHttpProvider;

namespace AElf.CrossChainServer.Signature.Provider;

public interface ISignatureProvider
{
    Task<string> SignTxMsg(string publicKey, string hexMsg);
}

public class SignatureProvider : ISignatureProvider, ISingletonDependency
{
    private const int DefaultErrorCode = 50000;
    private const string DefaultErrorReason = "Assert failed";

    private const string GetSignatureUri = "/api/app/signature";
    private readonly IHttpProvider _httpProvider;
    private readonly IOptionsMonitor<SignatureServerOptions> _signatureServerOptions;


    public SignatureProvider(IOptionsMonitor<SignatureServerOptions> signatureOptions, IHttpProvider httpProvider)
    {
        _httpProvider = httpProvider;
        _signatureServerOptions = signatureOptions;
    }

    private string Uri(string path) => _signatureServerOptions.CurrentValue.BaseUrl.TrimEnd('/') + path;

    public async Task<string> SignTxMsg(string publicKey, string hexMsg)
    {
        var signatureSend = new SendSignatureDto
        {
            PublicKey = publicKey,
            HexMsg = hexMsg,
        };

        var resp = await _httpProvider.InvokeAsync<CommonResponseDto<SignResponseDto>>(HttpMethod.Post,
            Uri(GetSignatureUri),
            body: JsonConvert.SerializeObject(signatureSend)
        );
        IsTrue(resp?.Success ?? false, DefaultErrorCode, "Signature response failed");
        IsTrue(!(resp!.Data?.Signature).IsNullOrEmpty(), DefaultErrorCode, "Signature response empty");
        return resp.Data!.Signature;
    }
    
    private static void IsTrue(bool expression, int code = DefaultErrorCode, string? reason = DefaultErrorReason, params object?[] args)
    {
        if (!expression)
        {
            throw new UserFriendlyException(Format(reason, args), code.ToString());
        }
    }
    
    private static string Format(string template, params object[] values)
    {
        if (values == null || values.Length == 0)
            return template;
        
        var valueIndex = 0;
        var start = 0;
        int placeholderStart;
        var result = new StringBuilder();

        while ((placeholderStart = template.IndexOf('{', start)) != -1)
        {
            var placeholderEnd = template.IndexOf('}', placeholderStart);
            if (placeholderEnd == -1) break;

            result.Append(template, start, placeholderStart - start);

            if (valueIndex < values.Length)
                result.Append(values[valueIndex++] ?? "null");
            else
                result.Append(template, placeholderStart, placeholderEnd - placeholderStart + 1);

            start = placeholderEnd + 1;
        }

        if (start < template.Length)
            result.Append(template, start, template.Length - start);

        return result.ToString();
    }
}

public class SendSignatureDto
{
    public string PublicKey { get; set; }
    public string HexMsg { get; set; }
}

public class SignResponseDto
{
    public string Signature { get; set; }
}