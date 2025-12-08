using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Contracts.Bridge;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class ReceiptCreatedProcessorTests : ContractEventHandlerCoreTestBase
{
    private readonly IEventHandlerTestProcessor<ReceiptCreated> _receiptCreatedTestProcessor;
    private readonly IEventHandlerTestProcessor<TokenSwapped> _tokenSwappedTestProcessor;
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;

    public ReceiptCreatedProcessorTests()
    {
        _receiptCreatedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<ReceiptCreated>>();
        _tokenSwappedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<TokenSwapped>>();
        _crossChainTransferAppService = GetRequiredService<ICrossChainTransferAppService>();
        _tokenAppService = GetRequiredService<ITokenAppService>();
    }

    [Fact]
    public async Task HandleEventTest()
    {
        var tokenTransfer = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId ="MainChain_AELF",
            Symbol = "Symbol"
        });
        
        var transferEvent = new ReceiptCreated
        {
            Symbol = "Symbol",
            Amount = 100,
            Owner = Address.FromBase58("2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7"),
            ReceiptId = "ReceiptId",
            TargetAddress = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25",
            TargetChainId = "Ethereum",
        };
        var contractEvent = EventContextHelper.Create("ReceiptCreated",9992731);
        var transferTxId = contractEvent.TransactionId;
        
        await _receiptCreatedTestProcessor.HandleEventAsync(transferEvent, contractEvent);
        
        var transfers = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        transfers.TotalCount.ShouldBe(1);
        transfers.Items[0].TransferAmount.ShouldBe(transferEvent.Amount);
        transfers.Items[0].ReceiveAmount.ShouldBe(0);
        transfers.Items[0].Progress.ShouldBe(0);
        transfers.Items[0].Status.ShouldBe(CrossChainStatus.Transferred);
        transfers.Items[0].TransferToken.Id.ShouldBe(tokenTransfer.Id);
        transfers.Items[0].ReceiveToken.ShouldBeNull();
        transfers.Items[0].Type.ShouldBe(CrossChainType.Heterogeneous);
        transfers.Items[0].FromAddress.ShouldBe(transferEvent.Owner.ToBase58());
        transfers.Items[0].ToAddress.ShouldBe(transferEvent.TargetAddress);
        transfers.Items[0].FromChainId.ShouldBe("MainChain_AELF");
        transfers.Items[0].ToChainId.ShouldBe(transferEvent.TargetChainId);
        transfers.Items[0].TransferBlockHeight.ShouldBe(contractEvent.BlockNumber);
        transfers.Items[0].TransferTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(contractEvent.BlockTime));
        transfers.Items[0].TransferTransactionId.ShouldBe(transferTxId);
    }
}