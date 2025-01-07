// using System;
// using System.Threading.Tasks;
// using AElf.CrossChainServer.TokenAccess.ThirdUserTokenIssue;
// using Microsoft.Extensions.DependencyInjection;
// using NSubstitute;
// using Shouldly;
// using Volo.Abp.Users;
// using Xunit;
//
// namespace AElf.CrossChainServer.TokenAccess;
//
// public sealed partial class TokenAccessAppServiceTests : CrossChainServerApplicationTestBase
// {
//     protected ICurrentUser _currentUser;
//     private readonly ITokenAccessAppService _tokenAccessAppService;
//     private readonly IThirdUserTokenIssueRepository _thirdUserTokenIssueRepository;
//     
//     public TokenAccessAppServiceTests()
//     {
//         _tokenAccessAppService = GetRequiredService<ITokenAccessAppService>();
//         _thirdUserTokenIssueRepository = GetRequiredService<IThirdUserTokenIssueRepository>();
//     }
//
//     protected override void AfterAddApplication(IServiceCollection services)
//     {
//         base.AfterAddApplication(services);
//         _currentUser = Substitute.For<ICurrentUser>();
//         services.AddSingleton(_currentUser);
//         services.AddSingleton(GetMockTokenInvokeProvider());
//         // services.AddSingleton(GetMockUserTokenOwnerProvider());
//     }
//
//     private void Login(Guid userId)
//     {
//         _currentUser.Id.Returns(userId);
//         _currentUser.IsAuthenticated.Returns(true);
//     }
//
//     [Fact]
//     public async Task GetAvailableTokensTest()
//     {
//         Login(new Guid("d3d94468-2d38-4b1f-9dcd-fbfc7ddcab1b"));
//         var result = await _tokenAccessAppService.GetAvailableTokensAsync(new GetAvailableTokensInput());
//         result.TokenList.Count.ShouldBe(1);
//         result.TokenList[0].TokenName.ShouldBe("test_token");
//         result.TokenList[0].Symbol.ShouldBe("test_token");
//         result.TokenList[0].LiquidityInUsd.ShouldBe("100000");
//         result.TokenList[0].Holders.ShouldBe(100000);
//     }
//
//     [Fact]
//     public async Task CommitTokenAccessInfoTest()
//     {
//         Login(new Guid("d3d94468-2d38-4b1f-9dcd-fbfc7ddcab1b"));
//         var input = new UserTokenAccessInfoInput
//         {
//             Symbol = "test_token",
//             OfficialWebsite = "test_official_website",
//             OfficialTwitter = "test_official_twitter",
//             Title = "test_title",
//             PersonName = "test_person",
//             TelegramHandler = "test_telegram",
//             Email = "test@gmail.com"
//         };
//         var result = await _tokenAccessAppService.CommitTokenAccessInfoAsync(input);
//         result.ShouldBe(true);
//     }
//
//     [Fact]
//     public async Task GetUserTokenAccessInfoTest()
//     {
//         Login(new Guid("d3d94468-2d38-4b1f-9dcd-fbfc7ddcab1b"));
//         var input = new UserTokenAccessInfoInput
//         {
//             Symbol = "test_token",
//             OfficialWebsite = "test_official_website",
//             OfficialTwitter = "test_official_twitter",
//             Title = "test_title",
//             PersonName = "test_person",
//             TelegramHandler = "test_telegram",
//             Email = "test@gmail.com"
//         };
//         var result = await _tokenAccessAppService.GetUserTokenAccessInfoAsync(input);
//     }
//
//     [Fact]
//     public async Task CheckChainAccessStatusTest()
//     {
//         Login(new Guid("d3d94468-2d38-4b1f-9dcd-fbfc7ddcab1b"));
//         var input = new CheckChainAccessStatusInput
//         {
//             Symbol = "test_token"
//         };
//         var result = await _tokenAccessAppService.CheckChainAccessStatusAsync(input);
//     }
//
//     [Fact]
//     public async Task PrepareBindingIssueTest()
//     {
//         Login(new Guid("d3d94468-2d38-4b1f-9dcd-fbfc7ddcab1b"));
//         await _thirdUserTokenIssueRepository.InsertAsync(new ThirdUserTokenIssueInfo
//         {
//             Id = default,
//             Address = "side_test_user_address",
//             WalletAddress = "test_wallet",
//             Symbol = "test_token",
//             ChainId = "AELF",
//             CreateTime = 0,
//             UpdateTime = 0,
//             TokenName = "test_token",
//             TokenImage = "test_image",
//             OtherChainId = "test_third_chain",
//             TotalSupply = "10000",
//             ContractAddress = "test_contract_address",
//             BindingId = "test_bind_id",
//             ThirdTokenId = "test_third_token_id",
//             Status = "issued"
//         });
//         var input = new PrepareBindIssueInput
//         {
//             Address = "side_test_user_address",
//             Symbol = "test_token",
//             ChainId = "test_third_chain",
//             ContractAddress = "test_contract_address",
//             Supply = "1000"
//         };
//         // var result = await _tokenAccessAppService.PrepareBindingIssueAsync(input);
//     }
//     
// }