using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AElf.CrossChainServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CrossChainIndexing")]
[Route("api/app/cross-chain-indexing")]
public class CrossChainIndexingController
{
    private readonly ICrossChainIndexingInfoAppService _crossChainIndexingInfoAppService;

    public CrossChainIndexingController(ICrossChainIndexingInfoAppService crossChainIndexingInfoAppService)
    {
        _crossChainIndexingInfoAppService = crossChainIndexingInfoAppService;
    }

    /// <summary>
    /// Calculate cross chain indexing progress.
    /// </summary>
    /// <param name="fromChainId"></param>
    /// <param name="toChainId"></param>
    /// <param name="height">the height to be indexed.</param>
    /// <returns></returns>
    [HttpGet]
    [Route("progress")]
    public Task<int> CalculateProgressAsync(string fromChainId, string toChainId, long height)
    {
        return _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync(fromChainId, toChainId, height);
    }
}