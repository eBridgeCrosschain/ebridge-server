using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
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

public class AElfBridgeContractProvider : AElfClientProvider, IBridgeContractProvider
{
    private readonly ITokenAppService _tokenAppService;

    public AElfBridgeContractProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsSnapshot<AccountOptions> accountOptions, ITokenAppService tokenAppService) : base(
        blockchainClientFactory, accountOptions)
    {
        _tokenAppService = tokenAppService;
    }

    public Task<DailyLimitDto> GetReceiptDailyLimitAsync(string chainId, string contractAddress, Guid tokenId,
        string targetChainId)
    {
        throw new NotImplementedException();
    }

    public Task<DailyLimitDto> GetSwapDailyLimitAsync(string chainId, string contractAddress, string swapId)
    {
        throw new NotImplementedException();
    }

    public Task<List<TokenBucketDto>> GetCurrentReceiptTokenBucketStatesAsync(string chainId, string contractAddress,
        List<Guid> tokenIds,
        List<string> targetChainIds)
    {
        throw new NotImplementedException();
    }

    public Task<List<TokenBucketDto>> GetCurrentSwapTokenBucketStatesAsync(string chainId, string contractAddress,
        List<Guid> tokenIds, List<string> fromChainIds)
    {
        throw new NotImplementedException();
    }

    public async Task<List<PoolLiquidityDto>> GetPoolLiquidityAsync(string chainId, string contractAddress,
        List<Guid> tokenIds)
    {
        var client = BlockchainClientFactory.GetClient(chainId);
        var result = new List<PoolLiquidityDto>();
        foreach (var tokenId in tokenIds)
        {
            var tokenInfo = await _tokenAppService.GetAsync(tokenId);
            var param = new GetTokenPoolInfoInput
            {
                TokenSymbol = tokenInfo.Symbol
            };
            var transaction =
                await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(GetPrivateKey(chainId)),
                    contractAddress,
                    "GetTokenPoolInfo", param);
            var txWithSign = client.SignTransaction(GetPrivateKey(chainId), transaction);
            var transactionResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });
            var tokenPoolInfo = TokenPoolInfo.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionResult));
            var liquidity = tokenPoolInfo.Liquidity / (decimal)Math.Pow(10, tokenInfo.Decimals);
            Log.Debug(
                "Get pool liquidity from aelf contract, chainId: {chainId}, tokenId: {tokenId}, liquidity: {liquidity}",
                chainId, tokenId, liquidity);
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