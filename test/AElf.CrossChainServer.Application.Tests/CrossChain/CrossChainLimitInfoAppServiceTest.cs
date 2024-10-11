using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AElf.Client.Proto;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace AElf.CrossChainServer.CrossChain;

public class CrossChainLimitInfoAppServiceTest
{
    private readonly ILogger<CrossChainLimitInfoAppService> _mockLogger;
    private readonly CrossChainLimitInfoAppService _service;
    private readonly IObjectMapper _mockObjectMapper;
    private readonly IIndexerCrossChainLimitInfoService _mockIndexerCrossChainLimitInfoService;
    private readonly IBridgeContractAppService _mockBridgeContractAppService;
    private readonly IOptionsMonitor<EvmTokensOptions> _mockEvmTokensOptions;
    private readonly ITokenAppService _mockTokenAppService;
    private readonly IChainAppService _mockChainAppService;
    private readonly IOptionsMonitor<CrossChainLimitsOptions> _mockCrossChainLimitsOptions;
    private readonly ITokenSymbolMappingProvider _mockTokenSymbolMappingProvider;


    public CrossChainLimitInfoAppServiceTest()
    {
        _mockObjectMapper = Substitute.For<IObjectMapper>();
        _mockIndexerCrossChainLimitInfoService = Substitute.For<IIndexerCrossChainLimitInfoService>();
        _mockBridgeContractAppService = Substitute.For<IBridgeContractAppService>();
        _mockEvmTokensOptions = Substitute.For<IOptionsMonitor<EvmTokensOptions>>();
        _mockTokenAppService = Substitute.For<ITokenAppService>();
        _mockChainAppService = Substitute.For<IChainAppService>();
        _mockCrossChainLimitsOptions = Substitute.For<IOptionsMonitor<CrossChainLimitsOptions>>();
        _mockTokenSymbolMappingProvider = Substitute.For<ITokenSymbolMappingProvider>();

        _service = new CrossChainLimitInfoAppService(
            _mockIndexerCrossChainLimitInfoService,
            _mockBridgeContractAppService,
            _mockEvmTokensOptions,
            _mockTokenAppService,
            _mockChainAppService,
            _mockCrossChainLimitsOptions,
            _mockTokenSymbolMappingProvider
        );
    }

    [Fact]
    public async void GetCrossChainDailyLimitsAsyncTest()
    {
        // Arrange
        var expectedDtoList = MockIndexerCrossChainLimitInfos();
        
        _mockIndexerCrossChainLimitInfoService.GetAllCrossChainLimitInfoIndexAsync()
            .Returns(expectedDtoList);
        
        _mockCrossChainLimitsOptions.CurrentValue.Returns(MockEthChainIdsOptions());
        
        MockChainAppService();
        
        MockTokenAppService();
        
        // Act
        var result = await _service.GetCrossChainDailyLimitsAsync();
        
        var elfToken = result.Items.Where(r => r.Token.Equals("ELF")).Select(r => r.Allowance).FirstOrDefault();
        
        Assert.Equal(new decimal(0.2010), elfToken);
        
        var firstToken = result.Items.Select(r => r.Token).FirstOrDefault();
        
        //sort of ELF is first
        Assert.Equal("ELF", firstToken);
    }   


    [Fact]
    public async void GetCrossChainRateLimitsAsyncTest()
    {
        // Arrange
        var crossChainLimitInfos = MockIndexerCrossChainLimitInfos();
        _mockIndexerCrossChainLimitInfoService.GetAllCrossChainLimitInfoIndexAsync()
            .Returns(crossChainLimitInfos);

        _mockCrossChainLimitsOptions.CurrentValue.Returns(MockEthChainIdsOptions());
        
        _mockEvmTokensOptions.CurrentValue.Returns(MockEvmTokensOptions());

        MockChainAppService();

        MockTokenAppService();

        MockBridgeContractAppService();

        MockTokenSymbolMappingProvider();

        // Act
        var result = await _service.GetCrossChainRateLimitsAsync();

        // Assert
        Assert.Equal(4, result.Items.Count);
    }

    
    private CrossChainLimitsOptions MockEthChainIdsOptions()
    {
        var evmTokensOptions = new CrossChainLimitsOptions
        {
            ChainIdInfo = new ChainIdInfo
            {
                TokenFirstChainId = "Sepolia",
                ToChainIds = new List<string>
                {
                    "AELF","tDVV"
                }
            },
            
            TokenSortRules = new Dictionary<string, int>()
            {
                {"BTC", 0},
                {"ELF", 1},
                {"USDT", 2},
                {"USDC", 3}
            },
            
            ChainSortRules = new Dictionary<string, int>()
            {
                {"Sepolia-AELF", 1},
                {"Sepolia-tDVV", 2},
                {"AELF-Sepolia", 3},
                {"tDVW-Sepolia", 4},
                {"BSC-AELF", 5},
                {"BSC-tDVV", 6},
                {"AELF-BSC", 7},
                {"tDVV-BSC", 8}
            }
        };
        return evmTokensOptions;
    }
    
