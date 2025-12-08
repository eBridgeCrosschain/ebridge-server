using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Contracts.MultiToken;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.ContractEventHandler.Processors;

public class CrossChainTransferProcessorTests : ContractEventHandlerCoreTestBase
{
    private readonly IEventHandlerTestProcessor<CrossChainTransferred> _crossChainTransferredTestProcessor;
    private readonly IEventHandlerTestProcessor<CrossChainReceived> _crossChainReceivedTestProcessor;
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;

    public CrossChainTransferProcessorTests()
    {
        _crossChainTransferredTestProcessor = GetRequiredService<IEventHandlerTestProcessor<CrossChainTransferred>>();
        _crossChainReceivedTestProcessor = GetRequiredService<IEventHandlerTestProcessor<CrossChainReceived>>();
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
        var tokenReceive = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId ="SideChain_tDVV",
            Symbol = "Symbol"
        });
        var transferEvent = new CrossChainTransferred
        {
            Symbol = "Symbol",
            Amount = 100,
            From = Address.FromBase58("2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7"),
            To = Address.FromBase58("Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25"),
            Memo = "Memo",
            IssueChainId = 9992731,
            ToChainId = 1866392
        };
        var contractEvent = EventContextHelper.Create("CrossChainTransferred",9992731);
        var transferTxId = contractEvent.TransactionId;
        
        await _crossChainTransferredTestProcessor.HandleEventAsync(transferEvent, contractEvent);
        
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
        transfers.Items[0].Type.ShouldBe(CrossChainType.Homogeneous);
        transfers.Items[0].FromAddress.ShouldBe(transferEvent.From.ToBase58());
        transfers.Items[0].ToAddress.ShouldBe(transferEvent.To.ToBase58());
        transfers.Items[0].FromChainId.ShouldBe("MainChain_AELF");
        transfers.Items[0].ToChainId.ShouldBe("SideChain_tDVV");
        transfers.Items[0].TransferBlockHeight.ShouldBe(contractEvent.BlockNumber);
        transfers.Items[0].TransferTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(contractEvent.BlockTime));
        transfers.Items[0].TransferTransactionId.ShouldBe(transferTxId);
        
        var receivedEvent = new CrossChainReceived
        {
            Symbol = "Symbol",
            Amount = 100,
            From = Address.FromBase58("2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7"),
            To = Address.FromBase58("Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25"),
            Memo = "Memo",
            FromChainId = 9992731,
            TransferTransactionId = Hash.LoadFromHex(transferTxId),
            IssueChainId = 9992731,
            ParentChainHeight = 10000
        };
        contractEvent = EventContextHelper.Create("CrossChainReceived",1866392);
        await _crossChainReceivedTestProcessor.HandleEventAsync(receivedEvent, contractEvent);

        transfers = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        transfers.Items[0].Progress.ShouldBe(100);
        transfers.Items[0].ReceiveTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(contractEvent.BlockTime));
        transfers.Items[0].ReceiveTransactionId.ShouldBe(contractEvent.TransactionId);
        transfers.Items[0].Status.ShouldBe(CrossChainStatus.Received);
        transfers.Items[0].ReceiveToken.Id.ShouldBe(tokenReceive.Id);
    }

    [Fact]
    public async Task HandleEvent_ChainNotExist_Test()
    {
        var transferEvent = new CrossChainTransferred
        {
            Symbol = "Symbol",
            Amount = 100,
            From = Address.FromBase58("2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7"),
            To = Address.FromBase58("Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25"),
            Memo = "Memo",
            IssueChainId = 9992731,
            ToChainId = 100
        };
        var contractEvent = EventContextHelper.Create("CrossChainTransferred", 9992731);

        await _crossChainTransferredTestProcessor.HandleEventAsync(transferEvent, contractEvent);

        var transfers = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        transfers.TotalCount.ShouldBe(0);
        
        var receivedEvent = new CrossChainReceived
        {
            Symbol = "Symbol",
            Amount = 100,
            From = Address.FromBase58("2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7"),
            To = Address.FromBase58("Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25"),
            Memo = "Memo",
            FromChainId = 100,
            TransferTransactionId = Hash.LoadFromHex(contractEvent.TransactionId),
            IssueChainId = 9992731,
            ParentChainHeight = 10000
        };
        contractEvent = EventContextHelper.Create("CrossChainReceived",1866392);
        await _crossChainReceivedTestProcessor.HandleEventAsync(receivedEvent, contractEvent);

        transfers = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        transfers.TotalCount.ShouldBe(0);
    }
}