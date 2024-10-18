using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.ExceptionHandler;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Serilog;

namespace AElf.CrossChainServer.CrossChain;

[RemoteService(IsEnabled = false)]
public partial class CrossChainTransferAppService : CrossChainServerAppService, ICrossChainTransferAppService
{
    private readonly ICrossChainTransferRepository _crossChainTransferRepository;
    private readonly IChainAppService _chainAppService;
    private readonly INESTRepository<CrossChainTransferIndex, Guid> _crossChainTransferIndexRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IBlockchainAppService _blockchainAppService;
    private readonly ICheckTransferProvider _checkTransferProvider;
    private readonly IEnumerable<ICrossChainTransferProvider> _crossChainTransferProviders;
    private readonly IIndexerAppService _indexerAppService;
    private readonly ITokenAppService _tokenAppService;
    private readonly AutoReceiveConfigOptions _autoReceiveConfigOptions;

    private const int PageCount = 1000;

    public CrossChainTransferAppService(ICrossChainTransferRepository crossChainTransferRepository,
        IChainAppService chainAppService,
        INESTRepository<CrossChainTransferIndex, Guid> crossChainTransferIndexRepository,
        ITokenRepository tokenRepository,
        IBlockchainAppService blockchainAppService,
        ICheckTransferProvider checkTransferProvider,
        IEnumerable<ICrossChainTransferProvider> crossChainTransferProviders,
        IIndexerAppService indexerAppService, ITokenAppService tokenAppService,
        IOptionsSnapshot<AutoReceiveConfigOptions> autoReceiveConfigOptions)
    {
        _crossChainTransferRepository = crossChainTransferRepository;
        _chainAppService = chainAppService;
        _crossChainTransferIndexRepository = crossChainTransferIndexRepository;
        _tokenRepository = tokenRepository;
        _blockchainAppService = blockchainAppService;
        _checkTransferProvider = checkTransferProvider;
        _indexerAppService = indexerAppService;
        _tokenAppService = tokenAppService;
        _autoReceiveConfigOptions = autoReceiveConfigOptions.Value;
        _crossChainTransferProviders = crossChainTransferProviders.ToList();
    }

