using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.Report;
using AElf.CrossChainServer.Chains;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.CrossChainServer.Contracts.Report;

public class AElfReportContractProvider : AElfClientProvider, IReportContractProvider
{
    public AElfReportContractProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsSnapshot<AccountOptions> accountOptions) : base(blockchainClientFactory, accountOptions)
    {
    }

    public async Task<string> QueryOracleAsync(string chainId, string contractAddress, string privateKey,
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
        var fromAddress = client.GetAddressFromPrivateKey(privateKey);
        var transaction = await client.GenerateTransactionAsync(fromAddress, contractAddress, "QueryOracle", param);
        var txWithSign = client.SignTransaction(privateKey, transaction);

        var result = await client.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        return result.TransactionId;
    }
}