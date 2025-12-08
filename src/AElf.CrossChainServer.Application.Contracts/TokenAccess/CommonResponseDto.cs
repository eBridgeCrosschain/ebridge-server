using System;
using JetBrains.Annotations;
using Volo.Abp;

namespace AElf.CrossChainServer.TokenAccess;

public class CommonResponseDto<T> where T : class
{
    private const string SuccessCode = "20000";
    private const string CommonErrorCode = "50000";
    public string Code { get; set; }
    public object Data { get; set; }
    public string Message { get; set; } = string.Empty;
    private readonly bool _success;
    bool Success => Code == SuccessCode;
    private readonly T _value;
    public T Value => Data as T;

    public CommonResponseDto()
    {
        Code = SuccessCode;
    }

    public CommonResponseDto(T data)
    {
        Code = SuccessCode;
        Data = data;
    }

    public CommonResponseDto<T> Error(string code, string message)
    {
        Code = code;
        Message = message;
        return this;
    }

    public CommonResponseDto<T> Error(string message)
    {
        Code = CommonErrorCode;
        Message = message;
        return this;
    }

    public CommonResponseDto<T> Error(Exception e, [CanBeNull] string message = null)
    {
        return e is UserFriendlyException ufe
            ? Error(ufe.Code, message ?? ufe.Message)
            : Error(CommonErrorCode, message ?? e.Message);
    }
}