using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Tokens;
using Nethereum.Util;
using Nethereum.Web3;

namespace AElf.CrossChainServer.Contracts.Bridge;

public class EvmBridgeContractProvider : EvmClientProvider,IBridgeContractProvider
{
    private readonly ITokenAppService _tokenAppService;

    public EvmBridgeContractProvider(IBlockchainClientFactory<Web3> blockchainClientFactory,
        ITokenAppService tokenAppService) : base(
        blockchainClientFactory)
    {
        _tokenAppService = tokenAppService;
    }

    public async Task<List<ReceiptInfoDto>> GetSendReceiptInfosAsync(string chainId, string contractAddress, string targetChainId, Guid tokenId,
        long fromIndex, long endIndex)
    {
        var token = await _tokenAppService.GetAsync(tokenId);
        var web3 = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = web3.Eth.GetContractHandler(contractAddress);

        var evmGetReceiptInfos = await contractHandler
            .QueryDeserializingToObjectAsync<GetReceiptInfosFunctionMessage, GetReceiptInfosDto>(
                new GetReceiptInfosFunctionMessage
                {
                    Token = token.Address,
                    TargetChainId = targetChainId,
                    FromIndex = fromIndex,
                    EndIndex = endIndex
                });

        var result = new List<ReceiptInfoDto>();
        foreach (var receipt in evmGetReceiptInfos.Receipts)
        {
            var receiptInfo = new ReceiptInfoDto();
            receiptInfo.ReceiptId = receipt.ReceiptId;
            receiptInfo.TokenId = tokenId;
            receiptInfo.FromAddress = receipt.Owner;
            receiptInfo.ToChainId = receipt.TargetChainId;
            receiptInfo.ToAddress = receipt.TargetAddress;
            receiptInfo.Amount = (decimal)((BigDecimal)receipt.Amount / BigInteger.Pow(10, token.Decimals));
            receiptInfo.BlockHeight = (long)receipt.BlockHeight;
            receiptInfo.BlockTime = DateTimeHelper.FromUnixTimeMilliseconds((long)receipt.BlockTime * 1000);

            result.Add(receiptInfo);
        }

        return result;
    }

    public async Task<List<ReceivedReceiptInfoDto>> GetReceivedReceiptInfosAsync(string chainId, string contractAddress, string fromChainId, Guid tokenId,
        long fromIndex, long endIndex)
    {
        var token = await _tokenAppService.GetAsync(tokenId);
        var web3 = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = web3.Eth.GetContractHandler(contractAddress);
        
        var evmGetReceiptInfos = await contractHandler
            .QueryDeserializingToObjectAsync<GetReceivedReceiptInfosFunctionMessage, GetReceivedReceiptInfosDto>(
                new GetReceivedReceiptInfosFunctionMessage
                {
                    Token = token.Address,
                    FromChainId = fromChainId,
                    FromIndex = fromIndex,
                    EndIndex = endIndex
                });

        var result = new List<ReceivedReceiptInfoDto>();
        foreach (var receipt in evmGetReceiptInfos.Receipts)
        {
            var receiptInfo = new ReceivedReceiptInfoDto();
            receiptInfo.ReceiptId = receipt.ReceiptId;
            receiptInfo.TokenId = tokenId;
            //receiptInfo.FromAddress = receipt.Owner;
            receiptInfo.FromChainId = receipt.FromChainId;
            receiptInfo.ToAddress = receipt.TargetAddress;
            receiptInfo.Amount = (decimal)((BigDecimal)receipt.Amount / BigInteger.Pow(10, token.Decimals));
            receiptInfo.BlockHeight = (long)receipt.BlockHeight;
            receiptInfo.BlockTime = DateTimeHelper.FromUnixTimeMilliseconds((long)receipt.BlockTime * 1000);

            result.Add(receiptInfo);
        }

        return result;
    }

    public async Task<List<ReceiptIndexDto>> GetTransferReceiptIndexAsync(string chainId, string contractAddress,
        List<Guid> tokenIds, List<string> targetChainIds)
    {
        var tokenAddress = new List<string>();
        foreach (var tokenId in tokenIds)
        {
            var token = await _tokenAppService.GetAsync(tokenId);
            tokenAddress.Add(token.Address);
        }

        var web3 = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = web3.Eth.GetContractHandler(contractAddress);

        var indexes = await contractHandler
            .QueryDeserializingToObjectAsync<GetSendReceiptIndexFunctionMessage, GetSendReceiptIndexDto>(
                new GetSendReceiptIndexFunctionMessage
                {
                    Tokens = tokenAddress,
                    TargetChainIds = targetChainIds
                });

        return indexes.Indexes.Select((t, i) => new ReceiptIndexDto
        {
            TargetChainId = targetChainIds[i],
            TokenId = tokenIds[i], 
            Index = (long)t
        }).ToList();
    }

    public async Task<List<ReceiptIndexDto>> GetReceiveReceiptIndexAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> fromChainIds)
    {
        var tokenAddress = new List<string>();
        foreach (var tokenId in tokenIds)
        {
            var token = await _tokenAppService.GetAsync(tokenId);
            tokenAddress.Add(token.Address);
        }

        var web3 = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = web3.Eth.GetContractHandler(contractAddress);

        var indexes = await contractHandler
            .QueryDeserializingToObjectAsync<GetReceiveReceiptIndexFunctionMessage, GetReceiveReceiptIndexDto>(
                new GetReceiveReceiptIndexFunctionMessage
                {
                    Tokens = tokenAddress,
                    FromChainIds = fromChainIds
                });

        return indexes.Indexes.Select((t, i) => new ReceiptIndexDto
        {
            TargetChainId = fromChainIds[i],
            TokenId = tokenIds[i], 
            Index = (long)t
        }).ToList();
    }

    public async Task<bool> CheckTransmitAsync(string chainId, string contractAddress, string receiptHash)
    {
        var web3 = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = web3.Eth.GetContractHandler(contractAddress);
        var isTransmit = await contractHandler
            .QueryDeserializingToObjectAsync<IsReceiptRecordedFunctionMessage, IsReceiptRecordedDto>(
                new IsReceiptRecordedFunctionMessage
                {
                    ReceiptHash = ByteArrayHelper.HexStringToByteArray(receiptHash)
                });

        return isTransmit.IsReceiptRecorded;
    }

    public Task<string> GetSwapIdByTokenAsync(string chainId, string contractAddress, string fromChainId, string symbol)
    {
        throw new NotImplementedException();
    }

    public Task<string> SwapTokenAsync(string chainId, string contractAddress, string privateKey, string swapId, string receiptId, string originAmount,
        string receiverAddress)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsTransferCanReceiveAsync(string chainId, string contractAddress, string symbol, string amount)
    {
        throw new NotImplementedException();
    }
}