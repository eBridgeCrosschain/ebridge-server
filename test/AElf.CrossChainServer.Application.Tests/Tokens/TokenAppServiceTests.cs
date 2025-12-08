using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace AElf.CrossChainServer.Tokens;

public class TokenAppServiceTests: CrossChainServerApplicationTestBase
{
    private readonly ITokenAppService _tokenAppService;

    public TokenAppServiceTests()
    {
        _tokenAppService = GetRequiredService<ITokenAppService>();
    }

    [Fact]
    public async Task CreateTest()
    {
        var getTokenInput = new GetTokenInput
        {
            Address = "Address_Address_Address_Address_Address_Address_Address_Address_Address_Address_Address_Address",
            Symbol = "SymbolSymbol_SymbolSymbol_SymbolSymbol",
            ChainId = "MainChain_AELF_AELF_AELF"
        };

        var exception = await Assert.ThrowsAsync<AbpValidationException>(async () => await _tokenAppService.GetAsync(getTokenInput));
        exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem.Contains("Address")));
        exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem.Contains("Symbol")));
        exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem.Contains("ChainId")));
        
        var tokenInput = new TokenCreateInput
        {
            Address = "Address",
            Decimals = 8,
            Symbol = "Symbol",
            ChainId = "MainChain_AELF"
        };

        var result = await _tokenAppService.CreateAsync(tokenInput);

        var token = await _tokenAppService.GetAsync(result.Id);
        token.Address.ShouldBe(tokenInput.Address);
        token.Decimals.ShouldBe(tokenInput.Decimals);
        token.Symbol.ShouldBe(tokenInput.Symbol);
        token.ChainId.ShouldBe(tokenInput.ChainId);
    }
}