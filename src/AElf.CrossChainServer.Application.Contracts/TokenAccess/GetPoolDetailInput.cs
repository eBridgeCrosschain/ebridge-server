using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace AElf.CrossChainServer.TokenAccess;

public class GetPoolDetailInput
{
    [CanBeNull] public string Address { get; set; }
    [Required] public string Token { get; set; }
    [Required] public string ChainId { get; set; }
}