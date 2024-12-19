using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AElf.CrossChainServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Chain")]
    [Route("api/app/chains")]
    public class ChainController : CrossChainServerController
    {
        private readonly IChainAppService _chainAppService;

        public ChainController(IChainAppService chainAppService)
        {
            _chainAppService = chainAppService;
        }

        [Authorize]
        [HttpGet]
        public Task<ListResultDto<ChainDto>> GetChainsAsync(GetChainsInput input)
        {
            return _chainAppService.GetListAsync(input);
        }
    }
}