using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Notify;
using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;
using AElf.CrossChainServer.TokenAccess.UserTokenAccess;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Tokens;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace AElf.CrossChainServer.TokenAccess;

public class TokenAccessAppServiceTests : CrossChainServerApplicationTestBase
{
    private readonly ITokenAccessAppService _tokenAccessAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly IUserAccessTokenInfoRepository _userAccessTokenInfoRepository;
    private readonly IThirdUserTokenIssueRepository _thirdUserTokenIssueRepository;
    private readonly ITokenApplyOrderRepository _tokenApplyOrderRepository;
    private readonly ICrossChainUserRepository _crossChainUserRepository;
    private readonly ITokenInfoCacheProvider _tokenInfoCacheProvider;
    private readonly IScanProvider _scanProvider;
    private readonly IAwakenProvider _awakenProvider;

    public TokenAccessAppServiceTests()
    {
        _tokenAccessAppService = GetRequiredService<ITokenAccessAppService>();
        _tokenAppService = GetRequiredService<ITokenAppService>();
        _userAccessTokenInfoRepository = GetRequiredService<IUserAccessTokenInfoRepository>();
        _thirdUserTokenIssueRepository = GetRequiredService<IThirdUserTokenIssueRepository>();
        _tokenApplyOrderRepository = GetRequiredService<ITokenApplyOrderRepository>();
        _crossChainUserRepository = GetRequiredService<ICrossChainUserRepository>();
        _tokenInfoCacheProvider = GetRequiredService<ITokenInfoCacheProvider>();
        _scanProvider = GetRequiredService<IScanProvider>();
        _awakenProvider = GetRequiredService<IAwakenProvider>();
    }

    [Fact]
    public async Task GetTokenConfigAsync_Should_Return_Config()
    {
        // Act
        var result = await _tokenAccessAppService.GetTokenConfigAsync(new GetTokenConfigInput { Symbol = "ELF" });

        // Assert
        result.ShouldNotBeNull();
        // LiquidityInUsd is a string type, only check it's not null
        result.LiquidityInUsd.ShouldNotBeNull();
        result.Holders.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetTokenWhitelistAsync_Should_Return_Whitelist()
    {
        // Act
        var result = await _tokenAccessAppService.GetTokenWhitelistAsync();

        // Assert
        result.ShouldNotBeNull();
        // Whitelist may be empty, but should return a non-null object
    }

    [Fact]
    public async Task GetTokenPriceAsync_Should_Return_Price()
    {
        // Arrange
        var input = new GetTokenPriceInput
        {
            Symbol = "ELF",
            Amount = 100
        };

        // Act
        var result = await _tokenAccessAppService.GetTokenPriceAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.Symbol.ShouldBe("ELF");
        (result.TokenAmountInUsd >= 0).ShouldBeTrue();
    }

    [Fact]
    public async Task GetAvailableTokensAsync_Empty_When_Not_Authenticated()
    {
        // Assume current user is not authenticated

        // Act
        var result = await _tokenAccessAppService.GetAvailableTokensAsync(new GetAvailableTokensInput());

        // Assert
        result.ShouldNotBeNull();
        result.TokenList.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommitTokenAccessInfoAsync_Should_Throw_When_Not_Authenticated()
    {
        // Arrange
        var input = new UserTokenAccessInfoInput
        {
            Symbol = "ELF",
            Email = "test@example.com",
            OfficialWebsite = "https://example.com",
            PersonName = "Test Person",
            Title = "Test Title"
        };

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => 
            await _tokenAccessAppService.CommitTokenAccessInfoAsync(input));
    }

    [Fact]
    public async Task CheckChainAccessStatusAsync_Should_Throw_When_Not_Authenticated()
    {
        // Arrange
        var input = new CheckChainAccessStatusInput
        {
            Symbol = "ELF"
        };

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => 
            await _tokenAccessAppService.CheckChainAccessStatusAsync(input));
    }

