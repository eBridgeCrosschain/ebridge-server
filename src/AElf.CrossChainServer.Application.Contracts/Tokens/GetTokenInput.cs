using System.ComponentModel.DataAnnotations;

namespace AElf.CrossChainServer.Tokens;

public class GetTokenInput
{
    [MaxLength(length:20)]
    public string ChainId { get; set; }
    [MaxLength(length:80)]
    public string Address { get; set; }
    [MaxLength(length:30)]
    public string Symbol { get; set; }
}