namespace AElf.CrossChainServer.TokenAccess;

public class PrepareBindingResultDto
{
    public string Code { get; set; }
    public string Message { get; set; }
    public UserTokenBindingDto Data { get; set; }
}

public class PrepareBindingInput
{
    public string Address { get; set; }
    public string AelfToken { get; set; }
    public string AelfChain { get; set; }
    public ThirdTokenDto ThirdTokens { get; set; }
    public string Signature { get; set; }
}

public class ThirdTokenDto
{
    public string TokenName { get; set; }
    public string Symbol { get; set; }
    public string TokenImage { get; set; }
    public string TotalSupply { get; set; }
    public string ThirdChain { get; set; }
    public string Owner { get; set; }
    public string ContractAddress { get; set; }
}

public class BindingInput
{
    public string BindingId { get; set; }
    public string ThirdTokenId { get; set; }
    public string Signature { get; set; }
}