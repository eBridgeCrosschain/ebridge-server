using System;
using System.Threading.Tasks;

namespace AElf.CrossChainServer.Tokens
{
    public interface ITokenAppService
    {
        Task<TokenDto> GetAsync(Guid id);
        Task<TokenDto> GetAsync(GetTokenInput input);
        Task<TokenDto> CreateAsync(TokenCreateInput input);
    }
}