    public async Task<PagedResultDto<CrossChainTransferIndexDto>> GetListAsync(GetCrossChainTransfersInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CrossChainTransferIndex>, QueryContainer>>();

        if (!input.FromChainId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.FromChainId).Value(input.FromChainId)));
        }

        if (!input.ToChainId.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ToChainId).Value(input.ToChainId)));
        }

        if (!input.FromAddress.IsNullOrWhiteSpace())
        {
            var shouldFromQuery = new List<Func<QueryContainerDescriptor<CrossChainTransferIndex>, QueryContainer>>();

            if (!Base58CheckEncoding.Verify(input.FromAddress) &&
                Nethereum.Util.AddressExtensions.IsValidEthereumAddressHexFormat(input.FromAddress))
            {
                shouldFromQuery.Add(q => q.Term(i => i.Field(f => f.FromAddress).Value(input.FromAddress.ToLower())));
            }

            shouldFromQuery.Add(q => q.Term(i => i.Field(f => f.FromAddress).Value(input.FromAddress)));

            mustQuery.Add(q => q.Bool(bb => bb
                .MinimumShouldMatch(1)
                .Should(shouldFromQuery)
            ));
        }

        if (!input.ToAddress.IsNullOrWhiteSpace())
        {
            var shouldToQuery = new List<Func<QueryContainerDescriptor<CrossChainTransferIndex>, QueryContainer>>();

            if (!Base58CheckEncoding.Verify(input.ToAddress) &&
                Nethereum.Util.AddressExtensions.IsValidEthereumAddressHexFormat(input.ToAddress))
            {
                shouldToQuery.Add(q => q.Term(i => i.Field(f => f.ToAddress).Value(input.ToAddress.ToLower())));
            }

            shouldToQuery.Add(q => q.Term(i => i.Field(f => f.ToAddress).Value(input.ToAddress)));

            mustQuery.Add(q => q.Bool(bb => bb
                .MinimumShouldMatch(1)
                .Should(shouldToQuery)
            ));
        }

        if (!input.Addresses.IsNullOrWhiteSpace())
        {
            var addressList = input.Addresses.Split(',');
            var shouldAddressesQuery =
                new List<Func<QueryContainerDescriptor<CrossChainTransferIndex>, QueryContainer>>();
            foreach (var address in addressList)
            {
                if (!Base58CheckEncoding.Verify(address) &&
                    Nethereum.Util.AddressExtensions.IsValidEthereumAddressHexFormat(address))
                {
                    shouldAddressesQuery.Add(q => q.Bool(b => b.Should(
                        s => s.Term(i => i.Field(f => f.FromAddress).Value(address.ToLower())),
                        s => s.Term(i => i.Field(f => f.ToAddress).Value(address.ToLower()))
                    )));
                }

                shouldAddressesQuery.Add(q => q.Bool(b => b.Should(
                        s => s.Term(i => i.Field(f => f.FromAddress).Value(address)),
                        s => s.Term(i => i.Field(f => f.ToAddress).Value(address))
                    )));
            }

            mustQuery.Add(q => q.Bool(bb => bb
                .MinimumShouldMatch(1)
                .Should(shouldAddressesQuery)
            ));
        }

        if (input.Status.HasValue)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Status).Value(input.Status.Value)));
        }

        if (input.Type.HasValue)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Type).Value(input.Type.Value)));
        }

        QueryContainer Filter(QueryContainerDescriptor<CrossChainTransferIndex> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _crossChainTransferIndexRepository.GetListAsync(Filter, limit: input.MaxResultCount,
            skip: input.SkipCount, sortExp: o => o.TransferTime, sortType: SortOrder.Descending);
        var totalCount = await _crossChainTransferIndexRepository.CountAsync(Filter);

        return new PagedResultDto<CrossChainTransferIndexDto>
        {
            TotalCount = totalCount.Count,
            Items = ObjectMapper.Map<List<CrossChainTransferIndex>, List<CrossChainTransferIndexDto>>(list.Item2)
        };
    }

    public async Task<ListResultDto<CrossChainTransferStatusDto>> GetStatusAsync(GetCrossChainTransferStatusInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CrossChainTransferIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Ids(i => i.Values(input.Ids)));

        QueryContainer Filter(QueryContainerDescriptor<CrossChainTransferIndex> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _crossChainTransferIndexRepository.GetListAsync(Filter);
        return new ListResultDto<CrossChainTransferStatusDto>
        {
            Items = ObjectMapper.Map<List<CrossChainTransferIndex>, List<CrossChainTransferStatusDto>>(list.Item2)
        };
    }

    public async Task TransferAsync(CrossChainTransferInput input)
    {
        var transfer = await FindCrossChainTransferAsync(input.FromChainId, input.ToChainId,
            input.TransferTransactionId, input.ReceiptId);

        var isTransferExist = true;
        if (transfer == null)
        {
            Log.Information(
                "New transfer {TransferTransactionId}", input.TransferTransactionId);
            isTransferExist = false;
            transfer = ObjectMapper.Map<CrossChainTransferInput, CrossChainTransfer>(input);
            transfer.Type = await GetCrossChainTypeAsync(input.FromChainId, input.ToChainId);
            transfer.Progress = 0;
            transfer.ProgressUpdateTime = input.TransferTime;
            transfer.Status = CrossChainStatus.Transferred;
        }
        else
        {
            Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                .Debug(
                "Update transfer {TransferTransactionId}", input.TransferTransactionId);
            transfer.TransferTokenId = input.TransferTokenId;
            transfer.TransferTransactionId = input.TransferTransactionId;
            transfer.TransferAmount = input.TransferAmount;
            transfer.TransferTime = input.TransferTime;
            transfer.TransferBlockHeight = input.TransferBlockHeight;
        }

        if (isTransferExist)
        {
            await UpdateCrossChainTransferAsync(transfer);
        }
        else
        {
            await InsertCrossChainTransferAsync(transfer);
        }
    }

    [ExceptionHandler(typeof(DbUpdateConcurrencyException), typeof(DbUpdateException),
        TargetType = typeof(CrossChainTransferAppService),
        MethodName = nameof(HandleDbException))]
    private Task UpdateCrossChainTransferAsync(CrossChainTransfer transfer)
    {
        return _crossChainTransferRepository.UpdateAsync(transfer);
    }

    [ExceptionHandler(typeof(DbUpdateConcurrencyException), typeof(DbUpdateException),
        TargetType = typeof(CrossChainTransferAppService),
        MethodName = nameof(HandleDbException))]
    private Task InsertCrossChainTransferAsync(CrossChainTransfer transfer)
    {
        return _crossChainTransferRepository.InsertAsync(transfer, autoSave: true);
    }

    private async Task HandleUniqueTransfer<T>(Exception ex, T input)
    {
        if (ex.InnerException is MySqlException mySqlEx && mySqlEx.Number == 1062)
        {
            if (typeof(T) == typeof(CrossChainTransferInput))
            {
                var transferInput = input as CrossChainTransferInput;
                var transfer = await _crossChainTransferRepository.FindAsync(o =>
                    o.FromChainId == transferInput.FromChainId && o.ToChainId == transferInput.ToChainId &&
                    o.TransferTransactionId == transferInput.TransferTransactionId);
                ;

                if (transfer != null)
                {
                    transfer.TransferTokenId = transferInput.TransferTokenId;
                    transfer.TransferTransactionId = transferInput.TransferTransactionId;
                    transfer.TransferAmount = transferInput.TransferAmount;
                    transfer.TransferTime = transferInput.TransferTime;
                    transfer.TransferBlockHeight = transferInput.TransferBlockHeight;

                    await _crossChainTransferRepository.UpdateAsync(transfer);
                }
                else
                {
                    Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                        .Error(ex,
                        "Unable to handle unique constraint for transfer {TransferTransactionId} and receipt {ReceiptId}.",
                        transferInput.TransferTransactionId, transferInput.ReceiptId);
                }
            }
            else if (typeof(T) == typeof(CrossChainReceiveInput))
            {
                var receiveInput = input as CrossChainReceiveInput;
                var transfer = await _crossChainTransferRepository.FindAsync(o =>
                    o.FromChainId == receiveInput.FromChainId && o.ToChainId == receiveInput.ToChainId &&
                    o.TransferTransactionId == receiveInput.TransferTransactionId);
                ;
                if (transfer != null)
                {
                    transfer.ReceiveTokenId = receiveInput.ReceiveTokenId;
                    transfer.ReceiveTransactionId = receiveInput.ReceiveTransactionId;
                    transfer.ReceiveTime = receiveInput.ReceiveTime;
                    transfer.ReceiveAmount = receiveInput.ReceiveAmount;

                    transfer.Status = CrossChainStatus.Received;
                    transfer.Progress = CrossChainServerConsts.FullOfTheProgress;
                    transfer.ProgressUpdateTime = receiveInput.ReceiveTime;

                    await _crossChainTransferRepository.UpdateAsync(transfer);
                }
                else
                {
                    Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                        .Error(
                        ex,"Unable to handle unique constraint for transfer {TransferTransactionId} and receipt {ReceiptId}.",
                        receiveInput.TransferTransactionId, receiveInput.ReceiptId);
                }
            }
        }
        else
        {
            Log.Error(ex,
                "Failed to insert or update transfer info.");
        }
    }

    public async Task ReceiveAsync(CrossChainReceiveInput input)
    {
        Log.ForContext("fromChainId", input.FromChainId).ForContext("toChainId", input.ToChainId).Information(
            "Receive transfer {TransferTransactionId}", input.TransferTransactionId);
        var transfer = await FindCrossChainTransferAsync(input.FromChainId, input.ToChainId,
            input.TransferTransactionId, input.ReceiptId);

        var isTransferExist = true;
        if (transfer == null)
        {
            Logger.LogDebug("New receive {TransferTransactionId} from {FromChainId} to {ToChainId}",
                input.TransferTransactionId, input.FromChainId, input.ToChainId);
            isTransferExist = false;
            transfer = ObjectMapper.Map<CrossChainReceiveInput, CrossChainTransfer>(input);
            transfer.Type = await GetCrossChainTypeAsync(input.FromChainId, input.ToChainId);
        }
        else
        {
            Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                .Debug(
                    "Update receive {TransferTransactionId}", input.TransferTransactionId);
            transfer.ReceiveTokenId = input.ReceiveTokenId;
            transfer.ReceiveTransactionId = input.ReceiveTransactionId;
            transfer.ReceiveTime = input.ReceiveTime;
            transfer.ReceiveAmount = input.ReceiveAmount;
        }

        transfer.Status = CrossChainStatus.Received;
        transfer.Progress = CrossChainServerConsts.FullOfTheProgress;
        transfer.ProgressUpdateTime = input.ReceiveTime;
        if (isTransferExist)
        {
            await UpdateCrossChainTransferAsync(transfer);
        }
        else
        {
            await InsertCrossChainTransferAsync(transfer);
        }
    }

    [ExceptionHandler(typeof(Exception),typeof(InvalidOperationException),Message = "Get cross chain transfer failed.",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleException))]
    private async Task<CrossChainTransfer> FindCrossChainTransferAsync(string fromChainId, string toChainId,
        string transferTransactionId, string receiptId)
    {
        var crossChainType = await GetCrossChainTypeAsync(fromChainId, toChainId);
        return await GetCrossChainTransferProvider(crossChainType)
            .FindTransferAsync(fromChainId, toChainId, transferTransactionId, receiptId);
    }

    public async Task AddIndexAsync(AddCrossChainTransferIndexInput input)
    {
        var index = ObjectMapper.Map<AddCrossChainTransferIndexInput, CrossChainTransferIndex>(input);

        if (input.TransferTokenId != Guid.Empty)
        {
            index.TransferToken = await _tokenRepository.GetAsync(input.TransferTokenId);
        }

        if (input.ReceiveTokenId != Guid.Empty)
        {
            index.ReceiveToken = await _tokenRepository.GetAsync(input.ReceiveTokenId);
        }

        await _crossChainTransferIndexRepository.AddAsync(index);
    }

    public async Task UpdateIndexAsync(UpdateCrossChainTransferIndexInput input)
    {
        var index = ObjectMapper.Map<UpdateCrossChainTransferIndexInput, CrossChainTransferIndex>(input);

        if (input.TransferTokenId != Guid.Empty)
        {
            index.TransferToken = await _tokenRepository.GetAsync(input.TransferTokenId);
        }

        if (input.ReceiveTokenId != Guid.Empty)
        {
            index.ReceiveToken = await _tokenRepository.GetAsync(input.ReceiveTokenId);
        }

        await _crossChainTransferIndexRepository.UpdateAsync(index);
    }

    public async Task UpdateProgressAsync()
    {
        var page = 0;
        var crossChainTransfers = await GetToUpdateProgressAsync(page);

        while (crossChainTransfers.Count != 0)
        {
            var now = DateTime.UtcNow;
            var toUpdate = new List<CrossChainTransfer>();
            foreach (var transfer in crossChainTransfers)
            {
                Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                    .Debug(
                        "UpdateProgress.TransferTransactionId:{txId},progress:{progress}",
                        transfer.TransferTransactionId, transfer.Progress);
                var provider = GetCrossChainTransferProvider(transfer.Type);
                var progress = await provider.CalculateCrossChainProgressAsync(transfer);

                if (progress == transfer.Progress)
                {
                    Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                        .Debug(
                            "Progress not changed. TransferTransactionId:{txId}",
                            transfer.TransferTransactionId);
                    continue;
                }

                Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                    .Debug(
                        "Progress changed. Progress:{progress},transferTransactionId:{txId}",
                        progress, transfer.TransferTransactionId);
                transfer.Progress = progress;
                transfer.ProgressUpdateTime = now;
                if (progress == CrossChainServerConsts.FullOfTheProgress)
                {
                    transfer.Status = CrossChainStatus.Indexed;
                }

                toUpdate.Add(transfer);
            }

            await _crossChainTransferRepository.UpdateManyAsync(toUpdate, true);

            page++;
            crossChainTransfers = await GetToUpdateProgressAsync(page);
        }
    }

    private async Task<CrossChainType> GetCrossChainTypeAsync(string fromChainId, string toChainId)
    {
        var fromChain = await _chainAppService.GetAsync(fromChainId);
        var toChain = await _chainAppService.GetAsync(toChainId);

        if (fromChain == null || toChain == null)
        {
            return CrossChainType.Homogeneous;
        }

        return fromChain.Type == toChain.Type ? CrossChainType.Homogeneous : CrossChainType.Heterogeneous;
    }

    private async Task<List<CrossChainTransfer>> GetToUpdateProgressAsync(int page)
    {
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.Status == CrossChainStatus.Transferred && o.ProgressUpdateTime > DateTime.UtcNow.AddDays(-3))
            .OrderBy(o => o.ProgressUpdateTime)
            .Skip(PageCount * page)
            .Take(PageCount));
        return crossChainTransfers;
    }
    
    [ExceptionHandler(typeof(Exception),Message = "Update receive transaction failed.",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionWithOutReturnValue))]
    public async Task UpdateReceiveTransactionAsync()
    {
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var crossChainTransfers = await GetToUpdateReceiveTransactionAsync(page);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                    .Debug(
                        "UpdateReceiveTransaction.TransferTransactionId:{id}", transfer.TransferTransactionId);
                var txResult =
                        await _blockchainAppService.GetTransactionResultAsync(transfer.ToChainId,
                            transfer.ReceiveTransactionId);
                    Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                        .Information(
                            "txResult.TransferTransactionId:{id},is failed:{isFailed}", transfer.TransferTransactionId,
                            txResult.IsFailed);

                    if (txResult.IsFailed)
                    {
                        transfer.ReceiveTransactionId = null;
                        toUpdate.Add(transfer);
                    }
                    else if (txResult.IsMined)
                    {
                        var chainStatus = await _blockchainAppService.GetChainStatusAsync(transfer.ToChainId);
                        if (chainStatus.ConfirmedBlockHeight >= txResult.BlockHeight)
                        {
                            var block = await _blockchainAppService.GetBlockByHeightAsync(transfer.ToChainId,
                                txResult.BlockHeight);
                            if (block.BlockHash != txResult.BlockHash)
                            {
                                transfer.ReceiveTransactionId = null;
                                toUpdate.Add(transfer);
                            }
                        }
                    }
            }

            page++;
            crossChainTransfers = await GetToUpdateReceiveTransactionAsync(page);
        }

        if (toUpdate.Count > 0)
        {
            await _crossChainTransferRepository.UpdateManyAsync(toUpdate);
        }
    }

    private async Task<List<CrossChainTransfer>> GetToUpdateReceiveTransactionAsync(int page)
    {
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.Status == CrossChainStatus.Indexed &&
                        o.Progress == CrossChainServerConsts.FullOfTheProgress && o.ReceiveTransactionId != null)
            .OrderBy(o => o.ProgressUpdateTime)
            .Skip(PageCount * page)
            .Take(PageCount));
        return crossChainTransfers;
    }
    
    [ExceptionHandler(typeof(Exception),Message = "Auto receive transaction failed.",
        TargetType = typeof(CrossChainTransferAppService),
        MethodName = nameof(CrossChainTransferAppService.HandleAutoReceiveException))]
    public async Task AutoReceiveAsync()
    {
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var crossChainTransfers = await GetToReceivedAsync(page);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                    .Debug(
                        "AutoReceive.TransferTransactionId:{id}", transfer.TransferTransactionId);
                var toChain = await _chainAppService.GetAsync(transfer.ToChainId);
                    if (toChain == null)
                    {
                        continue;
                    }

                    if (toChain.Type != BlockchainType.AElf)
                    {
                        continue;
                    }

                    Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                        .Debug(
                            "Start to auto receive, transferTransactionId:{id}", transfer.TransferTransactionId);

                    if (!await _checkTransferProvider.CheckTokenExistAsync(transfer.FromChainId,transfer.ToChainId,transfer.TransferTokenId))
                    {
                        Logger.LogInformation("Token not exist. From chain:{fromChain}, to chain:{toChain}, Id: {transferId}", transfer.FromChainId, transfer.ToChainId, transfer.TransferTransactionId);
                        transfer.ReceiveTransactionAttemptTimes += 1;
                        toUpdate.Add(transfer);
                        continue; 
                    }
                    // Heterogeneous:check limit.
                    if (transfer.Type == CrossChainType.Heterogeneous &&
                        !await _checkTransferProvider.CheckTransferAsync(
                            transfer.FromChainId,
                            transfer.ToChainId, transfer.TransferTokenId, transfer.TransferAmount))
                    {
                        Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                            .Warning(
                                "Incorrect chain or check limit failed, from chain:{fromChainId}, to chain:{toChainId}, Id: {transferId}, transfer amount:{amount}",
                                transfer.FromChainId, transfer.ToChainId, transfer.Id, transfer.TransferAmount);
                        continue;
                    }

                    var provider = GetCrossChainTransferProvider(transfer.Type);
                    var txId = await provider.SendReceiveTransactionAsync(transfer);
                    if (string.IsNullOrWhiteSpace(txId))
                    {
                        continue;
                    }
                    transfer.ReceiveTransactionId = txId;
                    transfer.ReceiveTransactionAttemptTimes += 1;
                    toUpdate.Add(transfer);
                    Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                        .Information(
                            "Send auto receive tx: {txId}, Id:{id},TransferTransactionId:{transferId}",
                            txId, transfer.Id,transfer.TransferTransactionId);
            }

            page++;
            crossChainTransfers = await GetToReceivedAsync(page);
        }

        if (toUpdate.Count > 0)
        {
            await _crossChainTransferRepository.UpdateManyAsync(toUpdate);
        }
    }

    private void AddReceiveTransactionAttemptTimes(CrossChainTransfer transfer,List<CrossChainTransfer> toUpdate)
    {
        transfer.ReceiveTransactionAttemptTimes += 1;
        toUpdate.Add(transfer);
    }

    public async Task CheckReceiveTransactionAsync()
    {
        Logger.LogInformation("Start to check receive transaction.");
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var crossChainTransfers = await GetToCheckReceivedTransactionAsync(page);
        Logger.LogInformation("Check receive transaction. Count:{count}", crossChainTransfers.Count);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                Logger.LogInformation("Check if the transaction has been received. TransferTransactionId:{id}", transfer.TransferTransactionId);
                var chain = await _chainAppService.GetAsync(transfer.ToChainId);
                var crossChainTransferInfo = await _indexerAppService.GetPendingTransactionAsync(ChainHelper.ConvertChainIdToBase58(chain.AElfChainId), transfer.TransferTransactionId);
                if (crossChainTransferInfo == null)
                {
                    Logger.LogInformation("Transaction not exist. TransferTransactionId:{id}", transfer.TransferTransactionId);
                    continue;
                }
                if (crossChainTransferInfo.ReceiveTransactionId == null)
                {
                    Logger.LogInformation("Transaction was not received. TransferTransactionId:{id}", transfer.TransferTransactionId);
                    continue;
                }
                Logger.LogInformation("Transaction exist. TransferTransactionId:{id},receiveTransactionId:{receiveId}", transfer.TransferTransactionId, crossChainTransferInfo.ReceiveTransactionId);
                var receiveToken = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId = transfer.ToChainId,
                    Symbol = crossChainTransferInfo.ReceiveTokenSymbol
                });
                transfer.ReceiveTokenId = receiveToken.Id;
                transfer.ReceiveTransactionId = crossChainTransferInfo.ReceiveTransactionId;
                transfer.ReceiveTime = crossChainTransferInfo.ReceiveTime;
                transfer.ReceiveAmount = crossChainTransferInfo.ReceiveAmount;
                transfer.Status = CrossChainStatus.Received;
                toUpdate.Add(transfer);
            }

            page++;
            crossChainTransfers = await GetToCheckReceivedTransactionAsync(page);
        }

        if (toUpdate.Count > 0)
        {
            await _crossChainTransferRepository.UpdateManyAsync(toUpdate);
        }
    }
    
    private async Task<List<CrossChainTransfer>> GetToCheckReceivedTransactionAsync(int page)
    {
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.Progress == CrossChainServerConsts.FullOfTheProgress && o.Status == CrossChainStatus.Indexed && o.Type == CrossChainType.Homogeneous)
            .OrderBy(o => o.ProgressUpdateTime)
            .Skip(PageCount * page)
            .Take(PageCount));
        return crossChainTransfers;
    }
    
    
    public async Task Test()
    {
        await TestB();
    }

    [ExceptionHandler(typeof(Exception),Message = "failed.",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionWithOutReturnValue))]
    public virtual async Task TestB()
    {
        await TestC();
    }
    
    public async Task TestC()
    {
        throw new Exception();
    }
    
    private async Task<List<CrossChainTransfer>> GetToReceivedAsync(int page)
    {
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.Progress == CrossChainServerConsts.FullOfTheProgress && o.ReceiveTransactionId == null && o.ReceiveTransactionAttemptTimes < _autoReceiveConfigOptions.ReceiveRetryTimes)
            .OrderBy(o => o.ProgressUpdateTime)
            .Skip(PageCount * page)
            .Take(PageCount));
        return crossChainTransfers;
    }

    private ICrossChainTransferProvider GetCrossChainTransferProvider(CrossChainType crossChainType)
    {
        return _crossChainTransferProviders.First(o => o.CrossChainType == crossChainType);
    }
}