using System;
using System.Threading.Tasks;
using AElf.CrossChainServer.Tokens;
using Shouldly;
using Xunit;

namespace AElf.CrossChainServer.CrossChain;

public class ReportInfoAppServiceTests : CrossChainServerApplicationTestBase
{
    private readonly IReportInfoAppService _reportInfoAppService;
    private readonly IReportInfoRepository _reportInfoRepository;
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;

    public ReportInfoAppServiceTests()
    {
        _reportInfoAppService = GetRequiredService<IReportInfoAppService>();
        _reportInfoRepository = GetRequiredService<IReportInfoRepository>();
        _crossChainTransferAppService = GetRequiredService<ICrossChainTransferAppService>();
        _tokenAppService = GetRequiredService<ITokenAppService>();
    }

    [Fact]
    public async Task CreateTest()
    {
        await CreateCrossChainTransfer();
        var input = new CreateReportInfoInput
        {
            ChainId = "MainChain_AELF",
            Token = "Token",
            ReceiptHash = "ReceiptHash",
            ReceiptId = "ReceiptId",
            RoundId = 1,
            LastUpdateHeight = 100,
            TargetChainId = "Sepolia"
        };
        await _reportInfoAppService.CreateAsync(input);

        var reports = await _reportInfoRepository.GetListAsync();
        reports.Count.ShouldBe(1);
        reports[0].ChainId.ShouldBe(input.ChainId);
        reports[0].Token.ShouldBe(input.Token);
        reports[0].ReceiptHash.ShouldBe(input.ReceiptHash);
        reports[0].ReceiptId.ShouldBe(input.ReceiptId);
        reports[0].RoundId.ShouldBe(input.RoundId);
        reports[0].LastUpdateHeight.ShouldBe(input.LastUpdateHeight);
        reports[0].TargetChainId.ShouldBe(input.TargetChainId);
        reports[0].Step.ShouldBe(ReportStep.Proposed);
        reports[0].Amount.ShouldBe(100);
        reports[0].TargetAddress.ShouldBe("ToAddress");


        await _reportInfoAppService.UpdateStepAsync("MainChain_AELF",1, "Token1", "Sepolia", ReportStep.Confirmed,  150);
        
        reports = await _reportInfoRepository.GetListAsync();
        reports.Count.ShouldBe(1);
        reports[0].LastUpdateHeight.ShouldBe(input.LastUpdateHeight);
        reports[0].Step.ShouldBe(ReportStep.Proposed);
        
        await _reportInfoAppService.UpdateStepAsync("MainChain_AELF",1, "Token", "Sepolia", ReportStep.Confirmed,  150);
        
        reports = await _reportInfoRepository.GetListAsync();
        reports.Count.ShouldBe(1);
        reports[0].LastUpdateHeight.ShouldBe(150);
        reports[0].Step.ShouldBe(ReportStep.Confirmed);
        
        await _reportInfoAppService.UpdateStepAsync("MainChain_AELF",1, "Token", "Sepolia", ReportStep.Proposed,  200);
        
        reports = await _reportInfoRepository.GetListAsync();
        reports.Count.ShouldBe(1);
        reports[0].LastUpdateHeight.ShouldBe(150);
        reports[0].Step.ShouldBe(ReportStep.Confirmed);
    }

    [Fact]
    public async Task Create_Repeat_Test()
    {
        await CreateCrossChainTransfer();
        var input = new CreateReportInfoInput
        {
            ChainId = "MainChain_AELF",
            Token = "Token",
            ReceiptHash = "ReceiptHash",
            ReceiptId = "ReceiptId",
            RoundId = 1,
            LastUpdateHeight = 100,
            TargetChainId = "Sepolia"
        };
        await _reportInfoAppService.CreateAsync(input);
        await _reportInfoAppService.CreateAsync(input);

        var reports = await _reportInfoRepository.GetListAsync();
        reports.Count.ShouldBe(1);
    }

    private async Task CreateCrossChainTransfer()
    {
        var tokenTransfer = await _tokenAppService.GetAsync(new GetTokenInput
        {
            ChainId = "MainChain_AELF",
            Symbol = "ELF"
        });

        var input = new CrossChainTransferInput
        {
            TransferAmount = 100,
            FromAddress = "FromAddress",
            ToAddress = "ToAddress",
            TransferTokenId = tokenTransfer.Id,
            FromChainId = "MainChain_AELF",
            ToChainId = "Sepolia",
            TransferBlockHeight = 100,
            TransferTime = DateTime.UtcNow.AddMinutes(-1),
            TransferTransactionId = "TransferTransactionId",
            ReceiptId = "ReceiptId"
        };
        await _crossChainTransferAppService.TransferAsync(input);
    }
    
    [Fact]
    public async Task Create_ResentTimes_Test()
    {
        await CreateCrossChainTransfer();
        var input1 = new CreateReportInfoInput
        {
            ChainId = "MainChain_AELF",
            Token = "Token",
            ReceiptHash = "ReceiptHash",
            ReceiptId = "ReceiptId",
            RoundId = 1,
            LastUpdateHeight = 100,
            TargetChainId = "Sepolia"
        };
        await _reportInfoAppService.CreateAsync(input1);
        var input2 = new CreateReportInfoInput
        {
            ChainId = "MainChain_AELF",
            Token = "Token",
            ReceiptHash = "ReceiptHash",
            ReceiptId = "ReceiptId",
            RoundId = 2,
            LastUpdateHeight = 100,
            TargetChainId = "Sepolia"
        };
        await _reportInfoAppService.CreateAsync(input2);

        var reports = await _reportInfoRepository.GetListAsync();
        reports.Count.ShouldBe(2);
        reports[0].ResendTimes.ShouldBe(0);
        reports[1].ResendTimes.ShouldBe(1);
    }
}