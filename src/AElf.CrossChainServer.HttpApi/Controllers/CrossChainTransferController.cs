using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CrossChainTransfer")]
[Route("api/app/cross-chain-transfers")]
public class CrossChainTransferController
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;

    public CrossChainTransferController(ICrossChainTransferAppService crossChainTransferAppService)
    {
        _crossChainTransferAppService = crossChainTransferAppService;
    }
    

    [HttpGet]
    public Task<PagedResultDto<CrossChainTransferIndexDto>> GetListAsync(GetCrossChainTransfersInput input)
    {
        return _crossChainTransferAppService.GetListAsync(input);
    }
    
    [HttpGet]
    [Route("status")]
    public Task<ListResultDto<CrossChainTransferStatusDto>> GetStatusAsync(GetCrossChainTransferStatusInput input)
    {
        return _crossChainTransferAppService.GetStatusAsync(input);
    }
}