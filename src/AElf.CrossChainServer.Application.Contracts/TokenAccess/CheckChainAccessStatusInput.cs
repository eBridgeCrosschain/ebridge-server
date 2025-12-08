using System.ComponentModel.DataAnnotations;

namespace AElf.CrossChainServer.TokenAccess;

public class CheckChainAccessStatusInput
{
    [Required] public string Symbol { get; set; }
}