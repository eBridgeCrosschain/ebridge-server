using System.ComponentModel.DataAnnotations;

namespace AElf.CrossChainServer.TokenAccess;

public class CommitAddLiquidityInput
{
    [Required] public string OrderId { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public decimal Amount { get; set; }
}