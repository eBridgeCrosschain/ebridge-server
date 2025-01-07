// using System.Collections.Generic;
// using Moq;
//
// namespace AElf.CrossChainServer.TokenAccess;
//
// public partial class TokenAccessAppServiceTests
// {
//     private ITokenInvokeProvider GetMockTokenInvokeProvider()
//     {
//         var mockTokenInvokeProvider = new Mock<ITokenInvokeProvider>();
//         // mockTokenInvokeProvider.Setup(o => o.GetUserTokenOwnerListAndUpdateAsync(It.IsAny<string>()))
//         //     .ReturnsAsync(new List<UserTokenInfoDto>
//         //     {
//         //         new()
//         //         {
//         //             Address = "test_address",
//         //             TokenName = "test_token",
//         //             Symbol = "test_token",
//         //             Decimals = 8,
//         //             Icon = "test_icon",
//         //             Owner = "test_owner",
//         //             ChainId = "AELF",
//         //             TotalSupply = 1000000,
//         //             LiquidityInUsd = "100000",
//         //             Holders = 100000,
//         //             PoolAddress = "test_pool",
//         //             ContractAddress = "test_contract_address",
//         //             Status = "test_status"
//         //         }
//         //     });
//         //
//         // mockTokenInvokeProvider.Setup(o => o.GetAsync(It.IsAny<string>()))
//         //     .ReturnsAsync(new List<UserTokenInfoDto>()
//         //     {
//         //         new()
//         //         {
//         //             Address = "test_address",
//         //             TokenName = "test_token",
//         //             Symbol = "test_token",
//         //             Decimals = 8,
//         //             Icon = "test_icon",
//         //             Owner = "test_owner",
//         //             ChainId = "AELF",
//         //             TotalSupply = 1000000,
//         //             LiquidityInUsd = "100000",
//         //             Holders = 100000,
//         //             PoolAddress = "test_pool",
//         //             ContractAddress = "test_contract_address",
//         //             Status = "test_status"
//         //         }
//         //     });
//         //
//         mockTokenInvokeProvider.Setup(o => o.GetThirdTokenListAndUpdateAsync(It.IsAny<string>(), It.IsAny<string>()))
//             .ReturnsAsync(true);
//         
//         mockTokenInvokeProvider.Setup(o => o.PrepareBindingAsync(It.IsAny<ThirdUserTokenIssueInfoDto>()))
//             .ReturnsAsync(new UserTokenBindingDto
//             {
//                 BindingId = "test_binding_id",
//                 ThirdTokenId = "test_third_token_id"
//             });
//         mockTokenInvokeProvider.Setup(o => o.BindingAsync(It.IsAny<UserTokenBindingDto>()))
//             .ReturnsAsync(true);
//
//         return mockTokenInvokeProvider.Object;
//     }
//
//     // private IUserTokenOwnerProvider GetMockUserTokenOwnerProvider()
//     // {
//     //     var mockUserTokenOwnerProvider = new Mock<IUserTokenOwnerProvider>();
//     //     // mockUserTokenOwnerProvider.Setup(o => o.GetUserTokenOwnerListAsync(It.IsAny<string>()))
//     //     //     .ReturnsAsync(new List<UserTokenInfoDto>()
//     //     //     {
//     //     //         new()
//     //     //         {
//     //     //             Address = "test_address",
//     //     //             TokenName = "test_token",
//     //     //             Symbol = "test_token",
//     //     //             Decimals = 8,
//     //     //             Icon = "test_icon",
//     //     //             Owner = "test_owner",
//     //     //             ChainId = "AELF",
//     //     //             TotalSupply = 1000000,
//     //     //             LiquidityInUsd = "100000",
//     //     //             Holders = 100000,
//     //     //             PoolAddress = "test_pool",
//     //     //             ContractAddress = "test_contract_address",
//     //     //             Status = "test_status"
//     //     //         }
//     //     //     });
//     //     return mockUserTokenOwnerProvider.Object;
//     // }
// }