using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.Bridge;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Signature.Provider;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.CrossChainServer.Contracts.Bridge;

public class AElfBridgeContractProvider: AElfClientProvider, IBridgeContractProvider
{
    private readonly ISignatureProvider _signatureProvider;
    public AElfBridgeContractProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsSnapshot<AccountOptions> accountOptions, ISignatureProvider signatureProvider) : base(blockchainClientFactory, accountOptions)
    {
        _signatureProvider = signatureProvider;
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
            await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(GetPrivateKeyForCall(chainId)), contractAddress,
                "GetSwapIdByToken", param);
        var txWithSign = client.SignTransaction(GetPrivateKeyForCall(chainId), transaction);
        var transactionResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
        var swapId = Hash.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionResult));
        return swapId.ToHex();
    }

    public async Task<string> SwapTokenAsync(string chainId, string contractAddress, string pubKey, string swapId, string receiptId, string originAmount,
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
        var fromAddress = client.GetAddressFromPubKey(pubKey);
        var transaction = await client.GenerateTransactionAsync(fromAddress, contractAddress, "SwapToken", param);
        
        var txWithSign = await _signatureProvider.SignTxMsg(pubKey, transaction.GetHash().ToHex());
        transaction.Signature = ByteStringHelper.FromHexString(txWithSign);
        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });

        return result.TransactionId;
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
}