using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.Tokens;
using AElf.CSharp.Core;
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
using Volo.Abp.Uow;

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
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly TonConfigOption _tonConfigOption;
    private readonly SyncStateServiceOption _syncStateServiceOption;

    private const int PageCount = 1000;

    public CrossChainTransferAppService(ICrossChainTransferRepository crossChainTransferRepository,
        IChainAppService chainAppService,
        INESTRepository<CrossChainTransferIndex, Guid> crossChainTransferIndexRepository,
        ITokenRepository tokenRepository,
        IBlockchainAppService blockchainAppService,
        ICheckTransferProvider checkTransferProvider,
        IEnumerable<ICrossChainTransferProvider> crossChainTransferProviders,
        IIndexerAppService indexerAppService, ITokenAppService tokenAppService,
        IOptionsSnapshot<AutoReceiveConfigOptions> autoReceiveConfigOptions, IUnitOfWorkManager unitOfWorkManager,
        IOptionsSnapshot<TonConfigOption> tonConfigOption,
        IOptionsSnapshot<SyncStateServiceOption> syncStateServiceOption)
    {
        _crossChainTransferRepository = crossChainTransferRepository;
        _chainAppService = chainAppService;
        _crossChainTransferIndexRepository = crossChainTransferIndexRepository;
        _tokenRepository = tokenRepository;
        _blockchainAppService = blockchainAppService;
        _checkTransferProvider = checkTransferProvider;
        _indexerAppService = indexerAppService;
        _tokenAppService = tokenAppService;
        _unitOfWorkManager = unitOfWorkManager;
        _autoReceiveConfigOptions = autoReceiveConfigOptions.Value;
        _crossChainTransferProviders = crossChainTransferProviders.ToList();
        _tonConfigOption = tonConfigOption.Value;
        _syncStateServiceOption = syncStateServiceOption.Value;
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

            if (TonAddressHelper.IsTonFriendlyAddress(input.FromAddress))
            {
                shouldFromQuery.Add(q => q.Term(i =>
                    i.Field(f => f.FromAddress).Value(TonAddressHelper.GetTonRawAddress(input.FromAddress))));
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

            if (TonAddressHelper.IsTonFriendlyAddress(input.ToAddress))
            {
                shouldToQuery.Add(q => q.Term(i =>
                    i.Field(f => f.ToAddress).Value(TonAddressHelper.GetTonRawAddress(input.ToAddress))));
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

                if (TonAddressHelper.IsTonFriendlyAddress(address))
                {
                    var rawAddress = TonAddressHelper.GetTonRawAddress(address);
                    shouldAddressesQuery.Add(q => q.Bool(b => b.Should(
                        s => s.Term(i => i.Field(f => f.FromAddress).Value(rawAddress)),
                        s => s.Term(i => i.Field(f => f.ToAddress).Value(rawAddress))
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

        var items = ObjectMapper.Map<List<CrossChainTransferIndex>, List<CrossChainTransferIndexDto>>(list.Item2)
            .Select(dto =>
            {
                dto.FromAddress = TonAddressHelper.ConvertRawAddressToFriendly(dto.FromAddress,
                    _tonConfigOption.IsTestOnly, _tonConfigOption.IsBounceable);
                dto.ToAddress = TonAddressHelper.ConvertRawAddressToFriendly(dto.ToAddress, _tonConfigOption.IsTestOnly,
                    _tonConfigOption.IsBounceable);
                return dto;
            })
            .ToList();

        return new PagedResultDto<CrossChainTransferIndexDto>
        {
            TotalCount = totalCount.Count,
            Items = items
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
                "New transfer {transferTxId},{receiptId},{status}", input.TransferTransactionId, input.ReceiptId,input.TransferStatus);
            isTransferExist = false;
            transfer = ObjectMapper.Map<CrossChainTransferInput, CrossChainTransfer>(input);
            transfer.ReceiveStatus = ReceiptStatus.Initializing;
            transfer.Type = await GetCrossChainTypeAsync(input.FromChainId, input.ToChainId);
            transfer.Progress = 0;
            transfer.ProgressUpdateTime = input.TransferTime;
            transfer.Status = CrossChainStatus.Transferred;
        }
        else
        {
            Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                .Debug(
                    "Update transfer {transferTxId},{receiptId},{status}", input.TransferTransactionId, input.ReceiptId,
                    input.TransferStatus);
            transfer.FromAddress = input.FromAddress;
            transfer.TransferTokenId = input.TransferTokenId;
            transfer.TransferTransactionId = input.TransferTransactionId;
            transfer.TransferAmount = input.TransferAmount;
            transfer.TransferTime = input.TransferTime;
            transfer.TransferBlockHeight = input.TransferBlockHeight;
            transfer.TransferStatus = input.TransferStatus;
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
        MethodName = nameof(HandleTransferDbException))]
    public virtual Task UpdateCrossChainTransferAsync(CrossChainTransfer transfer)
    {
        return _crossChainTransferRepository.UpdateAsync(transfer, true);
    }

    [ExceptionHandler(typeof(DbUpdateConcurrencyException), typeof(DbUpdateException),
        TargetType = typeof(CrossChainTransferAppService),
        MethodName = nameof(HandleTransferDbException))]
    public virtual Task InsertCrossChainTransferAsync(CrossChainTransfer transfer)
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
                if (transfer != null)
                {
                    transfer.ReceiveTokenId = receiveInput.ReceiveTokenId;
                    transfer.ReceiveTransactionId = receiveInput.ReceiveTransactionId;
                    transfer.ReceiveTime = receiveInput.ReceiveTime;
                    transfer.ReceiveAmount = receiveInput.ReceiveAmount;

                    transfer.ReceiveBlockHeight = receiveInput.ReceiveBlockHeight;
                    transfer.ReceiveStatus = receiveInput.ReceiveStatus;
                    if (receiveInput.ReceiveStatus == ReceiptStatus.Confirmed)
                    {
                        transfer.Status = CrossChainStatus.Received;
                    }

                    transfer.Progress = CrossChainServerConsts.FullOfTheProgress;
                    transfer.ProgressUpdateTime = receiveInput.ReceiveTime;

                    await _crossChainTransferRepository.UpdateAsync(transfer);
                }
                else
                {
                    Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                        .Error(
                            ex,
                            "Unable to handle unique constraint for transfer {TransferTransactionId} and receipt {ReceiptId}.",
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
            "Receive transfer {transferTxId},{receiptId},{receiveTxId}", input.TransferTransactionId, input.ReceiptId,
            input.ReceiveTransactionId);
        var transfer = await FindCrossChainTransferAsync(input.FromChainId, input.ToChainId,
            input.TransferTransactionId, input.ReceiptId);

        var isTransferExist = true;
        if (transfer == null)
        {
            Log.ForContext("fromChainId", input.FromChainId).ForContext("toChainId", input.ToChainId).Information(
                "New receive {transferTxId},{receiptId},{receiveTxId},{status}", input.TransferTransactionId, input.ReceiptId,
                input.ReceiveTransactionId,input.ReceiveStatus);
            isTransferExist = false;
            transfer = ObjectMapper.Map<CrossChainReceiveInput, CrossChainTransfer>(input);
            transfer.Type = await GetCrossChainTypeAsync(input.FromChainId, input.ToChainId);
            transfer.TransferStatus = ReceiptStatus.Confirmed;
        }
        else
        {
            Log.ForContext("fromChainId", input.FromChainId).ForContext("toChainId", input.ToChainId).Information(
                "Update receive {transferTxId},{receiptId},{receiveTxId},{status}", transfer.TransferTransactionId,
                input.ReceiptId,
                input.ReceiveTransactionId,input.ReceiveStatus);
            transfer.ReceiveTokenId = input.ReceiveTokenId;
            transfer.ReceiveTransactionId = input.ReceiveTransactionId;
            transfer.ReceiveTime = input.ReceiveTime;
        }

        transfer.ReceiveAmount = input.ReceiveAmount;
        transfer.ReceiveBlockHeight = input.ReceiveBlockHeight;
        transfer.ReceiveStatus = input.ReceiveStatus;
        transfer.Status = CrossChainStatus.Indexed;
        if (input.ReceiveStatus == ReceiptStatus.Confirmed)
        {
            transfer.Status = CrossChainStatus.Received;
        }

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

    public async Task DeleteCrossChainTransferAsync(string fromChainId, string toChainId, string transferTransactionId,
        string receiptId)
    {
        var transfer = await FindCrossChainTransferAsync(fromChainId, toChainId,
            transferTransactionId, receiptId);
        if (transfer != null)
        {
            Log.Debug("Delete transfer {transferTxId},{receiptId}", transfer.TransferTransactionId,
                transfer.ReceiptId);
            await _crossChainTransferRepository.DeleteAsync(transfer);
        }
    }

    [ExceptionHandler(typeof(Exception), typeof(InvalidOperationException),
        Message = "Find cross chain transfer failed.", ReturnDefault = ReturnDefault.Default,
        LogTargets = new[] { "fromChainId", "toChainId", "transferTransactionId", "receiptId" })]
    public virtual async Task<CrossChainTransfer> FindCrossChainTransferAsync(string fromChainId, string toChainId,
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

    public async Task DeleteIndexAsync(Guid id)
    {
        await _crossChainTransferIndexRepository.DeleteAsync(id);
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
        using var uow = _unitOfWorkManager.Begin();
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.Status == CrossChainStatus.Transferred && o.TransferStatus == ReceiptStatus.Confirmed && 
                        o.Progress != CrossChainServerConsts.FullOfTheProgress && o.ProgressUpdateTime > DateTime.UtcNow.AddDays(-1))
            .OrderBy(o => o.ProgressUpdateTime)
            .Skip(PageCount * page)
            .Take(PageCount));
        await uow.CompleteAsync();
        return crossChainTransfers;
    }

    [ExceptionHandler(typeof(Exception), Message = "Update receive transaction failed.")]
    public virtual async Task UpdateReceiveTransactionAsync()
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
                    Log.Debug("[UpdateReceiveTransactionAsync] Transaction failed. TransferTransactionId:{id}, ReceiveTransactionId:{txId}",
                        transfer.TransferTransactionId,transfer.ReceiveTransactionId);
                    transfer.ReceiveTransactionId = null;
                    transfer.ReceiveStatus = ReceiptStatus.Initializing;
                    transfer.ReceiveBlockHeight = 0;
                    transfer.ReceiveTime = new DateTime();
                    transfer.ReceiveAmount = 0;
                    toUpdate.Add(transfer);
                }
                else if (txResult.IsMined)
                {
                    Log.Debug("[UpdateReceiveTransactionAsync] Transaction mined. TransferTransactionId:{id}, ReceiveTransactionId:{txId}",
                        transfer.TransferTransactionId,transfer.ReceiveTransactionId);
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
        using var uow = _unitOfWorkManager.Begin();
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.Status == CrossChainStatus.Indexed &&
                        o.Progress == CrossChainServerConsts.FullOfTheProgress && o.ReceiveTransactionId != null &&
                        o.Type == CrossChainType.Homogeneous)
            .OrderBy(o => o.ProgressUpdateTime)
            .Skip(PageCount * page)
            .Take(PageCount));
        await uow.CompleteAsync();
        return crossChainTransfers;
    }

    // This method is used to auto receive the cross chain transfer.
    // Only for homogeneous transfer.
    public async Task AutoReceiveAsync()
    {
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var crossChainTransfers = await GetHomogeneousTransferToReceivedAsync(page);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                    .Debug(
                        "AutoReceive. TransferTransactionId:{id}", transfer.TransferTransactionId);
                var toChain = await _chainAppService.GetAsync(transfer.ToChainId);
                if (toChain == null || toChain.Type != BlockchainType.AElf)
                {
                    continue;
                }

                Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                    .Debug(
                        "Start to auto receive, transferTransactionId:{id}", transfer.TransferTransactionId);

                if (!await _checkTransferProvider.CheckTokenExistAsync(transfer.FromChainId, transfer.ToChainId,
                        transfer.TransferTokenId))
                {
                    Logger.LogInformation(
                        "Token not exist. From chain:{fromChain}, to chain:{toChain}, Id: {transferId}",
                        transfer.FromChainId, transfer.ToChainId, transfer.TransferTransactionId);
                    transfer.ReceiveTransactionAttemptTimes += 1;
                    toUpdate.Add(transfer);
                    continue;
                }

                var provider = GetCrossChainTransferProvider(transfer.Type);
                var txId = await provider.SendReceiveTransactionAsync(transfer);
                if (string.IsNullOrWhiteSpace(txId))
                {
                    transfer.ReceiveTransactionId = txId;
                    transfer.ReceiveTransactionAttemptTimes += 1;
                    toUpdate.Add(transfer);
                    continue;
                }

                transfer.ReceiveTransactionId = txId;
                transfer.ReceiveTransactionAttemptTimes += 1;
                toUpdate.Add(transfer);
                Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                    .Information(
                        "Send auto receive tx: {txId}, Id:{id},TransferTransactionId:{transferId}",
                        txId, transfer.Id, transfer.TransferTransactionId);
            }

            page++;
            crossChainTransfers = await GetHomogeneousTransferToReceivedAsync(page);
        }

        if (toUpdate.Count > 0)
        {
            await _crossChainTransferRepository.UpdateManyAsync(toUpdate);
        }
    }

    public async Task CheckReceiveTransactionAsync()
    {
        Logger.LogInformation("Start to check receive transaction.");
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var crossChainTransfers = await GetHomogeneousToCheckReceivedTransactionAsync(page);
        Logger.LogInformation("Check receive transaction. Count:{count}", crossChainTransfers.Count);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                Logger.LogInformation("Check if the transaction has been received. TransferTransactionId:{id}",
                    transfer.TransferTransactionId);
                var chain = await _chainAppService.GetAsync(transfer.ToChainId);
                var (success, crossChainTransferInfo) =
                    await _indexerAppService.GetPendingTransactionAsync(
                        ChainHelper.ConvertChainIdToBase58(chain.AElfChainId),
                        string.IsNullOrWhiteSpace(transfer.InlineTransferTransactionId)
                            ? transfer.TransferTransactionId
                            : transfer.InlineTransferTransactionId);
                if (!success)
                {
                    continue;
                }

                if (crossChainTransferInfo == null)
                {
                    Logger.LogInformation("Transaction not exist. TransferTransactionId:{id}",
                        transfer.TransferTransactionId);
                    continue;
                }

                if (crossChainTransferInfo.ReceiveTransactionId == null)
                {
                    Logger.LogInformation("Transaction was not received. TransferTransactionId:{id}",
                        transfer.TransferTransactionId);
                    continue;
                }

                Logger.LogInformation("Transaction exist. TransferTransactionId:{id},receiveTransactionId:{receiveId}",
                    transfer.TransferTransactionId, crossChainTransferInfo.ReceiveTransactionId);
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
            crossChainTransfers = await GetHomogeneousToCheckReceivedTransactionAsync(page);
        }

        if (toUpdate.Count > 0)
        {
            await _crossChainTransferRepository.UpdateManyAsync(toUpdate);
        }
    }

    public async Task CheckTransferTransactionConfirmedAsync(string chainId)
    {
        Log.Information(
            "[CheckTransferTransactionConfirmedAsync] Start to check transfer transaction confirmed. ChainId:{ChainId}",
            chainId);
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var currentLib = await _indexerAppService.GetLatestIndexHeightAsync(chainId);
        var crossChainTransfers = await GetPendingTransferTransactionAsync(chainId, currentLib, page);
        Log.Debug("[CheckTransferTransactionConfirmedAsync] Check transfer transaction confirmed. Count:{count}",
            crossChainTransfers.Count);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                Log.Debug(
                    "[CheckTransferTransactionConfirmedAsync] Check if the transaction has been confirmed. TransferTransactionId:{id}, receiptId:{receiptId}",
                    transfer.TransferTransactionId, transfer.ReceiptId);
                var chain = await _chainAppService.GetAsync(transfer.FromChainId);
                bool success;
                CrossChainTransferInfoDto crossChainTransferInfo;
                if (transfer.Type == CrossChainType.Heterogeneous)
                {
                    (success, crossChainTransferInfo) =
                        await _indexerAppService.GetPendingReceiptAsync(
                            ChainHelper.ConvertChainIdToBase58(chain.AElfChainId),
                            transfer.ReceiptId);
                }
                else
                {
                    (success, crossChainTransferInfo) = await _indexerAppService.GetPendingTransactionAsync(
                        ChainHelper.ConvertChainIdToBase58(chain.AElfChainId),
                        transfer.TransferTransactionId);
                }

                switch (success)
                {
                    case false:
                        Log.Warning(
                            "[CheckTransferTransactionConfirmedAsync] Query aefinder failed, retry. TransferTransactionId:{id}, receiptId:{receiptId}",
                            transfer.TransferTransactionId, transfer.ReceiptId);
                        continue;
                    case true when crossChainTransferInfo == null:
                        Log.Warning(
                            "[CheckTransferTransactionConfirmedAsync] Transaction not exist from aefinder, delete. TransferTransactionId:{id}, receiptId:{receiptId}",
                            transfer.TransferTransactionId, transfer.ReceiptId);
                        await DeleteCrossChainTransferAsync(transfer.FromChainId, transfer.ToChainId,
                            transfer.TransferTransactionId, transfer.ReceiptId);
                        continue;
                }

                Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                    .Debug(
                        "[CheckTransferTransactionConfirmedAsync] Update transfer {transferTxId},{receiptId}",
                        transfer.TransferTransactionId,
                        transfer.ReceiptId);
                var token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId = transfer.FromChainId,
                    Symbol = crossChainTransferInfo.TransferTokenSymbol
                });
                transfer.FromAddress = crossChainTransferInfo.FromAddress;
                transfer.TransferTokenId = token.Id;
                transfer.TransferTransactionId = crossChainTransferInfo.TransferTransactionId;
                transfer.TransferAmount = crossChainTransferInfo.TransferAmount / (decimal)Math.Pow(10, token.Decimals);
                transfer.TransferTime = crossChainTransferInfo.TransferTime;
                transfer.TransferBlockHeight = crossChainTransferInfo.TransferBlockHeight;
                transfer.TransferStatus = ReceiptStatus.Confirmed;
                toUpdate.Add(transfer);
            }

            page++;
            crossChainTransfers = await GetPendingTransferTransactionAsync(chainId, currentLib, page);
        }

        if (toUpdate.Count > 0)
        {
            await _crossChainTransferRepository.UpdateManyAsync(toUpdate, true);
        }
    }

    public async Task CheckReceiveTransactionConfirmedAsync(string chainId)
    {
        Log.Information(
            "[CheckReceiveTransactionConfirmedAsync] Start to check receive transaction confirmed. ChainId:{ChainId}",
            chainId);
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var currentLib = await _indexerAppService.GetLatestIndexHeightAsync(chainId);
        var crossChainTransfers = await GetPendingReceiveTransactionAsync(chainId, currentLib, page);
        Log.Debug("[CheckReceiveTransactionConfirmedAsync] Check receive transaction confirmed. Count:{count}",
            crossChainTransfers.Count);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                Log.Debug(
                    "[CheckReceiveTransactionConfirmedAsync] Check if the transaction has been confirmed. ReceiveTransactionId:{id}, receiptId:{receiptId}",
                    transfer.TransferTransactionId, transfer.ReceiptId);
                var chain = await _chainAppService.GetAsync(transfer.ToChainId);
                bool success;
                CrossChainTransferInfoDto crossChainTransferInfo;
                if (transfer.Type == CrossChainType.Heterogeneous)
                {
                    (success, crossChainTransferInfo) =
                        await _indexerAppService.GetPendingReceiptAsync(
                            ChainHelper.ConvertChainIdToBase58(chain.AElfChainId),
                            transfer.ReceiptId);
                }
                else
                {
                    (success, crossChainTransferInfo) = await _indexerAppService.GetPendingReceiveTransactionAsync(
                        ChainHelper.ConvertChainIdToBase58(chain.AElfChainId),
                        transfer.ReceiveTransactionId);
                }
                if (!success)
                {
                    Log.Warning(
                        "[CheckReceiveTransactionConfirmedAsync] Query aefinder failed, retry. ReceiveTransactionId:{id}, receiptId:{receiptId}",
                        transfer.ReceiveTransactionId, transfer.ReceiptId);
                    continue;
                }

                if (success && crossChainTransferInfo == null)
                {
                    Log.Warning(
                        "[CheckReceiveTransactionConfirmedAsync] Transaction not exist from aefinder, delete. ReceiveTransactionId:{id}, receiptId:{receiptId}",
                        transfer.ReceiveTransactionId, transfer.ReceiptId);
                    transfer.ReceiveTransactionId = null;
                    transfer.ReceiveStatus = ReceiptStatus.Initializing;
                    transfer.ReceiveBlockHeight = 0;
                    transfer.ReceiveTime = new DateTime();
                    transfer.ReceiveAmount = 0;
                    toUpdate.Add(transfer);
                    continue;
                }

                Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                    .Debug(
                        "[CheckReceiveTransactionConfirmedAsync] Update receive {receiveTransactionId},{receiptId}",
                        transfer.ReceiveTransactionId,
                        transfer.ReceiptId);
                var token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    ChainId = transfer.FromChainId,
                    Symbol = crossChainTransferInfo.ReceiveTokenSymbol
                });
                transfer.ReceiveTokenId = token.Id;
                transfer.ReceiveTransactionId = crossChainTransferInfo.ReceiveTransactionId;
                transfer.ReceiveTime = crossChainTransferInfo.ReceiveTime;

                transfer.ReceiveAmount = crossChainTransferInfo.ReceiveAmount / (decimal)Math.Pow(10, token.Decimals);
                transfer.ReceiveBlockHeight = crossChainTransferInfo.ReceiveBlockHeight;
                transfer.ReceiveStatus = ReceiptStatus.Confirmed;
                transfer.Status = CrossChainStatus.Received;
                transfer.Progress = CrossChainServerConsts.FullOfTheProgress;
                transfer.ProgressUpdateTime = crossChainTransferInfo.ReceiveTime;
                transfer.TransferStatus = ReceiptStatus.Confirmed;

                toUpdate.Add(transfer);
            }

            page++;
            crossChainTransfers = await GetPendingReceiveTransactionAsync(chainId, currentLib, page);
        }

        if (toUpdate.Count > 0)
        {
            await _crossChainTransferRepository.UpdateManyAsync(toUpdate, true);
        }
    }

    public async Task CheckEvmTransferTransactionConfirmedAsync(string chainId)
    {
        Log.Information(
            "[CheckEvmTransferTransactionConfirmedAsync] Start to check evm transfer transaction confirmed. ChainId:{ChainId}",
            chainId);
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var currentChainStatus = await _blockchainAppService.GetChainStatusAsync(chainId);
        var crossChainTransfers =
            await GetPendingTransferTransactionAsync(chainId, currentChainStatus.ConfirmedBlockHeight, page);
        Log.Debug("[CheckEvmTransferTransactionConfirmedAsync] Check evm transfer transaction confirmed. Count:{count}",
            crossChainTransfers.Count);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                Log.Debug(
                    "[CheckEvmTransferTransactionConfirmedAsync] Check if the evm transaction has been confirmed. TransferTransactionId:{id}, receiptId:{receiptId}",
                    transfer.TransferTransactionId, transfer.ReceiptId);
                var txResult =
                    await GetTransactionResultWithRetryAsync(chainId,
                        transfer.TransferTransactionId);
                if (txResult == null)
                {
                    Log.Error(
                        "[CheckEvmTransferTransactionConfirmedAsync] Transaction {TransactionId} not found on chain {ChainId}",
                        transfer.TransferTransactionId,
                        chainId);
                    await DeleteCrossChainTransferAsync(transfer.FromChainId, transfer.ToChainId,
                        transfer.TransferTransactionId, transfer.ReceiptId);
                }

                if (txResult.IsMined && txResult.BlockHeight == transfer.TransferBlockHeight)
                {
                    Log.Debug(
                        "[CheckEvmTransferTransactionConfirmedAsync] Transaction {TransactionId} is mined on chain {ChainId}",
                        transfer.TransferTransactionId,
                        chainId);
                    Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                        .Debug(
                            "[CheckEvmTransferTransactionConfirmedAsync] Update transfer {transferTxId},{receiptId}",
                            transfer.TransferTransactionId,
                            transfer.ReceiptId);
                    transfer.TransferStatus = ReceiptStatus.Confirmed;
                    toUpdate.Add(transfer);
                }
                
                else if (txResult.IsFailed)
                {
                    Log.Debug(
                        "[CheckEvmTransferTransactionConfirmedAsync] Transaction {TransactionId} failed on chain {ChainId}",
                        transfer.TransferTransactionId,
                        chainId);
                    await DeleteCrossChainTransferAsync(transfer.FromChainId, transfer.ToChainId,
                        transfer.TransferTransactionId, transfer.ReceiptId);
                }
                else
                {
                    Log.Debug(
                        "[CheckEvmTransferTransactionConfirmedAsync] Query from {ChainId} failed. {TransactionId}",
                        chainId, transfer.TransferTransactionId);
                    // retry
                }
            }

            page++;
            crossChainTransfers =
                await GetPendingTransferTransactionAsync(chainId, currentChainStatus.ConfirmedBlockHeight, page);
        }

        if (toUpdate.Count > 0)
        {
            await _crossChainTransferRepository.UpdateManyAsync(toUpdate, true);
        }
    }

    public async Task CheckEvmReceiveTransactionConfirmedAsync(string chainId)
    {
        Log.Information(
            "[CheckEvmReceiveTransactionConfirmedAsync] Start to check evm receive transaction confirmed. ChainId:{ChainId}",
            chainId);
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var currentChainStatus = await _blockchainAppService.GetChainStatusAsync(chainId);
        var crossChainTransfers =
            await GetPendingReceiveTransactionAsync(chainId, currentChainStatus.ConfirmedBlockHeight, page);
        Log.Debug("[CheckEvmReceiveTransactionConfirmedAsync] Check evm receive transaction confirmed. Count:{count}",
            crossChainTransfers.Count);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                Log.Debug(
                    "[CheckEvmReceiveTransactionConfirmedAsync] Check if the evm transaction has been confirmed. receiveTransactionId:{id}, receiptId:{receiptId}",
                    transfer.ReceiveTransactionId, transfer.ReceiptId);
                var txResult =
                    await GetTransactionResultWithRetryAsync(chainId,
                        transfer.ReceiveTransactionId);
                if (txResult == null)
                {
                    Log.Error(
                        "[CheckEvmReceiveTransactionConfirmedAsync] Transaction {TransactionId} not found on chain {ChainId}",
                        transfer.ReceiveTransactionId,
                        chainId);
                    transfer.ReceiveTransactionId = null;
                    transfer.ReceiveStatus = ReceiptStatus.Initializing;
                    transfer.ReceiveBlockHeight = 0;
                    transfer.ReceiveTime = new DateTime();
                    transfer.ReceiveAmount = 0;
                    toUpdate.Add(transfer);
                }

                else if (txResult.IsMined && txResult.BlockHeight == transfer.ReceiveBlockHeight)
                {
                    Log.Debug(
                        "[CheckEvmReceiveTransactionConfirmedAsync] Transaction {TransactionId} is mined on chain {ChainId}",
                        transfer.ReceiveTransactionId,
                        chainId);

                    Log.ForContext("fromChainId", transfer.FromChainId).ForContext("toChainId", transfer.ToChainId)
                        .Debug(
                            "[CheckEvmReceiveTransactionConfirmedAsync] Update receive {transferTxId},{receiptId}",
                            transfer.ReceiveTransactionId,
                            transfer.ReceiptId);
                    transfer.TransferStatus = ReceiptStatus.Confirmed;
                    transfer.ReceiveStatus = ReceiptStatus.Confirmed;
                    transfer.Status = CrossChainStatus.Received;
                    transfer.Progress = CrossChainServerConsts.FullOfTheProgress;
                    toUpdate.Add(transfer);
                }
                else if (txResult.IsFailed)
                {
                    Log.Debug(
                        "[CheckEvmReceiveTransactionConfirmedAsync] Transaction {TransactionId} failed on chain {ChainId}",
                        transfer.ReceiveTransactionId,
                        chainId);
                    transfer.ReceiveTransactionId = null;
                    transfer.ReceiveStatus = ReceiptStatus.Initializing;
                    transfer.ReceiveBlockHeight = 0;
                    transfer.ReceiveTime = new DateTime();
                    transfer.ReceiveAmount = 0;
                    toUpdate.Add(transfer);
                }
                else
                {
                    Log.Debug("[CheckEvmReceiveTransactionConfirmedAsync] Query from {ChainId} failed. {TransactionId}",
                        chainId, transfer.ReceiveTransactionId);
                    // retry
                }
            }

            page++;
            crossChainTransfers =
                await GetPendingReceiveTransactionAsync(chainId, currentChainStatus.ConfirmedBlockHeight, page);
        }

        if (toUpdate.Count > 0)
        {
            await _crossChainTransferRepository.UpdateManyAsync(toUpdate, true);
        }
    }

    private async Task<TransactionResultDto> GetTransactionResultWithRetryAsync(string chainId, string txId)
    {
        const int maxRetries = 3;
        const int delayMilliseconds = 500;

        for (var i = 1; i <= maxRetries; i++)
        {
            var res = await _blockchainAppService.GetTransactionResultAsync(chainId, txId);
            if (res != null && res.ChainId == null)
            {
                Log.Warning("Retry {Retry}/{MaxRetry} failed to query transaction {TxId} on chain {ChainId}",
                    i, maxRetries, txId, chainId);
                if (i == maxRetries)
                {
                    Log.Error("Max retry reached. Failed to query transaction {TxId} on chain {ChainId}", txId,
                        chainId);
                    return new TransactionResultDto();
                }

                await Task.Delay(delayMilliseconds * (int)Math.Pow(2, i - 1));
            }
            else
            {
                return res;
            }
        }

        return new TransactionResultDto();
    }

    private async Task<List<CrossChainTransfer>> GetPendingTransferTransactionAsync(string chainId, long lib,
        int page)
    {
        using var uow = _unitOfWorkManager.Begin();
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.TransferStatus == ReceiptStatus.Pending && o.FromChainId == chainId &&
                        o.TransferBlockHeight <= lib)
            .OrderBy(o => o.TransferBlockHeight)
            .Skip(PageCount * page)
            .Take(PageCount));
        await uow.CompleteAsync();
        return crossChainTransfers;
    }

    private async Task<List<CrossChainTransfer>> GetPendingReceiveTransactionAsync(string chainId, long lib,
        int page)
    {
        using var uow = _unitOfWorkManager.Begin();
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.ReceiveStatus == ReceiptStatus.Pending && o.ToChainId == chainId &&
                        o.ReceiveTransactionId != null &&
                        o.ReceiveBlockHeight > 0 &&
                        o.ReceiveBlockHeight <= lib)
            .OrderBy(o => o.ReceiveTime)
            .Skip(PageCount * page)
            .Take(PageCount));
        await uow.CompleteAsync();
        return crossChainTransfers;
    }

    private async Task<List<CrossChainTransfer>> GetHomogeneousToCheckReceivedTransactionAsync(int page)
    {
        using var uow = _unitOfWorkManager.Begin();
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.Progress == CrossChainServerConsts.FullOfTheProgress &&
                        o.Status == CrossChainStatus.Indexed && o.Type == CrossChainType.Homogeneous)
            .OrderBy(o => o.ProgressUpdateTime)
            .Skip(PageCount * page)
            .Take(PageCount));
        await uow.CompleteAsync();
        return crossChainTransfers;
    }

    // This method is used to get the transfers that need to be received.
    // It will check if the transfer is in progress and if it has not been received yet.
    // Only for homogeneous transfers.
    private async Task<List<CrossChainTransfer>> GetHomogeneousTransferToReceivedAsync(int page)
    {
        using var uow = _unitOfWorkManager.Begin();
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.Progress == CrossChainServerConsts.FullOfTheProgress && o.ReceiveTransactionId == null &&
                        o.ReceiveTransactionAttemptTimes < _autoReceiveConfigOptions.ReceiveRetryTimes &&
                        o.Type == CrossChainType.Homogeneous)
            .OrderBy(o => o.ProgressUpdateTime)
            .Skip(PageCount * page)
            .Take(PageCount));
        await uow.CompleteAsync();
        return crossChainTransfers;
    }

    private ICrossChainTransferProvider GetCrossChainTransferProvider(CrossChainType crossChainType)
    {
        return _crossChainTransferProviders.First(o => o.CrossChainType == crossChainType);
    }
}