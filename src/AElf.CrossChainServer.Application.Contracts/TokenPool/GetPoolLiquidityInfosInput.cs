using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenPool;

public class GetPoolLiquidityInfosInput : PagedAndSortedResultRequestDto
{
    [CanBeNull] public string ChainId { get; set; }
    [CanBeNull] public string Token { get; set; }
}