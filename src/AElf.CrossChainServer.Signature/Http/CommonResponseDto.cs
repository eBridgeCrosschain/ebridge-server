using JetBrains.Annotations;
using Volo.Abp;

namespace AElf.CrossChainServer.Signature.Http;

public class CommonResponseDto<T>
{
    private const string SuccessCode = "20000";
    private const string CommonErrorCode = "50000";

    public string Code { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    
    public bool Success => Code == SuccessCode;

}