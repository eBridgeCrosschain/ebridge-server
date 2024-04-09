using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.CrossChainServer.Chains;
using AElf.Standards.ACS7;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace AElf.CrossChainServer.Contracts.CrossChain;

public class AElfCrossChainContractProvider : AElfClientProvider, ICrossChainContractProvider
{
    public AElfCrossChainContractProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsSnapshot<AccountOptions> accountOptions) : base(blockchainClientFactory, accountOptions)
    {
    }

    public async Task<CrossChainMerkleProofContext> GetBoundParentChainHeightAndMerklePathByHeightAsync(string chainId,
        string contractAddress, long height)
    {
        var client = BlockchainClientFactory.GetClient(chainId);

        var param = new Int64Value
        {
            Value = height
        };
        var transaction =
            await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(GetPrivateKeyForCall(chainId)), contractAddress,
                "GetBoundParentChainHeightAndMerklePathByHeight", param);
        var txWithSign = client.SignTransaction(GetPrivateKeyForCall(chainId), transaction);
        var transactionResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
        var result =
            CrossChainMerkleProofContext.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionResult));
        return result;
    }
}