using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Tokens;
using Nethereum.Util;
using TronClient;
using TronNet.Crypto;

namespace AElf.CrossChainServer.Contracts.Bridge;

public class TronBridgeContractProvider : TronClientProvider,IBridgeContractProvider
{
    private class TokenDetails
    {
        public List<string> AddressList { get; set; }
        public List<int> DecimalList { get; set; }
    }
    
    private readonly ITokenAppService _tokenAppService;

    public TronBridgeContractProvider(IBlockchainClientFactory<TronClient.TronClient> blockchainClientFactory,
        ITokenAppService tokenAppService) : base(
        blockchainClientFactory)
    {
        _tokenAppService = tokenAppService;
    }

    public async Task<List<ReceiptInfoDto>> GetSendReceiptInfosAsync(string chainId, string contractAddress, string targetChainId, Guid tokenId,
        long fromIndex, long endIndex)
    {
        var token = await _tokenAppService.GetAsync(tokenId);
        var tronClient = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = tronClient.GetContract(contractAddress);
        
        var tronGetReceiptInfos = await contractHandler.CallAsync<GetReceiptInfosFunctionMessage, TronDto.GetReceiptInfosDto>(new TronConstantContractFunctionMessage<GetReceiptInfosFunctionMessage>
        {
            FunctionMessage = new GetReceiptInfosFunctionMessage
            {
                Token = TronAddressToHex(token.Address),
                TargetChainId = targetChainId,
                FromIndex = fromIndex,
                EndIndex = endIndex
            },
            Visible = true
        });

        var result = new List<ReceiptInfoDto>();
        foreach (var receipt in tronGetReceiptInfos.Receipts)
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
        var tronClient = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = tronClient.GetContract(contractAddress);
        
        var tronGetReceiptInfos = await contractHandler.CallAsync<GetReceivedReceiptInfosFunctionMessage, TronDto.GetReceivedReceiptInfosDto>(new TronConstantContractFunctionMessage<GetReceivedReceiptInfosFunctionMessage>
        {
            FunctionMessage = new GetReceivedReceiptInfosFunctionMessage
            {
                Token = TronAddressToHex(token.Address),
                FromChainId = fromChainId,
                FromIndex = fromIndex,
                EndIndex = endIndex
            },
            Visible = true
        });

        var result = new List<ReceivedReceiptInfoDto>();
        foreach (var receipt in tronGetReceiptInfos.Receipts)
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
        var tokenDetails = await GetTokenDetails(tokenIds);

        var tronClient = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = tronClient.GetContract(contractAddress);

        var indexes = await contractHandler.CallAsync<GetSendReceiptIndexFunctionMessage, TronDto.GetSendReceiptIndexDto>(new TronConstantContractFunctionMessage<GetSendReceiptIndexFunctionMessage>
        {
            FunctionMessage = new GetSendReceiptIndexFunctionMessage
            {
                Tokens = tokenDetails.AddressList,
                TargetChainIds = targetChainIds
            },
            Visible = true
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
        var tokenDetails = await GetTokenDetails(tokenIds);

        var tronClient = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = tronClient.GetContract(contractAddress);
        
        var indexes = await contractHandler.CallAsync<GetReceiveReceiptIndexFunctionMessage, TronDto.GetReceiveReceiptIndexDto>(new TronConstantContractFunctionMessage<GetReceiveReceiptIndexFunctionMessage>
        {
            FunctionMessage = new GetReceiveReceiptIndexFunctionMessage
            {
                Tokens = tokenDetails.AddressList,
                FromChainIds = fromChainIds
            },
            Visible = true
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
        var tronClient = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = tronClient.GetContract(contractAddress);
        var isTransmit = await contractHandler.CallAsync<IsReceiptRecordedFunctionMessage, TronDto.IsReceiptRecordedDto>(new TronConstantContractFunctionMessage<IsReceiptRecordedFunctionMessage>
        {
            FunctionMessage = new IsReceiptRecordedFunctionMessage
            {
                ReceiptHash = ByteArrayHelper.HexStringToByteArray(receiptHash)
            },
            Visible = true
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

    public async Task<List<TokenBucketDto>> GetCurrentReceiptTokenBucketStatesAsync(string chainId, string contractAddress, List<Guid> tokenIds,
        List<string> targetChainIds)
    {
        var tokenDetails = await GetTokenDetails(tokenIds);
        
        var tronClient = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = tronClient.GetContract(contractAddress);
        var receiptTokenBucket = await contractHandler.CallAsync<GetCurrentReceiptTokenBucketStatesFunctionMessage, TronDto.ReceiptTokenBucketsDto>(new TronConstantContractFunctionMessage<GetCurrentReceiptTokenBucketStatesFunctionMessage>
        {
            FunctionMessage = new GetCurrentReceiptTokenBucketStatesFunctionMessage
            {
                Token = tokenDetails.AddressList,
                TargetChainId = targetChainIds
            },
            Visible = true
        });
        var tokenBuckets = receiptTokenBucket.TokenBuckets.Select((t, i) =>
            GetTokenBuckets(t.TokenCapacity, t.Rate, tokenDetails.DecimalList[i])).ToList();
        return tokenBuckets;
    }

    public async Task<List<TokenBucketDto>> GetCurrentSwapTokenBucketStatesAsync(string chainId, string contractAddress, List<Guid> tokenIds, List<string> fromChainIds)
    {
        var tokenDetails = await GetTokenDetails(tokenIds);
        
        var tronClient = BlockchainClientFactory.GetClient(chainId);
        var contractHandler = tronClient.GetContract(contractAddress);
        var swapTokenBucket = await contractHandler.CallAsync<GetCurrentSwapTokenBucketStatesFunctionMessage, TronDto.SwapTokenBucketsDto>(new TronConstantContractFunctionMessage<GetCurrentSwapTokenBucketStatesFunctionMessage>
        {
            FunctionMessage = new GetCurrentSwapTokenBucketStatesFunctionMessage
            {
                Token = tokenDetails.AddressList,
                FromChainId = fromChainIds
            },
            Visible = true
        });
        var tokenBuckets = swapTokenBucket.SwapTokenBuckets.Select((t, i) =>
            GetTokenBuckets(t.TokenCapacity, t.Rate, tokenDetails.DecimalList[i])).ToList();
        return tokenBuckets;
    }
    
    private static TokenBucketDto GetTokenBuckets(BigInteger capacity, BigInteger rate, int tokenDecimal)
    {
        if (capacity == 0 || rate == 0)
        {
            return new TokenBucketDto();
        }
        var tokenCapacity = (decimal)(new BigDecimal(capacity) / BigInteger.Pow(10, tokenDecimal));
        var refillRate = (decimal)(new BigDecimal(rate) / BigInteger.Pow(10, tokenDecimal));
        var maximumTimeConsumed = (int)Math.Ceiling(tokenCapacity / refillRate / CrossChainServerConsts.DefaultRateLimitSeconds);
        return new TokenBucketDto
        {
            Capacity = tokenCapacity,
            RefillRate = refillRate,
            MaximumTimeConsumed = maximumTimeConsumed
        };
    }
    
    private static string TronAddressToHex(string value)
    {
        var addressByte = Base58Encoder.DecodeFromBase58Check(value);
        addressByte = addressByte.Slice(1, addressByte.Length);
        return addressByte.ToHex();
    }
    
    private async Task<TokenDetails> GetTokenDetails(List<Guid> tokenIds)
    {
        var tokenAddressList = new List<string>();
        var tokenDecimalList = new List<int>();
        foreach (var tokenId in tokenIds)
        {
            var token = await _tokenAppService.GetAsync(tokenId);
            tokenAddressList.Add(TronAddressToHex(token.Address));
            tokenDecimalList.Add(token.Decimals);
        }
        
        return new TokenDetails
        {
            AddressList = tokenAddressList,
            DecimalList = tokenDecimalList
        };
    }
}