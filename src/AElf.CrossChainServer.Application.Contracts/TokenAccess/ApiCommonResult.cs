namespace AElf.CrossChainServer.TokenAccess;

public class ApiCommonResult<T>
{
    public string Code { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}