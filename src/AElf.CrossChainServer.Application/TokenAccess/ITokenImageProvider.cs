using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.TokenAccess;

public interface ITokenImageProvider
{
    Task<string> GetTokenImageAsync(string symbol);
}

public class TokenImageProvider : ITokenImageProvider, ITransientDependency
{
    private readonly IThirdUserTokenIssueRepository _thirdUserTokenIssueRepository;

    public TokenImageProvider(IThirdUserTokenIssueRepository thirdUserTokenIssueRepository)
    {
        _thirdUserTokenIssueRepository = thirdUserTokenIssueRepository;
    }

    public async Task<string> GetTokenImageAsync(string symbol)
    {
        var tokenInfo = await _thirdUserTokenIssueRepository.GetListAsync(o => o.Symbol == symbol);
        var tokenImageExistList = tokenInfo.Where(t => t.TokenImage != null).ToList();
        return tokenImageExistList.FirstOrDefault()?.TokenImage;
    }
}