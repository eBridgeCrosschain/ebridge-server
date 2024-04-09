using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.BridgeContract;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace AElf.CrossChainServer.Contracts.Bridge;

[RemoteService(IsEnabled = false)]
public class BridgeContractAppService : CrossChainServerAppService, IBridgeContractAppService
{
    private readonly IBridgeContractProviderFactory _bridgeContractProviderFactory;
    private readonly BridgeContractOptions _bridgeContractOptions;
    private readonly IBridgeContractSyncInfoRepository _bridgeContractSyncInfoRepository;
    private readonly AccountOptions _accountOptions;

    public BridgeContractAppService(IBridgeContractProviderFactory bridgeContractProviderFactory,
        IOptionsSnapshot<BridgeContractOptions> options,
        IBridgeContractSyncInfoRepository bridgeContractSyncInfoRepository,
        IOptionsSnapshot<AccountOptions> accountOptions)
    {
        _bridgeContractProviderFactory = bridgeContractProviderFactory;
        _bridgeContractSyncInfoRepository = bridgeContractSyncInfoRepository;
        _bridgeContractOptions = options.Value;
        _accountOptions = accountOptions.Value;
    }

    public async Task<List<ReceiptInfoDto>> GetTransferReceiptInfosAsync(string chainId, string targetChainId,
        Guid tokenId,
        long fromIndex, long endIndex)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new List<ReceiptInfoDto>();
        }
        return await provider.GetSendReceiptInfosAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].BridgeInContract, targetChainId, tokenId, fromIndex,
            endIndex);
    }

    public async Task<List<ReceivedReceiptInfoDto>> GetReceivedReceiptInfosAsync(string chainId, string targetChainId,
        Guid tokenId,
        long fromIndex, long endIndex)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new List<ReceivedReceiptInfoDto>();
        }
        return await provider.GetReceivedReceiptInfosAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].BridgeOutContract, targetChainId, tokenId, fromIndex,
            endIndex);
    }

    public async Task<BridgeContractSyncInfoDto> GetSyncInfoAsync(string chainId, TransferType type,
        string targetChainId, Guid tokenId)
    {
        var info = await _bridgeContractSyncInfoRepository.FindAsync(o =>
            o.ChainId == chainId && o.Type == type && o.TargetChainId == targetChainId && o.TokenId == tokenId);

        return ObjectMapper.Map<BridgeContractSyncInfo, BridgeContractSyncInfoDto>(info);
    }

    public async Task UpdateSyncInfoAsync(string chainId, TransferType type, string targetChainId, Guid tokenId,
        long syncIndex)
    {
        var info = await _bridgeContractSyncInfoRepository.FindAsync(o =>
            o.ChainId == chainId && o.Type == type && o.TargetChainId == targetChainId && o.TokenId == tokenId);

        if (info == null)
        {
            info = new BridgeContractSyncInfo
            {
                ChainId = chainId,
                TargetChainId = targetChainId,
                Type = type,
                TokenId = tokenId,
                SyncIndex = syncIndex,
            };
            await _bridgeContractSyncInfoRepository.InsertAsync(info);
        }
        else
        {
            info.SyncIndex = syncIndex;
            await _bridgeContractSyncInfoRepository.UpdateAsync(info);
        }
    }

    public async Task<List<ReceiptIndexDto>> GetTransferReceiptIndexAsync(string chainId, List<Guid> tokenIds,
        List<string> targetChainIds)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new List<ReceiptIndexDto>();
        }
        return await provider.GetTransferReceiptIndexAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].BridgeInContract, tokenIds, targetChainIds);
    }

    public async Task<List<ReceiptIndexDto>> GetReceiveReceiptIndexAsync(string chainId, List<Guid> tokenIds,
        List<string> targetChainIds)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new List<ReceiptIndexDto>();
        }
        return await provider.GetReceiveReceiptIndexAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].BridgeOutContract, tokenIds, targetChainIds);
    }

    public async Task<bool> CheckTransmitAsync(string chainId, string receiptHash)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return false;
        }
        return await provider.CheckTransmitAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].BridgeOutContract, receiptHash);
    }

    public async Task<string> GetSwapIdByTokenAsync(string chainId, string fromChainId, string symbol)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return "";
        }
        return await provider.GetSwapIdByTokenAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].BridgeOutContract, fromChainId, symbol);
    }

    public async Task<string> SwapTokenAsync(string chainId, string swapId, string receiptId, string originAmount,
        string receiverAddress)
    {
        var pubKey = _accountOptions.PublicKeys[chainId];
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return "";
        }
        return await provider.SwapTokenAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].BridgeOutContract, pubKey, swapId, receiptId,
            originAmount, receiverAddress);
    }

    public async Task<List<TokenBucketDto>> GetCurrentReceiptTokenBucketStatesAsync(string chainId, List<Guid> tokenIds,
        List<string> targetChainIds)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new List<TokenBucketDto>();
        }
        return await provider.GetCurrentReceiptTokenBucketStatesAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].LimiterContract, tokenIds, targetChainIds);
    }

    public async Task<List<TokenBucketDto>> GetCurrentSwapTokenBucketStatesAsync(string chainId, List<Guid> tokenIds,
        List<string> fromChainIds)
    {
        var provider = await _bridgeContractProviderFactory.GetBridgeContractProviderAsync(chainId);
        if (provider == null)
        {
            return new List<TokenBucketDto>();
        }
        return await provider.GetCurrentSwapTokenBucketStatesAsync(chainId,
            _bridgeContractOptions.ContractAddresses[chainId].LimiterContract, tokenIds, fromChainIds);
    }
}