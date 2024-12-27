using System.Collections.Generic;

namespace AElf.CrossChainServer.TokenAccess;

public class TriggerOrderStatusChangeInput
{
    public string OrderId { get; set; }
    public List<ChainTokenDto> ChainIdTokenInfos { get; set; }
}

public class ChainTokenDto
{
    public string ChainId { get; set; }
    public string TokenContractAddress { get; set; }
    public int TokenDecimals { get; set; }
}