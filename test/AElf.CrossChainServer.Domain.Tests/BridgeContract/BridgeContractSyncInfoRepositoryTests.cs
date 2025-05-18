// using System;
// using System.Threading.Tasks;
// using Shouldly;
// using Xunit;
//
// namespace AElf.CrossChainServer.BridgeContract;
//
// public class BridgeContractSyncInfoRepositoryTests: CrossChainServerDomainTestBase
// {
//     private readonly IBridgeContractSyncInfoRepository _bridgeContractSyncInfoRepository;
//
//     public BridgeContractSyncInfoRepositoryTests()
//     {
//         _bridgeContractSyncInfoRepository = GetRequiredService<IBridgeContractSyncInfoRepository>();
//     }
//
//     [Fact]
//     public async Task Get_Set_Test()
//     {
//         var bridgeContractSyncInfo = new BridgeContractSyncInfo
//         {
//             Type = TransferType.Receive,
//             ChainId = "MainChain_AELF",
//             SyncIndex = 100,
//             TargetChainId = "SideChain_AELF",
//             TokenId = Guid.NewGuid()
//         };
//
//         await _bridgeContractSyncInfoRepository.InsertAsync(bridgeContractSyncInfo);
//         var syncInfos = await _bridgeContractSyncInfoRepository.GetListAsync();
//         syncInfos.Count.ShouldBe(1);
//         syncInfos[0].Type.ShouldBe(bridgeContractSyncInfo.Type);
//         syncInfos[0].ChainId.ShouldBe(bridgeContractSyncInfo.ChainId);
//         syncInfos[0].SyncIndex.ShouldBe(bridgeContractSyncInfo.SyncIndex);
//         syncInfos[0].TargetChainId.ShouldBe(bridgeContractSyncInfo.TargetChainId);
//         syncInfos[0].TokenId.ShouldBe(bridgeContractSyncInfo.TokenId);
//     }
// }