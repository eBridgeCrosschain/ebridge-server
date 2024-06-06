using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.Report;
using AElf.CrossChainServer.Chains;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace AElf.CrossChainServer.Contracts.Report;

public class AElfReportContractProvider : AElfClientProvider, IReportContractProvider
{
    private readonly ILogger _logger;

    public AElfReportContractProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsSnapshot<AccountOptions> accountOptions, ILogger logger) : base(blockchainClientFactory, accountOptions)
    {
        _logger = logger;
    }

    public async Task<string> QueryOracleAsync(string chainId, string contractAddress, string privateKey,
        string targetChainId, string receiptId, string receiptHash, string amount, string targetAddress)
    {
        var client = BlockchainClientFactory.GetClient(chainId);
        var receiptIdToken = receiptId.Split(".").First();
        var res = long.TryParse(amount, out var originAmount);
        if (!res)
        {
            throw new UserFriendlyException("Failed to parser amount.");
        }
        var optionParam = $"{originAmount}-{Address.FromBase58(targetAddress)}-{receiptIdToken}";
        _logger.LogInformation("Query oracle params:{p}", optionParam);
        var param = new QueryOracleInput
        {
            Payment = 0,
            QueryInfo = new OffChainQueryInfo
            {
                Title = $"lock_token_{receiptId}",
                Options = { receiptHash, optionParam }
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