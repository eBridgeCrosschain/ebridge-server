using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Tokens;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainTransferAppServiceTests : CrossChainServerApplicationTestBase
{
    private readonly ICrossChainTransferRepository _crossChainTransferRepository;
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly ICrossChainIndexingInfoAppService _crossChainIndexingInfoAppService;
    private readonly IOracleQueryInfoAppService _oracleQueryInfoAppService;
    private readonly IReportInfoAppService _reportInfoAppService;

    public CrossChainTransferAppServiceTests()
    {
        _crossChainTransferRepository = GetRequiredService<ICrossChainTransferRepository>();
        _crossChainTransferAppService = GetRequiredService<ICrossChainTransferAppService>();
        _tokenAppService = GetRequiredService<ITokenAppService>();
        _crossChainIndexingInfoAppService = GetRequiredService<ICrossChainIndexingInfoAppService>();
        _oracleQueryInfoAppService = GetRequiredService<IOracleQueryInfoAppService>();
        _reportInfoAppService = GetRequiredService<IReportInfoAppService>();
    }

    [Fact]
    public async Task HomogeneousTransferTest()
    {
        var tokenTransfer = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = "MainChain_AELF",
            Symbol = "ELF"
        });
        var tokenReceived = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = "SideChain_tDVV",
            Symbol = "ELF"
        });

        var input = new CrossChainTransferInput
        {
            TransferAmount = 100,
            FromAddress = "FromAddress",
            ToAddress = "ToAddress",
            TransferTokenId = tokenTransfer.Id,
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            TransferBlockHeight = 100,
            TransferTime = DateTime.UtcNow.AddMinutes(-1),
            TransferTransactionId = "TransferTransactionId"
        };
        await _crossChainTransferAppService.TransferAsync(input);

        var list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items.Count.ShouldBe(1);
        list.Items[0].TransferAmount.ShouldBe(input.TransferAmount);
        list.Items[0].ReceiveAmount.ShouldBe(0);
        list.Items[0].Progress.ShouldBe(0);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Transferred);
        list.Items[0].TransferToken.Id.ShouldBe(tokenTransfer.Id);
        list.Items[0].ReceiveToken.ShouldBeNull();
        list.Items[0].Type.ShouldBe(CrossChainType.Homogeneous);
        list.Items[0].FromAddress.ShouldBe(input.FromAddress);
        list.Items[0].ToAddress.ShouldBe(input.ToAddress);
        list.Items[0].FromChainId.ShouldBe(input.FromChainId);
        list.Items[0].ToChainId.ShouldBe(input.ToChainId);
        list.Items[0].TransferBlockHeight.ShouldBe(input.TransferBlockHeight);
        list.Items[0].TransferTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(input.TransferTime));
        list.Items[0].TransferTransactionId.ShouldBe(input.TransferTransactionId);

        var status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(0);

        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 99000,
            BlockTime = DateTime.UtcNow.AddMinutes(-5),
            IndexBlockHeight = 60,
            IndexChainId = "MainChain_AELF",
        });

        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 80,
            IndexChainId = "MainChain_AELF",
        });

        await _crossChainTransferAppService.UpdateProgressAsync();

        var exception = await Assert.ThrowsAsync<AbpValidationException>(async () => await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            FromChainId = "FromChainId_FromChainId",
            ToChainId = "ToChainId_ToChainId_ToChainId",
            FromAddress = "FromAddress_FromAddress_FromAddress_FromAddress_FromAddress_FromAddress_FromAddress_FromAddress",
            ToAddress = "ToAddress_ToAddress_ToAddress_ToAddress_ToAddress_ToAddress_ToAddress_ToAddress_ToAddress"
        }));
        exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem.Contains("FromChainId")));
        exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem.Contains("ToChainId")));
        exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem.Contains("FromAddress")));
        exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem.Contains("ToAddress")));

        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(50);
        
        var guidList = new List<Guid>();
        for (int i = 0; i < 11; i++)
        {
            guidList.Add(new Guid());
        }
        exception = await Assert.ThrowsAsync<AbpValidationException>(async () => await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = guidList
        }));
        exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem.Contains("Ids")));
        
        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(50);
        
        await _crossChainIndexingInfoAppService.CreateAsync(new CreateCrossChainIndexingInfoInput
        {
            ChainId = "SideChain_tDVV",
            BlockHeight = 100000,
            BlockTime = DateTime.UtcNow,
            IndexBlockHeight = 120,
            IndexChainId = "MainChain_AELF",
        });

        await Task.Delay(1000);
        await _crossChainTransferAppService.UpdateProgressAsync();
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(100);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Indexed);
        
        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(100);

        var receiveInput = new CrossChainReceiveInput
        {
            ReceiveTime = DateTime.UtcNow,
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            ReceiveTransactionId = "ReceiveTransactionId",
            TransferTransactionId = "TransferTransactionId",
            ReceiveAmount = 100,
            ReceiveTokenId = tokenReceived.Id
        };
        await _crossChainTransferAppService.ReceiveAsync(receiveInput);
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(100);
        list.Items[0].ReceiveTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(receiveInput.ReceiveTime));
        list.Items[0].ReceiveTransactionId.ShouldBe(receiveInput.ReceiveTransactionId);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Received);
        list.Items[0].ReceiveToken.Id.ShouldBe(tokenReceived.Id);
        list.Items[0].ReceiveAmount.ShouldBe(receiveInput.ReceiveAmount);
        
        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(100);
    }
    
    [Fact]
    public async Task HeterogeneousTransfer_ETH_To_AELF_Test()
    {
        var tokenTransfer = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId ="Ethereum",
            Symbol = "ELF"
        });
        var tokenReceived = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId ="MainChain_AELF",
            Symbol = "ELF"
        });

        var input = new CrossChainTransferInput
        {
            TransferAmount = 100,
            FromAddress = "FromAddress",
            ToAddress = "ToAddress",
            TransferTokenId = tokenTransfer.Id,
            FromChainId = "Ethereum",
            ToChainId = "MainChain_AELF",
            TransferBlockHeight = 100,
            TransferTime = DateTime.UtcNow.AddMinutes(-1),
            ReceiptId = "ReceiptId"
        };
        await _crossChainTransferAppService.TransferAsync(input);

        var list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items.Count.ShouldBe(1);
        list.Items[0].TransferAmount.ShouldBe(input.TransferAmount);
        list.Items[0].ReceiveAmount.ShouldBe(0);
        list.Items[0].Progress.ShouldBe(0);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Transferred);
        list.Items[0].TransferToken.Id.ShouldBe(tokenTransfer.Id);
        list.Items[0].ReceiveToken.ShouldBeNull();
        list.Items[0].Type.ShouldBe(CrossChainType.Heterogeneous);
        list.Items[0].FromAddress.ShouldBe(input.FromAddress);
        list.Items[0].ToAddress.ShouldBe(input.ToAddress);
        list.Items[0].FromChainId.ShouldBe(input.FromChainId);
        list.Items[0].ToChainId.ShouldBe(input.ToChainId);
        list.Items[0].TransferBlockHeight.ShouldBe(input.TransferBlockHeight);
        list.Items[0].TransferTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(input.TransferTime));
        list.Items[0].ReceiptId.ShouldBe(input.ReceiptId);

        var status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(0);
        
        await _oracleQueryInfoAppService.CreateAsync(new CreateOracleQueryInfoInput
        {
            Option = "ReceiptId",
            Step = OracleStep.QueryCreated,
            ChainId = "MainChain_AELF",
            QueryId = "QueryId",
            LastUpdateHeight = 100
        });

        await _crossChainTransferAppService.UpdateProgressAsync();
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(20);
        
        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(20);
        
        await _oracleQueryInfoAppService.UpdateAsync(new UpdateOracleQueryInfoInput
        {
            Step = OracleStep.Committed,
            ChainId = "MainChain_AELF",
            QueryId = "QueryId",
            LastUpdateHeight = 100
        });
        await _oracleQueryInfoAppService.UpdateAsync(new UpdateOracleQueryInfoInput
        {
            Step = OracleStep.SufficientCommitmentsCollected,
            ChainId = "MainChain_AELF",
            QueryId = "QueryId",
            LastUpdateHeight = 100
        });
        await _oracleQueryInfoAppService.UpdateAsync(new UpdateOracleQueryInfoInput
        {
            Step = OracleStep.CommitmentRevealed,
            ChainId = "MainChain_AELF",
            QueryId = "QueryId",
            LastUpdateHeight = 100
        });
        await _oracleQueryInfoAppService.UpdateAsync(new UpdateOracleQueryInfoInput
        {
            Step = OracleStep.QueryCompleted,
            ChainId = "MainChain_AELF",
            QueryId = "QueryId",
            LastUpdateHeight = 100
        });

        await Task.Delay(1000);
        await _crossChainTransferAppService.UpdateProgressAsync();
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(100);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Indexed);
        
        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(100);

        var receiveInput = new CrossChainReceiveInput
        {
            ReceiveTime = DateTime.UtcNow,
            FromChainId = "Ethereum",
            ToChainId = "MainChain_AELF",
            ReceiveTransactionId = "ReceiveTransactionId",
            ReceiptId = "ReceiptId",
            ReceiveAmount = 100,
            ReceiveTokenId = tokenReceived.Id
        };
        await _crossChainTransferAppService.ReceiveAsync(receiveInput);
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(100);
        list.Items[0].ReceiveTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(receiveInput.ReceiveTime));
        list.Items[0].ReceiveTransactionId.ShouldBe(receiveInput.ReceiveTransactionId);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Received);
        list.Items[0].ReceiveToken.Id.ShouldBe(tokenReceived.Id);
        list.Items[0].ReceiveAmount.ShouldBe(receiveInput.ReceiveAmount);
        
        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(100);
    }
    
    [Fact]
    public async Task HeterogeneousTransfer_AELF_To_ETH_Test()
    {
        var tokenTransfer = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId ="MainChain_AELF",
            Symbol = "ELF"
        });
        var tokenReceived = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId ="Ethereum",
            Symbol = "ELF"
        });

        var input = new CrossChainTransferInput
        {
            TransferAmount = 100,
            FromAddress = "FromAddress",
            ToAddress = "ToAddress",
            TransferTokenId = tokenTransfer.Id,
            FromChainId = "MainChain_AELF",
            ToChainId = "Ethereum",
            TransferBlockHeight = 100,
            TransferTime = DateTime.UtcNow.AddMinutes(-1),
            ReceiptId = "ReceiptId"
        };
        await _crossChainTransferAppService.TransferAsync(input);

        var list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items.Count.ShouldBe(1);
        list.Items[0].TransferAmount.ShouldBe(input.TransferAmount);
        list.Items[0].ReceiveAmount.ShouldBe(0);
        list.Items[0].Progress.ShouldBe(0);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Transferred);
        list.Items[0].TransferToken.Id.ShouldBe(tokenTransfer.Id);
        list.Items[0].ReceiveToken.ShouldBeNull();
        list.Items[0].Type.ShouldBe(CrossChainType.Heterogeneous);
        list.Items[0].FromAddress.ShouldBe(input.FromAddress);
        list.Items[0].ToAddress.ShouldBe(input.ToAddress);
        list.Items[0].FromChainId.ShouldBe(input.FromChainId);
        list.Items[0].ToChainId.ShouldBe(input.ToChainId);
        list.Items[0].TransferBlockHeight.ShouldBe(input.TransferBlockHeight);
        list.Items[0].TransferTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(input.TransferTime));
        list.Items[0].ReceiptId.ShouldBe(input.ReceiptId);

        var status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(0);
        
        await _reportInfoAppService.CreateAsync(new CreateReportInfoInput()
        {
            ChainId = "MainChain_AELF",
            RoundId = 1,
            Token = "Eth",
            ReceiptHash = "ReceiptHash",
            ReceiptId = "ReceiptId",
            TargetChainId = "Ethereum"
        });

        await _crossChainTransferAppService.UpdateProgressAsync();
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(100/3);
        
        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(100/3);
        
        await _reportInfoAppService.UpdateStepAsync("MainChain_AELF",1,"Eth","Ethereum", ReportStep.Confirmed, 100);
        await Task.Delay(2000);
        await _crossChainTransferAppService.UpdateProgressAsync();
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(200/3);
        
        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(200/3);

        await _reportInfoAppService.UpdateStepAsync("MainChain_AELF",1,"Eth", "Ethereum",ReportStep.Transmitted, 110);
        await Task.Delay(2000);
        await _crossChainTransferAppService.UpdateProgressAsync();
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(100);
        
        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(100);

        var receiveInput = new CrossChainReceiveInput
        {
            ReceiveTime = DateTime.UtcNow,
            FromChainId = "MainChain_AELF",
            ToChainId = "Ethereum",
            ReceiveTransactionId = "ReceiveTransactionId",
            ReceiptId = "ReceiptId",
            ReceiveAmount = 100,
            ReceiveTokenId = tokenReceived.Id
        };
        await _crossChainTransferAppService.ReceiveAsync(receiveInput);
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(100);
        list.Items[0].ReceiveTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(receiveInput.ReceiveTime));
        list.Items[0].ReceiveTransactionId.ShouldBe(receiveInput.ReceiveTransactionId);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Received);
        list.Items[0].ReceiveToken.Id.ShouldBe(tokenReceived.Id);
        list.Items[0].ReceiveAmount.ShouldBe(receiveInput.ReceiveAmount);
        
        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(100);
    }

    [Fact]
    public async Task ReceiveFirstTest()
    {
        var tokenTransfer = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId ="MainChain_AELF",
            Symbol = "ELF"
        });
        var tokenReceived = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId ="Ethereum",
            Symbol = "ELF"
        });
        
        var receiveInput = new CrossChainReceiveInput
        {
            ReceiveTime = DateTime.UtcNow,
            FromChainId = "MainChain_AELF",
            ToChainId = "Ethereum",
            ReceiveTransactionId = "ReceiveTransactionId",
            ReceiptId = "ReceiptId",
            ReceiveAmount = 100,
            ReceiveTokenId = tokenReceived.Id,
            FromAddress = "FromAddress",
            ToAddress = "ToAddress",
        };
        await _crossChainTransferAppService.ReceiveAsync(receiveInput);
        
        var list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items[0].Progress.ShouldBe(100);
        list.Items[0].ReceiveTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(receiveInput.ReceiveTime));
        list.Items[0].ReceiveAmount.ShouldBe(receiveInput.ReceiveAmount);
        list.Items[0].ReceiveTransactionId.ShouldBe(receiveInput.ReceiveTransactionId);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Received);
        list.Items[0].ReceiveToken.Id.ShouldBe(tokenReceived.Id);
        list.Items[0].FromAddress.ShouldBe(receiveInput.FromAddress);
        list.Items[0].ToAddress.ShouldBe(receiveInput.ToAddress);
        list.Items[0].FromChainId.ShouldBe(receiveInput.FromChainId);
        list.Items[0].ToChainId.ShouldBe(receiveInput.ToChainId);
        list.Items[0].Type.ShouldBe(CrossChainType.Heterogeneous);
        
        var status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(100);
        
        var input = new CrossChainTransferInput
        {
            TransferAmount = 100,
            FromAddress = "FromAddress",
            ToAddress = "ToAddress",
            TransferTokenId = tokenTransfer.Id,
            FromChainId = "MainChain_AELF",
            ToChainId = "Ethereum",
            TransferBlockHeight = 100,
            TransferTime = DateTime.UtcNow.AddMinutes(-1),
            ReceiptId = "ReceiptId"
        };
        await _crossChainTransferAppService.TransferAsync(input);

        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 100
        });
        list.Items.Count.ShouldBe(1);
        list.Items[0].TransferAmount.ShouldBe(input.TransferAmount);
        list.Items[0].ReceiveAmount.ShouldBe(100);
        list.Items[0].TransferToken.Id.ShouldBe(tokenTransfer.Id);
        list.Items[0].Type.ShouldBe(CrossChainType.Heterogeneous);
        list.Items[0].FromAddress.ShouldBe(input.FromAddress);
        list.Items[0].ToAddress.ShouldBe(input.ToAddress);
        list.Items[0].FromChainId.ShouldBe(input.FromChainId);
        list.Items[0].ToChainId.ShouldBe(input.ToChainId);
        list.Items[0].TransferBlockHeight.ShouldBe(input.TransferBlockHeight);
        list.Items[0].TransferTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(input.TransferTime));
        list.Items[0].ReceiptId.ShouldBe(input.ReceiptId);
        list.Items[0].Progress.ShouldBe(100);
        list.Items[0].ReceiveTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(receiveInput.ReceiveTime));
        list.Items[0].ReceiveTransactionId.ShouldBe(receiveInput.ReceiveTransactionId);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Received);
        list.Items[0].ReceiveToken.Id.ShouldBe(tokenReceived.Id);

        status = await _crossChainTransferAppService.GetStatusAsync(new GetCrossChainTransferStatusInput
        {
            Ids = { list.Items[0].Id }
        });
        status.Items.Count.ShouldBe(1);
        status.Items[0].Progress.ShouldBe(100);
    }

    [Fact]
    public async Task GetListTest()
    {
        var transfers = new List<CrossChainTransfer>();
        transfers.Add(new CrossChainTransfer
        {
            FromAddress = "FromAddress1",
            ToAddress = "ToAddress1",
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            Status = CrossChainStatus.Indexed,
            Type = CrossChainType.Homogeneous,
        });
        transfers.Add(new CrossChainTransfer
        {
            FromAddress = "FromAddress2",
            ToAddress = "ToAddress2",
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVW",
            Status = CrossChainStatus.Received,
            Type = CrossChainType.Homogeneous,
        });
        transfers.Add(new CrossChainTransfer
        {
            FromAddress = "FromAddress3",
            ToAddress = "ToAddress3",
            FromChainId = "SideChain_tDVW",
            ToChainId = "Ethereum",
            Status = CrossChainStatus.Transferred,
            Type = CrossChainType.Heterogeneous,
        });
        transfers.Add(new CrossChainTransfer
        {
            FromAddress = "ToAddress3",
            ToAddress = "FromAddress1",
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            Status = CrossChainStatus.Indexed,
            Type = CrossChainType.Homogeneous,
        });
        transfers.Add(new CrossChainTransfer
        {
            FromAddress = "ToAddress1",
            ToAddress = "FromAddress2",
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            Status = CrossChainStatus.Indexed,
            Type = CrossChainType.Homogeneous,
        });
        transfers.Add(new CrossChainTransfer
        {
            FromAddress = "ToAddress1",
            ToAddress = "FromAddress3",
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            Status = CrossChainStatus.Indexed,
            Type = CrossChainType.Homogeneous,
        });
        transfers.Add(new CrossChainTransfer
        {
            FromAddress = "FromAddress2",
            ToAddress = "ToAddress3",
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            Status = CrossChainStatus.Indexed,
            Type = CrossChainType.Homogeneous,
        });
        transfers.Add(new CrossChainTransfer
        {
            FromAddress = "FromAddress1",
            ToAddress = "ToAddress3",
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            Status = CrossChainStatus.Indexed,
            Type = CrossChainType.Homogeneous,
        });
        transfers.Add(new CrossChainTransfer
        {
            FromAddress = "ToAddress1",
            ToAddress = "FromAddress1",
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            Status = CrossChainStatus.Indexed,
            Type = CrossChainType.Homogeneous,
        });
        await _crossChainTransferRepository.InsertManyAsync(transfers);

        var list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            MaxResultCount = 10
        });
        list.TotalCount.ShouldBe(9);
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            FromAddress = "FromAddress1",
            MaxResultCount = 10
        });
        list.TotalCount.ShouldBe(2);
        list.Items[0].FromAddress.ShouldBe("FromAddress1");
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            ToAddress = "ToAddress1",
            MaxResultCount = 10
        });
        list.TotalCount.ShouldBe(1);
        list.Items[0].ToAddress.ShouldBe("ToAddress1");
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            FromChainId = "SideChain_tDVW",
            MaxResultCount = 10
        });
        list.TotalCount.ShouldBe(1);
        list.Items[0].FromChainId.ShouldBe("SideChain_tDVW");
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            ToChainId = "Ethereum",
            MaxResultCount = 10
        });
        list.TotalCount.ShouldBe(1);
        list.Items[0].ToChainId.ShouldBe("Ethereum");
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            Status = CrossChainStatus.Indexed,
            MaxResultCount = 10
        });
        list.TotalCount.ShouldBe(7);
        list.Items[0].Status.ShouldBe(CrossChainStatus.Indexed);
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            Type = CrossChainType.Heterogeneous,
            MaxResultCount = 10
        });
        list.TotalCount.ShouldBe(1);
        list.Items[0].Type.ShouldBe(CrossChainType.Heterogeneous);
        
        list = await _crossChainTransferAppService.GetListAsync(new GetCrossChainTransfersInput
        {
            Addresses = "FromAddress1,ToAddress1,FromAddress2,ToAddress2",
            MaxResultCount = 10
        });
        list.TotalCount.ShouldBe(8);

    }

    [Fact]
    public async Task Transfer_Repeat_Test()
    {
        var tokenTransfer = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId ="MainChain_AELF",
            Symbol = "ELF"
        });
        
        var input = new CrossChainTransferInput
        {
            TransferAmount = 100,
            FromAddress = "FromAddress",
            ToAddress = "ToAddress",
            TransferTokenId = tokenTransfer.Id,
            FromChainId = "MainChain_AELF",
            ToChainId = "SideChain_tDVV",
            TransferBlockHeight = 100,
            TransferTime = DateTime.UtcNow.AddMinutes(-1),
            TransferTransactionId = "TransferTransactionId"
        };
        await _crossChainTransferAppService.TransferAsync(input);
        await _crossChainTransferAppService.TransferAsync(input);

        var list = await _crossChainTransferRepository.GetListAsync();
        list.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task Receive_Repeat_Test()
    {
        var tokenReceived = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId ="Ethereum",
            Symbol = "ELF"
        });
        
        var receiveInput = new CrossChainReceiveInput
        {
            ReceiveTime = DateTime.UtcNow,
            FromChainId = "MainChain_AELF",
            ToChainId = "Ethereum",
            ReceiveTransactionId = "ReceiveTransactionId",
            ReceiptId = "ReceiptId",
            ReceiveAmount = 100,
            ReceiveTokenId = tokenReceived.Id,
            FromAddress = "FromAddress",
            ToAddress = "ToAddress",
        };
        await _crossChainTransferAppService.ReceiveAsync(receiveInput);

        var list = await _crossChainTransferRepository.GetListAsync();
        list.Count.ShouldBe(1);
    }
}