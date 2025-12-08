using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace AElf.CrossChainServer.TokenAccess;

public class GetTokenApplyOrderInput
{
    [Required] public string Symbol { get; set; }
    [CanBeNull] public string Id { get; set; }
    [CanBeNull] public string ChainId { get; set; }
}