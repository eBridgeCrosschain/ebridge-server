using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenPool;

public class GetPoolListInput : PagedAndSortedResultRequestDto
{
    [CanBeNull] public string Addresses { get; set; }
}