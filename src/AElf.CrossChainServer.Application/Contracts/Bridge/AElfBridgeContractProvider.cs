using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.Bridge;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.ExceptionHandler;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler;
using AElf.Types;
using EBridge.Contracts.TokenPool;
using Google.Protobuf;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.Domain.Entities;

namespace AElf.CrossChainServer.Contracts.Bridge;

public class AElfBridgeContractProvider: AElfClientProvider, IBridgeContractProvider
{
    private readonly ITokenAppService _tokenAppService;

    public AElfBridgeContractProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsSnapshot<AccountOptions> accountOptions, ITokenAppService tokenAppService) : base(blockchainClientFactory, accountOptions)
    {
        _tokenAppService = tokenAppService;
    }

    public Task<List<ReceiptInfoDto>> GetSendReceiptInfosAsync(string chainId, string contractAddress, string targetChainId, Guid tokenId,
        long fromIndex, long endIndex)
    {
        throw new NotImplementedException();
    }

    public Task<List<ReceivedReceiptInfoDto>> GetReceivedReceiptInfosAsync(string chainId, string contractAddress, string fromChainId, Guid tokenId,
        long fromIndex, long endIndex)
    {
        throw new NotImplementedException();
    }

    public Task<List<ReceiptIndexDto>> GetTransferReceiptIndexAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> targetChainIds)
    {
        throw new NotImplementedException();
    }

    public Task<List<ReceiptIndexDto>> GetReceiveReceiptIndexAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> fromChainIds)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CheckTransmitAsync(string chainId, string contractAddress, string receiptHash)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetSwapIdByTokenAsync(string chainId, string contractAddress, string fromChainId,
        string symbol)
    {
        var client = BlockchainClientFactory.GetClient(chainId);

        var param = new GetSwapIdByTokenInput
        {
            ChainId = fromChainId,
            Symbol = symbol
        };

        var transaction =
            await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(GetPrivateKey(chainId)), contractAddress,
                "GetSwapIdByToken", param);
        var txWithSign = client.SignTransaction(GetPrivateKey(chainId), transaction);
        var transactionResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
        var swapId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionResult));
        return swapId.ToHex();
    }
    
    [ExceptionHandler(typeof(Exception), typeof(InvalidOperationException),typeof(WebException), Message = "[AElf contract provider] Swap token failed.",ReturnDefault = ReturnDefault.Default,
         LogTargets = new[]{"chainId","contractAddress","swapId","receiptId","originAmount","receiverAddress"})]
    public virtual async Task<string> SwapTokenAsync(string chainId, string contractAddress, string privateKey, string swapId, string receiptId, string originAmount,
        string receiverAddress)
    {
        var client = BlockchainClientFactory.GetClient(chainId);

        var param = new SwapTokenInput
        {
            SwapId = Hash.LoadFromHex(swapId),
            ReceiptId = receiptId,
            OriginAmount = originAmount,
            ReceiverAddress = Address.FromBase58(receiverAddress)
        };
        var fromAddress = client.GetAddressFromPrivateKey(privateKey);
        var transaction = await client.GenerateTransactionAsync(fromAddress, contractAddress, "SwapToken", param);
        var txWithSign = client.SignTransaction(privateKey, transaction);

        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        return result.TransactionId;
    }

    public Task<DailyLimitDto> GetDailyLimitAsync(string chainId, string contractAddress, Guid tokenId, string targetChainId)
    {
        throw new NotImplementedException();
    }

    public Task<List<TokenBucketDto>> GetCurrentReceiptTokenBucketStatesAsync(string chainId, string contractAddress, List<Guid> tokenIds,
        List<string> targetChainIds)
    {
        throw new NotImplementedException();
    }

    public Task<List<TokenBucketDto>> GetCurrentSwapTokenBucketStatesAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> fromChainIds)
    {
        throw new NotImplementedException();
    }
    
    public async Task<List<PoolLiquidityDto>> GetPoolLiquidityAsync(string chainId, string contractAddress, List<Guid> tokenIds)
    {
        var client = BlockchainClientFactory.GetClient(chainId);
        var result = new List<PoolLiquidityDto>();
        foreach (var tokenId in tokenIds)
        {
            var tokenInfo = await _tokenAppService.GetAsync(tokenId);
            var param = new GetLiquidityInput
            {
                TokenSymbol = tokenInfo.Symbol
            };
            var transaction =
                await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(GetPrivateKey(chainId)), contractAddress,
                    "GetTokenPoolInfo", param);
            var txWithSign = client.SignTransaction(GetPrivateKey(chainId), transaction);
            var transactionResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });
            var tokenPoolInfo = TokenPoolInfo.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionResult));
            var liquidity = tokenPoolInfo.Liquidity / (decimal)Math.Pow(10, tokenInfo.Decimals);
            result.Add(new PoolLiquidityDto
            {
                ChainId = chainId,
                TokenId = tokenId,
                Liquidity = liquidity
            });
        }
        return result;
    }
    
}