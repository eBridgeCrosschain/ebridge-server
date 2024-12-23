using System.Collections.Generic;
using JetBrains.Annotations;

namespace AElf.CrossChainServer.TokenAccess;

public class ThirdTokenResultDto
{
    public string Code { get; set; }
    public string Message { get; set; }
    // public ThirdTokenListDto Data { get; set; }
    public List<ThirdTokenItemDto> Data { get; set; }
}

public class ThirdTokenListDto
{
    public int TotalCount { get; set; }
    public List<ThirdTokenItemDto> Items { get; set; }
}

public class ThirdTokenItemDto
{
    public string AelfChain { get; set; }
    public string AelfToken { get; set; }
    public string ThirdChain { get; set; }
    public string ThirdTokenName { get; set; }
    public string ThirdSymbol { get; set; }
    public string ThirdTokenImage { get; set; }
    public string ThirdContractAddress { get; set; }
    public string ThirdTotalSupply { get; set; }
}