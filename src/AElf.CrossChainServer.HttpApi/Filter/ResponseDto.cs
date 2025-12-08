using System;

namespace AElf.CrossChainServer.Filter;
public class ResponseDto
{
    public string Code { get; set; }

    public object Data { get; set; }

    public string Message { get; set; } = string.Empty;
}