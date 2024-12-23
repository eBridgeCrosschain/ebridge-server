using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenAccess;

public class GetPoolListInput : PagedAndSortedResultRequestDto
{
    [CanBeNull] public string Address { get; set; }
}