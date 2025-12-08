using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.MultiToken;
using AElf.CrossChainServer.Chains;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Serilog;

namespace AElf.CrossChainServer.Contracts.Token;

public class AElfTokenContractProvider : AElfClientProvider, ITokenContractProvider
{
    public AElfTokenContractProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsSnapshot<AccountOptions> accountOptions) : base(blockchainClientFactory, accountOptions)
    {
    }

    public async Task<string> CrossChainReceiveTokenAsync(string chainId, string contractAddress, string privateKey, int fromChainId, long parentChainHeight,
        string transferTransaction, MerklePath merklePath)
    {
        var client = BlockchainClientFactory.GetClient(chainId);

        var param = new CrossChainReceiveTokenInput
        {
            MerklePath = merklePath,
            FromChainId = fromChainId,
            ParentChainHeight = parentChainHeight,
            TransferTransactionBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(transferTransaction)),
        };
        var fromAddress = client.GetAddressFromPrivateKey(privateKey);
        var transaction = await client.GenerateTransactionAsync(fromAddress, contractAddress, "CrossChainReceiveToken", param);
        var txWithSign = client.SignTransaction(privateKey, transaction);
        
        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
        Log.Debug("CrossChainReceiveToken txId {txId},RawTransaction: {raw}", result.TransactionId, txWithSign.ToByteArray().ToHex());
        return result.TransactionId;
    }
}