    private EvmTokensOptions MockEvmTokensOptions()
    {
        var evmTokensOptions = new EvmTokensOptions
        {
            Tokens = new Dictionary<string, List<TokenInfo>>
            {
                {
                    "Sepolia", new List<TokenInfo>
                    {
                        new TokenInfo { Address = "Sepolia_Address", TargetChainId = "MainChain_AELF" },
                        new TokenInfo { Address = "Sepolia_Address", TargetChainId = "SideChain_tDVV" }
                    }
                },
                {
                    "BSCTest", new List<TokenInfo>
                    {
                        new TokenInfo { Address = "BSCTest_Address", TargetChainId = "MainChain_AELF" },
                        new TokenInfo { Address = "BSCTest_Address", TargetChainId = "SideChain_tDVV" }
                    }
                }
            }
        };
        return evmTokensOptions;
    }

    private List<IndexerCrossChainLimitInfo> MockIndexerCrossChainLimitInfos()
    {
        var fromToChainJsonArray = @"[
            {
              ""FromChainId"": ""BSCTest"",
              ""ToChainId"": ""AELF""
            },
            {
              ""FromChainId"": ""AELF"",
              ""ToChainId"": ""BSCTest""
            },
            {
              ""FromChainId"": ""AELF"",
              ""ToChainId"": ""Sepolia""
            },
            {
              ""FromChainId"": ""Sepolia"",
              ""ToChainId"": ""AELF""
            },
            {
              ""FromChainId"": ""tDVV"",
              ""ToChainId"": ""BSCTest""
            },
            {
              ""FromChainId"": ""BSCTest"",
              ""ToChainId"": ""tDVV""            
            },
            {
              ""FromChainId"": ""tDVV"",
              ""ToChainId"": ""Sepolia""
            },
            {
              ""FromChainId"": ""Sepolia"",
              ""ToChainId"": ""tDVV""
            }
          ]";
        var fromToChainList = JsonSerializer.Deserialize<List<IndexerCrossChainLimitInfo>>(fromToChainJsonArray);
        var aelfChains = new HashSet<string> { "AELF", "tDVV" };
        var tokenList = new List<string>(new [] { "ELF", "USDT"});
        var crossChainLimitInfos = new List<IndexerCrossChainLimitInfo>();
        for (var i = 0; i < tokenList.Count; i++)
        {
            for (var j = 0; j < fromToChainList.Count; j++)
            {
                var limit = fromToChainList[j];
                var t = (i + j) * 10000;
                var info = new IndexerCrossChainLimitInfo();
                info.FromChainId = limit.FromChainId;
                info.ToChainId = limit.ToChainId;
                info.LimitType = aelfChains.Contains(info.ToChainId) ? CrossChainLimitType.Swap : CrossChainLimitType.Receipt;
                info.Symbol = tokenList[i];
                info.DefaultDailyLimit = 10000000 + t;
                info.RefreshTime = DateTime.UtcNow;
                info.CurrentDailyLimit = 8000000 + t;
                info.Capacity = 5000000 + t;
                info.RefillRate = 100000 + t;
                info.IsEnable = true;
                info.CurrentBucketTokenAmount = 2500000 + t;
                info.BucketUpdateTime = DateTime.UtcNow;
                info.Id = info.FromChainId + "-" + info.ToChainId + "-" + info.Symbol;
                crossChainLimitInfos.Add(info);
            }
        }
        return crossChainLimitInfos;
    }

    private void MockChainAppService()
    {
        var aelfChain = new ChainDto
        {
            Id = "MainChain_AELF",
            IsMainChain = true,
            Type = BlockchainType.AElf,
            BlockChain = "AElf",
            AElfChainId = 9992731
        };
        
        var tDVVChain = new ChainDto
        {
            Id = "SideChain_tDVV",
            IsMainChain = false,
            Type = BlockchainType.AElf,
            BlockChain = "AElf",
            AElfChainId = 1866392

        };
        _mockChainAppService.GetAsync(Arg.Is<string>(s => "MainChain_AELF".Contains(s)))
            .Returns(aelfChain);
        _mockChainAppService.GetAsync(Arg.Is<string>(s => "SideChain_tDVV".Contains(s)))
            .Returns(tDVVChain);
        _mockChainAppService.GetAsync(Arg.Is<string>(s =>  "Sepolia".Contains(s)))
            .Returns(new ChainDto
            {
                Id = "Sepolia",
                IsMainChain = true,
                Type = BlockchainType.Evm,
                BlockChain = "ETH"
            });
        
        _mockChainAppService.GetAsync(Arg.Is<string>(s =>  "BSCTest".Contains(s)))
            .Returns(new ChainDto
            {
                Id = "BSCTest",
                IsMainChain = true,
                Type = BlockchainType.Evm,
                BlockChain = "BSC"
            });
        
        _mockChainAppService.GetByAElfChainIdAsync(Arg.Is<int>(s => s == 9992731))
            .Returns(aelfChain);
        _mockChainAppService.GetByAElfChainIdAsync(Arg.Is<int>(s => s == 1866392))
            .Returns(aelfChain);
    }

    private void MockTokenAppService()
    {

        var tokenDict = new Dictionary<string, int>
        {
            { "ELF", 8 },
            { "USDT", 6 }
        };
        
        var chainIds = new HashSet<string> { "MainChain_AELF", "SideChain_tDVV", "Sepolia" ,"BSCTest"};
        
        foreach (var token in tokenDict)
        {
            
            foreach (var chainId in chainIds)
            {
                _mockTokenAppService
                    .GetAsync(Arg.Is<GetTokenInput>(input => chainId.Contains(input.ChainId) 
                                                             && (input.Symbol == token.Key || input.Address.Contains(chainId))))
                    .Returns(new TokenDto
                    {
                        Symbol = token.Key, Id = new Guid(), Decimals = token.Value
                    });
            }

        }
    }
    
    private void MockTokenSymbolMappingProvider()
    {
        
        var symbols = new HashSet<string> { "ELF", "USDT", "BTC" ,"USDC"};

        foreach (var symbol in symbols)
        {
            _mockTokenSymbolMappingProvider
                .GetMappingSymbol(Arg.Any<string>(), Arg.Any<string>(), symbol)
                .Returns(symbol);
        }
    }

    private void MockBridgeContractAppService()
    {
        _mockBridgeContractAppService
            .GetCurrentReceiptTokenBucketStatesAsync(Arg.Is("Sepolia"), Arg.Any<List<Guid>>(), Arg.Any<List<string>>())
            .Returns(new List<TokenBucketDto>()
            {
                new()
                {
                    Capacity = 121,
                    RefillRate = 102,
                    MaximumTimeConsumed = 3
                }
            });

        _mockBridgeContractAppService
            .GetCurrentSwapTokenBucketStatesAsync(Arg.Is("Sepolia"), Arg.Any<List<Guid>>(), Arg.Any<List<string>>())
            .Returns(new List<TokenBucketDto>()
            {
                new()
                {
                    Capacity = 122,
                    RefillRate = 103,
                    MaximumTimeConsumed = 4
                }
            });
        
        
        _mockBridgeContractAppService
            .GetCurrentReceiptTokenBucketStatesAsync(Arg.Is("BSCTest"), Arg.Any<List<Guid>>(), Arg.Any<List<string>>())
            .Returns(new List<TokenBucketDto>()
            {
                new()
                {
                    Capacity = 121,
                    RefillRate = 102,
                    MaximumTimeConsumed = 3
                }
            });

        _mockBridgeContractAppService
            .GetCurrentSwapTokenBucketStatesAsync(Arg.Is("BSCTest"), Arg.Any<List<Guid>>(), Arg.Any<List<string>>())
            .Returns(new List<TokenBucketDto>()
            {
                new()
                {
                    Capacity = 122,
                    RefillRate = 103,
                    MaximumTimeConsumed = 4
                }
            });
    }
}