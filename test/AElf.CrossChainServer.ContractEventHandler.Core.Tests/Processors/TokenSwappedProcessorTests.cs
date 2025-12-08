using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Contracts.Bridge;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class TokenSwappedProcessorTests : ContractEventHandlerCoreTestBase
{
    private readonly IEventHandlerTestProcessor<TokenSwapped> _tokenSwappedTestProcessor;
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;

    public TokenSwappedProcessorTests()
    {
        _tokenSwappedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<TokenSwapped>>();
        _crossChainTransferAppService = GetRequiredService<ICrossChainTransferAppService>();
        _tokenAppService = GetRequiredService<ITokenAppService>();
    }

    [Fact]
    public async Task HandleEventTest()
    {
        var tokenReceive = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = "MainChain_AELF",
            Symbol = "Symbol"
        });
        
        var receivedEvent = new TokenSwapped
        {
            Symbol = "Symbol",
            Amount = 100,
            Address = Address.FromBase58("2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7"),
            ReceiptId = "ReceiptId",
            FromChainId = "Ethereum"
        };
         var contractEvent = EventContextHelper.Create("TokenSwapped",9992731);
        await _tokenSwappedTestProcessor.HandleEventAsync(receivedEvent, contractEvent);

        var transfers = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        transfers.Items[0].Progress.ShouldBe(100);
        transfers.Items[0].ReceiveTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(contractEvent.BlockTime));
        transfers.Items[0].ReceiveTransactionId.ShouldBe(contractEvent.TransactionId);
        transfers.Items[0].Status.ShouldBe(CrossChainStatus.Received);
        transfers.Items[0].ReceiveToken.Id.ShouldBe(tokenReceive.Id);
    }
}