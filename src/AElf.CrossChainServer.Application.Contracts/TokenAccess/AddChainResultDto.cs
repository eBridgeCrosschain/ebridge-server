using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class AddChainResultDto
{
    public List<AddChainDto> ChainList { get; set; }
}

public class AddChainDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
}