using System.Threading.Tasks;
using AElf.CrossChainServer.Tokens;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AElf.CrossChainServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Token")]
    [Route("api/app/tokens")]
    public class TokenController : CrossChainServerController
    {
        private readonly ITokenAppService _tokenAppService;

        public TokenController(ITokenAppService tokenAppService)
        {
            _tokenAppService = tokenAppService;
        }

        [HttpGet]
        public Task<TokenDto> GetAsync(GetTokenInput input)
        {
            return _tokenAppService.GetAsync(input);
        }
    }
}