    [Fact]
    public async Task AddChainAsync_Should_Throw_When_Not_Authenticated()
    {
        // Arrange
        var input = new AddChainInput
        {
            Symbol = "ELF",
            ChainIds = new List<string> { "Ethereum" }
        };

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => 
            await _tokenAccessAppService.AddChainAsync(input));
    }

    [Fact]
    public async Task PrepareBindingIssueAsync_Should_Throw_For_Invalid_Address()
    {
        // Arrange
        var input = new PrepareBindIssueInput
        {
            Symbol = "ELF",
            ChainId = "Ethereum",
            Address = "invalid_address" // Invalid address
        };

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => 
            await _tokenAccessAppService.PrepareBindingIssueAsync(input));
    }

    [Fact]
    public async Task GetBindingIssueAsync_Should_Throw_When_Not_Authenticated()
    {
        // Arrange
        var input = new UserTokenBindingDto
        {
            BindingId = "binding_id",
            ThirdTokenId = "token_id",
            MintToAddress = "address"
        };

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => 
            await _tokenAccessAppService.GetBindingIssueAsync(input));
    }

    [Fact]
    public async Task GetTokenApplyOrderListAsync_Empty_When_Not_Authenticated()
    {
        // Act
        var result = await _tokenAccessAppService.GetTokenApplyOrderListAsync(new GetTokenApplyOrderListInput());

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTokenApplyOrderDetailAsync_Empty_When_Not_Authenticated()
    {
        // Arrange
        var input = new GetTokenApplyOrderInput
        {
            Symbol = "ELF"
        };

        // Act
        var result = await _tokenAccessAppService.GetTokenApplyOrderDetailAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task TriggerOrderStatusChangeAsync_Should_Throw_For_Invalid_Order()
    {
        // Arrange
        var input = new TriggerOrderStatusChangeInput
        {
            OrderId = Guid.NewGuid().ToString(),
            ChainIdTokenInfo = new ChainTokenDto
            {
                TokenContractAddress = "contract_address",
                TokenDecimals = 8
            }
        };

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => 
            await _tokenAccessAppService.TriggerOrderStatusChangeAsync(input));
    }

    // Index-related tests
    [Fact]
    public async Task AddUserTokenAccessInfoIndexAsync_Should_Work()
    {
        // Arrange
        var input = new AddUserTokenAccessInfoIndexInput
        {
            Symbol = "TEST_TOKEN2",
            Address = "test_address2",
            Email = "test2@example.com"
        };

        // Act & Assert - If no exception is thrown, consider it successful
        await _tokenAccessAppService.AddUserTokenAccessInfoIndexAsync(input);
    }

    [Fact]
    public async Task AddThirdUserTokenIssueInfoIndexAsync_Should_Work()
    {
        // Arrange
        var input = new AddThirdUserTokenIssueInfoIndexInput
        {
            Id = Guid.NewGuid(),
            Symbol = "TEST_TOKEN",
            Address = "test_address",
            ChainId = "MainChain_AELF",
            OtherChainId = "Ethereum"
        };

        // Act & Assert - If no exception is thrown, consider it successful
        await _tokenAccessAppService.AddThirdUserTokenIssueInfoIndexAsync(input);
    }

    [Fact]
    public async Task AddTokenApplyOrderIndexAsync_Should_Work()
    {
        // Arrange
        var input = new AddTokenApplyOrderIndexInput
        {
            Id = Guid.NewGuid(),
            Symbol = "TEST_TOKEN",
            UserAddress = "test_address",
            ChainId = "Ethereum",
            ChainName = "Ethereum",
            TokenName = "Test Token",
            Status = "Pending",
            StatusChangedRecords = new List<StatusChangedRecordDto>
            {
                new StatusChangedRecordDto
                {
                    Id = Guid.NewGuid(),
                    Status = "Pending",
                    Time = DateTime.UtcNow
                }
            }
        };

        // Act & Assert - If no exception is thrown, consider it successful
        await _tokenAccessAppService.AddTokenApplyOrderIndexAsync(input);
    }

    [Fact]
    public async Task GetAvailableTokenDetailAsync_Should_Throw_When_Not_Authenticated()
    {
        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => 
            await _tokenAccessAppService.GetAvailableTokenDetailAsync("ELF"));
    }
    
    [Fact]
    public async Task GetUserTokenAccessInfoAsync_Should_Throw_When_Not_Authenticated()
    {
        // Arrange
        var input = new UserTokenAccessInfoBaseInput
        {
            Symbol = "ELF"
        };

        // Act & Assert
        await Should.ThrowAsync<Exception>(async () => 
            await _tokenAccessAppService.GetUserTokenAccessInfoAsync(input));
    }
} 