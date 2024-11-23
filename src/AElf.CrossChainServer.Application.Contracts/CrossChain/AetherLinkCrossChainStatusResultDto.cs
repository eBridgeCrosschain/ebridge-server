namespace AElf.CrossChainServer.CrossChain;

public class AetherLinkCrossChainStatusResultDto
{
    public int Status { get; set; }
}

public class AetherLinkCommonResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}