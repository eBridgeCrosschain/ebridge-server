// using System;
// using System.Threading.Tasks;
// using AElf.AElfNode.EventHandler.Core.Domains.Entities;
// using AElf.AElfNode.EventHandler.Core.Repositories;
// using Shouldly;
// using Xunit;
//
// namespace AElf.CrossChainServer.Chains;
//
// public class EventHandlerAppServiceTests : CrossChainServerApplicationTestBase
// {
//     private readonly IEventHandlerAppService _eventHandlerAppService;
//     private readonly ISaveDataRepository _saveDataRepository;
//
//     public EventHandlerAppServiceTests()
//     {
//         _eventHandlerAppService = GetRequiredService<IEventHandlerAppService>();
//         _saveDataRepository = GetRequiredService<ISaveDataRepository>();
//     }
//
//     [Fact]
//     public async Task GetLatestSyncTimeTest()
//     {
//         var time = await _eventHandlerAppService.GetLatestSyncTimeAsync("MainChain_AELF", "CrossChain");
//         time.ShouldBe(DateTime.MinValue);
//
//         var data = DateTime.UtcNow;
//         await _saveDataRepository.InsertAsync(new SaveData
//         {
//             Key = "9992731-CrossChain-LatestCheckTickKey",
//             Data = data.Ticks.ToString()
//         });
//         
//         time = await _eventHandlerAppService.GetLatestSyncTimeAsync("MainChain_AELF", "CrossChain");
//         time.ShouldBe(data);
//     }
// }