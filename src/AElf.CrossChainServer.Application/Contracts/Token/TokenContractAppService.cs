using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace AElf.CrossChainServer.Contracts.Token;

[RemoteService(IsEnabled = false)]
public class TokenContractAppService: CrossChainServerAppService,ITokenContractAppService
{
    private readonly ITokenContractProvider _tokenContractProvider;
    private readonly AccountOptions _accountOptions;
    private readonly TokenContractOptions _tokenContractOptions;
    
    public TokenContractAppService(
        IOptionsSnapshot<AccountOptions> accountOptions,
        IOptionsSnapshot<TokenContractOptions> tokenContractOptions,
        ITokenContractProvider tokenContractProvider)
    {
        _tokenContractProvider = tokenContractProvider;
        _accountOptions = accountOptions.Value;
        _tokenContractOptions = tokenContractOptions.Value;
    }
    
    public async Task<string> CrossChainReceiveTokenAsync(string chainId, int fromChainId, long parentChainHeight, string transferTransaction,
        MerklePath merklePath)
    {
        var contractAddress = _tokenContractOptions.ContractAddresses[chainId];
        var privateKey = _accountOptions.PublicKeys[chainId];
        return await _tokenContractProvider.CrossChainReceiveTokenAsync(chainId, contractAddress, privateKey,
            fromChainId, parentChainHeight, transferTransaction, merklePath);
    }
}