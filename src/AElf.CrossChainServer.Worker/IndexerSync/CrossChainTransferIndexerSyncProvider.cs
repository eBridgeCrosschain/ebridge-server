using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Settings;
using AElf.CrossChainServer.Tokens;
using GraphQL;
using GraphQL.Client.Abstractions;
using Volo.Abp.Json;
using Volo.Abp.SettingManagement;

namespace AElf.CrossChainServer.Worker.IndexerSync;

public class CrossChainTransferIndexerSyncProvider : IndexerSyncProviderBase
{
    private readonly ICrossChainTransferAppService _crossChainTransferAppService;
    private readonly ITokenAppService _tokenAppService;

    public CrossChainTransferIndexerSyncProvider(IGraphQLClient graphQlClient, ISettingManager settingManager,
        ICrossChainTransferAppService crossChainTransferAppService, IChainAppService chainAppService,
        ITokenAppService tokenAppService, IJsonSerializer jsonSerializer,
        IIndexerAppService indexerAppService) : base(
        graphQlClient, settingManager,jsonSerializer,indexerAppService,chainAppService)
    {
        _crossChainTransferAppService = crossChainTransferAppService;
        _tokenAppService = tokenAppService;
    }

    protected override string SyncType { get; } = CrossChainServerSettings.CrossChainTransferIndexerSync;

    protected override async Task<long> HandleDataAsync(string aelfChainId, long startHeight, long endHeight)
    {
        var processedHeight = startHeight;

        var data = await QueryDataAsync<CrossChainTransferInfoDto>(GetRequest(aelfChainId, startHeight, endHeight));
        if (data == null || data.CrossChainTransferInfo.Count == 0)
        {
            return processedHeight;
        }

        foreach (var crossChainTransfer in data.CrossChainTransferInfo)
        {
            await HandleDataAsync(crossChainTransfer);
            processedHeight = crossChainTransfer.BlockHeight;
        }

        return processedHeight;
    }

    private async Task HandleDataAsync(CrossChainTransferInfo transfer)
    {
        var chain = await ChainAppService.GetByAElfChainIdAsync(ChainHelper.ConvertBase58ToChainId(transfer.ChainId));

        switch (transfer.TransferType)
        {
            case TransferType.Transfer:
                var toChainId = await GetChainIdAsync(transfer.ToChainId, transfer.CrossChainType);
                if(toChainId == null)
                {
                    return;
                }
                
                var transferToken = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId = chain.Id,
                    Symbol = transfer.TransferTokenSymbol
                });
                
                await _crossChainTransferAppService.TransferAsync(new CrossChainTransferInput
                {
                    TransferAmount = transfer.TransferAmount / (decimal)Math.Pow(10, transferToken.Decimals),
                    FromAddress = transfer.FromAddress,
                    ToAddress = transfer.ToAddress,
                    TransferTokenId = transferToken.Id,
                    FromChainId = chain.Id,
                    ToChainId = toChainId,
                    TransferBlockHeight = transfer.BlockHeight,
                    TransferTime = transfer.TransferTime,
                    TransferTransactionId = transfer.TransferTransactionId
                });
                break;
            case TransferType.Receive:
                var formChainId = await GetChainIdAsync(transfer.FromChainId, transfer.CrossChainType);
                if(formChainId == null)
                {
                    return;
                }

                var receiveToken = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId =chain.Id,
                    Symbol = transfer.ReceiveTokenSymbol
                });
                
                await _crossChainTransferAppService.ReceiveAsync(new CrossChainReceiveInput()
                {
                    ReceiveAmount = transfer.ReceiveAmount / (decimal)Math.Pow(10, receiveToken.Decimals),
                    ReceiveTime = transfer.ReceiveTime,
                    FromChainId = formChainId,
                    ReceiveTransactionId = transfer.ReceiveTransactionId,
                    ToChainId = chain.Id,
                    TransferTransactionId = transfer.TransferTransactionId,
                    ReceiveTokenId = receiveToken.Id,
                    FromAddress = transfer.FromAddress,
                    ToAddress = transfer.ToAddress
                });
                break;
        }
    }

    private async Task<string> GetChainIdAsync(string originalChainId, CrossChainType crossChainType)
    {
        var chainId = originalChainId;
        if (crossChainType == CrossChainType.Homogeneous)
        {
            return chainId;
        }

        var toChain =
            await ChainAppService.GetByAElfChainIdAsync(
                ChainHelper.ConvertBase58ToChainId(originalChainId));
        if (toChain == null)
        {
            return null;
        }

        chainId = toChain.Id;
        return chainId;
    }

    private GraphQLRequest GetRequest(string chainId, long startHeight, long endHeight)
    {
        return new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
            oracleQueryInfo(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight}){
                data{
                    id,
                    chainId,
                    blockHash,
                    blockHeight,
                    blockTime,
                    crossChainType,
                    transferType,
                    fromChainId,
                    toChainId,
                    transferTokenSymbol,
                    transferAmount,
                    transferTime,
                    transferTransactionId,
                    fromAddress,
                    toAddress,
                    receiveTokenSymbol,
                    receiveAmount,
                    receiveTime,
                    receiveTransactionId
                }
            }
        }",
            Variables = new
            {
                chainId = chainId,
                startBlockHeight = startHeight,
                endBlockHeight = endHeight
            }
        };
    }
}

public class CrossChainTransferInfoDto
{
    public List<CrossChainTransferInfo> CrossChainTransferInfo { get; set; }
}

public class CrossChainTransferInfo : GraphQLDto
{
    public string FromChainId { get; set; }
    public string ToChainId { get; set; }
    public string FromAddress { get; set; }
    public string ToAddress { get; set; }
    public string TransferTransactionId { get; set; }
    public string ReceiveTransactionId { get; set; }
    public DateTime TransferTime { get; set; }
    public long TransferBlockHeight { get; set; }
    public DateTime ReceiveTime { get; set; }
    public long TransferAmount { get; set; }
    public long ReceiveAmount { get; set; }
    public string ReceiptId { get; set; }
    public string TransferTokenSymbol { get; set; }
    public string ReceiveTokenSymbol { get; set; }
    public TransferType TransferType { get; set; }
    public CrossChainType CrossChainType { get; set; }
}

public enum TransferType
{
    Transfer,
    Receive
}