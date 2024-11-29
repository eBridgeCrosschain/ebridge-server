using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.TokenAccess;

public class GetTokenApplyOrderListInput : PagedResultRequestDto
{
    public string Address { get; set; }
}