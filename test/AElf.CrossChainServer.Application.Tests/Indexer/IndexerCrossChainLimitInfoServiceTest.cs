using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.CrossChain;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AElf.CrossChainServer.Indexer;

public class IndexerCrossChainLimitInfoServiceTest
{
    private readonly IndexerCrossChainLimitInfoService _service;
    private readonly IGraphQLClientFactory _mockGraphQlClientFactory;
    private readonly ILogger<IndexerCrossChainLimitInfoService> _mockLogger;

    public IndexerCrossChainLimitInfoServiceTest()
    {
        _mockGraphQlClientFactory = Substitute.For<IGraphQLClientFactory>();
        _mockLogger = Substitute.For<ILogger<IndexerCrossChainLimitInfoService>>();
    }

    [Fact]
    public async Task GetAllCrossChainLimitInfoIndexAsyncTest()
    {
        // Arrange
        int size = 1000;
        var expectedInfos = new List<IndexerCrossChainLimitInfo>();
        for (int i = 0; i < 1001; i++)
        {
            expectedInfos.Add(new IndexerCrossChainLimitInfo());
        }

        var mockGraphQLClient = Substitute.For<IGraphQLClient>();
        _mockGraphQlClientFactory.GetClient(Arg.Any<GraphQLClientEnum>()).Returns(mockGraphQLClient);

        // mock page query
        int callCount = 0;
        mockGraphQLClient
            .SendQueryAsync<IndexerCrossChainLimitInfos>(Arg.Any<GraphQLRequest>())
            .Returns(info =>
            { 
                int pageSize = size;
                int skip = callCount * pageSize;
                callCount++;
            
                var pagedData = expectedInfos.Skip(skip).Take(pageSize).ToList();
                return Task.FromResult(new GraphQLResponse<IndexerCrossChainLimitInfos>
                {
                    Data = new IndexerCrossChainLimitInfos
                    {
                        Data = new IndexerCrossChainLimitInfos
                        {
                            DataList = pagedData
                        }
                    }
                });
            });

        //IGraphQLClientFactory crest _service
        var _service = new IndexerCrossChainLimitInfoService(_mockGraphQlClientFactory);

        // Act
        var result = await _service.GetAllCrossChainLimitInfoIndexAsync();

        // Assert
        Assert.Equal(expectedInfos.Count, result.Count);

        // check SendQueryAsync request times
        var expectedCalls = (int)Math.Ceiling((expectedInfos.Count / 1000m));
        mockGraphQLClient.Received(expectedCalls).
            SendQueryAsync<IndexerCrossChainLimitInfos>(Arg.Any<GraphQLRequest>());
    }


    [Fact]
    public async Task GetCrossChainLimitInfoIndexAsyncTest()
    {
        // Arrange
        int size = 1000;
        var expectedInfos = new List<IndexerCrossChainLimitInfo>();
        for (int i = 0; i < 3001; i++)
        {
            expectedInfos.Add(new IndexerCrossChainLimitInfo());
        }

        var mockGraphQLClient = Substitute.For<IGraphQLClient>();
        _mockGraphQlClientFactory.GetClient(Arg.Any<GraphQLClientEnum>()).Returns(mockGraphQLClient);

        // mock page query
        int callCount = 0;
        mockGraphQLClient
            .SendQueryAsync<IndexerCrossChainLimitInfos>(Arg.Any<GraphQLRequest>())
            .Returns(info =>
            { 
                int pageSize = size;
                int skip = callCount * pageSize;
                callCount++;
            
                var pagedData = expectedInfos.Skip(skip).Take(pageSize).ToList();
                return Task.FromResult(new GraphQLResponse<IndexerCrossChainLimitInfos>
                {
                    Data = new IndexerCrossChainLimitInfos
                    {
                        Data = new IndexerCrossChainLimitInfos
                        {
                            DataList = pagedData
                        }
                    }
                });
            });

        //IGraphQLClientFactory crest _service
        var _service = new IndexerCrossChainLimitInfoService(_mockGraphQlClientFactory);

        // Act
        var result = await _service.GetAllCrossChainLimitInfoIndexAsync();

        // Assert
        Assert.Equal(expectedInfos.Count, result.Count);

        // check SendQueryAsync request times
        var expectedCalls = (int)Math.Ceiling((expectedInfos.Count / 1000m));
        mockGraphQLClient.Received(expectedCalls).
            SendQueryAsync<IndexerCrossChainLimitInfos>(Arg.Any<GraphQLRequest>());
    }
}