using System.ComponentModel.DataAnnotations;

namespace AElf.CrossChainServer.TokenAccess;

public class PrepareBindIssueInput
{
    [Required] public string Address { get; set; }
    [Required] public string Symbol { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public string ContractAddress { get; set; }
    [Required] public string Supply { get; set; }
}