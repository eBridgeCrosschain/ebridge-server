using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.Report;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Signature.Provider;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.CrossChainServer.Contracts.Report;

public class AElfReportContractProvider : AElfClientProvider, IReportContractProvider
{
    private readonly ISignatureProvider _signatureProvider;

    public AElfReportContractProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsSnapshot<AccountOptions> accountOptions, ISignatureProvider signatureProvider) : base(blockchainClientFactory, accountOptions)
    {
        _signatureProvider = signatureProvider;
    }

    public async Task<string> QueryOracleAsync(string chainId, string contractAddress, string pubKey,
        string targetChainId, string receiptId, string receiptHash)
    {
        var client = BlockchainClientFactory.GetClient(chainId);

        var param = new QueryOracleInput
        {
            Payment = 0,
            QueryInfo = new OffChainQueryInfo
            {
                Title = $"lock_token_{receiptId}",
                Options = { receiptHash }
            },
            ChainId = targetChainId
        };
        var fromAddress = client.GetAddressFromPubKey(pubKey);
        var transaction = await client.GenerateTransactionAsync(fromAddress, contractAddress, "QueryOracle", param);
        var txWithSign = await _signatureProvider.SignTxMsg(pubKey, transaction.GetHash().ToHex());
        transaction.Signature = ByteStringHelper.FromHexString(txWithSign);

        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        });

        return result.TransactionId;
    }
}