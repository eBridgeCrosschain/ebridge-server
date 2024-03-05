using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Tokens;
using AElf.Indexing.Elasticsearch;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChainServer.CrossChain;

[RemoteService(IsEnabled = false)]
public class CrossChainTransferAppService : CrossChainServerAppService, ICrossChainTransferAppService
{
    private readonly ICrossChainTransferRepository _crossChainTransferRepository;
    private readonly IChainAppService _chainAppService;
    private readonly INESTRepository<CrossChainTransferIndex, Guid> _crossChainTransferIndexRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IBlockchainAppService _blockchainAppService;
    private readonly ICheckTransferProvider _checkTransferProvider;
    private readonly IEnumerable<ICrossChainTransferProvider> _crossChainTransferProviders;

    private const int PageCount = 1000;

    public CrossChainTransferAppService(ICrossChainTransferRepository crossChainTransferRepository,
        IChainAppService chainAppService,
        INESTRepository<CrossChainTransferIndex, Guid> crossChainTransferIndexRepository,
        ITokenRepository tokenRepository,
        IBlockchainAppService blockchainAppService,
        ICheckTransferProvider checkTransferProvider,
        IEnumerable<ICrossChainTransferProvider> crossChainTransferProviders)
    {
        _crossChainTransferRepository = crossChainTransferRepository;
        _chainAppService = chainAppService;
        _crossChainTransferIndexRepository = crossChainTransferIndexRepository;
        _tokenRepository = tokenRepository;
        _blockchainAppService = blockchainAppService;
        _checkTransferProvider = checkTransferProvider;
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
            mustQuery.Add(q => q.Term(i => i.Field(f => f.FromAddress).Value(input.FromAddress)));
        }

        if (!input.ToAddress.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ToAddress).Value(input.ToAddress)));
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

        if (transfer == null)
        {
            transfer = ObjectMapper.Map<CrossChainTransferInput, CrossChainTransfer>(input);
            transfer.Type = await GetCrossChainTypeAsync(input.FromChainId, input.ToChainId);
            transfer.Progress = 0;
            transfer.ProgressUpdateTime = input.TransferTime;
            transfer.Status = CrossChainStatus.Transferred;

            await _crossChainTransferRepository.InsertAsync(transfer);
        }
        else
        {
            transfer.TransferTokenId = input.TransferTokenId;
            transfer.TransferTransactionId = input.TransferTransactionId;
            transfer.TransferAmount = input.TransferAmount;
            transfer.TransferTime = input.TransferTime;
            transfer.TransferBlockHeight = input.TransferBlockHeight;

            await _crossChainTransferRepository.UpdateAsync(transfer);
        }
    }

    public async Task ReceiveAsync(CrossChainReceiveInput input)
    {
        var transfer = await FindCrossChainTransferAsync(input.FromChainId, input.ToChainId,
            input.TransferTransactionId, input.ReceiptId);

        var isTransferExist = true;
        if (transfer == null)
        {
            isTransferExist = false;
            transfer = ObjectMapper.Map<CrossChainReceiveInput, CrossChainTransfer>(input);
            transfer.Type = await GetCrossChainTypeAsync(input.FromChainId, input.ToChainId);
        }
        else
        {
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
            await _crossChainTransferRepository.UpdateAsync(transfer);
        }
        else
        {
            await _crossChainTransferRepository.InsertAsync(transfer);
        }
    }

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
                var provider = GetCrossChainTransferProvider(transfer.Type);
                var progress = await provider.CalculateCrossChainProgressAsync(transfer);

                if (progress == transfer.Progress)
                {
                    continue;
                }

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
        
        if (fromChainId == null || toChainId == null)
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

    public async Task UpdateReceiveTransactionAsync()
    {
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var crossChainTransfers = await GetToUpdateReceiveTransactionAsync(page);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                Logger.LogInformation("UpdateReceiveTransaction.Transfer id:{id}",transfer.Id);
                try
                {
                    var txResult =
                        await _blockchainAppService.GetTransactionResultAsync(transfer.ToChainId,
                            transfer.ReceiveTransactionId);
                    Logger.LogInformation("txResult.Transfer id:{id},is failed:{isFailed}",transfer.Id,txResult.IsFailed);

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
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Update receive transaction failed. Id: {transferId}, Error: {message}",
                        transfer.Id, ex.Message);
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

    public async Task AutoReceiveAsync()
    {
        var page = 0;
        var toUpdate = new List<CrossChainTransfer>();
        var crossChainTransfers = await GetToReceivedAsync(page);
        while (crossChainTransfers.Count != 0)
        {
            foreach (var transfer in crossChainTransfers)
            {
                try
                {
                    var toChain = await _chainAppService.GetAsync(transfer.ToChainId);
                    if (toChain == null)
                    {
                        continue;
                    }
                    if (toChain.Type != BlockchainType.AElf)
                    {
                        continue;
                    }
                    Logger.LogInformation("Start to auto receive transfer id:{id}",transfer.Id);
                    
                    // Heterogeneous:check limit.
                    if (transfer.Type == CrossChainType.Heterogeneous &&
                        !await _checkTransferProvider.CheckTransferAsync(
                            transfer.FromChainId,
                            transfer.ToChainId, transfer.TransferTokenId, transfer.TransferAmount))
                    {
                        Logger.LogInformation(
                            "Incorrect chain or check limit failed, from chain:{fromChain}, to chain:{toChain}, Id: {transferId}, transfer amount:{amount}",
                            transfer.FromChainId, transfer.ToChainId, transfer.Id, transfer.TransferAmount);
                        continue;
                    }

                    var provider = GetCrossChainTransferProvider(transfer.Type);
                    var txId = await provider.SendReceiveTransactionAsync(transfer);
                    transfer.ReceiveTransactionId = txId;
                    toUpdate.Add(transfer);
                    Logger.LogDebug(
                        "Send auto receive tx: {txId}, FromChain: {fromChainId}, ToChain: {toChainId}, Id: {transferId}",
                        txId, transfer.FromChainId, transfer.ToChainId, transfer.Id);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Send auto receive tx failed. Id: {transferId}, Error: {message}", transfer.Id,
                        ex.Message);
                }
            }

            page++;
            crossChainTransfers = await GetToReceivedAsync(page);
        }

        if (toUpdate.Count > 0)
        {
            await _crossChainTransferRepository.UpdateManyAsync(toUpdate);
        }
    }

    private async Task<List<CrossChainTransfer>> GetToReceivedAsync(int page)
    {
        var q = await _crossChainTransferRepository.GetQueryableAsync();
        var crossChainTransfers = await AsyncExecuter.ToListAsync(q
            .Where(o => o.Progress == CrossChainServerConsts.FullOfTheProgress && o.ReceiveTransactionId == null)
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