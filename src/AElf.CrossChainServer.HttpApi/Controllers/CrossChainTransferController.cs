using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Indexer;
using Asp.Versioning;
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

    public CrossChainTransferController(ICrossChainTransferAppService crossChainTransferAppService,IIndexerAppService indexerAppService)
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