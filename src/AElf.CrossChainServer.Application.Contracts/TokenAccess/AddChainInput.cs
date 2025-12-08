using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AElf.CrossChainServer.TokenAccess;

public class AddChainInput
{
    [Required] public string Symbol { get; set; }
    public List<string> ChainIds { get; set; }
}