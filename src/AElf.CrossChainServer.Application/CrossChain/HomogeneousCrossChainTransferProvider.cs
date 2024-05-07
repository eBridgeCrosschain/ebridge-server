using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Core;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.CrossChain;

public class HomogeneousCrossChainTransferProvider : ICrossChainTransferProvider, ITransientDependency
{
    private readonly ICrossChainIndexingInfoAppService _crossChainIndexingInfoAppService;
    private readonly ITokenContractAppService _tokenContractAppService;
    private readonly IBlockchainAppService _blockchainAppService;
    private readonly IChainAppService _chainAppService;
    private readonly ICrossChainContractAppService _crossChainContractAppService;
    private readonly ICrossChainTransferRepository _crossChainTransferRepository;
    private readonly ILogger<HomogeneousCrossChainTransferProvider> Logger;
 

    public HomogeneousCrossChainTransferProvider(ICrossChainIndexingInfoAppService crossChainIndexingInfoAppService,
        ITokenContractAppService tokenContractAppService, IBlockchainAppService blockchainAppService,
        IChainAppService chainAppService, ICrossChainContractAppService crossChainContractAppService,
        ICrossChainTransferRepository crossChainTransferRepository)
    {
        _crossChainIndexingInfoAppService = crossChainIndexingInfoAppService;
        _tokenContractAppService = tokenContractAppService;
        _blockchainAppService = blockchainAppService;
        _chainAppService = chainAppService;
        _crossChainContractAppService = crossChainContractAppService;
        _crossChainTransferRepository = crossChainTransferRepository;
        Logger = NullLogger<HomogeneousCrossChainTransferProvider>.Instance;
    }

    public CrossChainType CrossChainType { get; } = CrossChainType.Homogeneous;

    public async Task<CrossChainTransfer> FindTransferAsync(string fromChainId, string toChainId, string transferTransactionId, string receiptId)
    {
        Logger.LogInformation("Find transfer Homogeneous:{fromchainid}-{tochainid}-{receiptId}",fromChainId,toChainId,receiptId);
        return await _crossChainTransferRepository.FindAsync(o =>
            o.FromChainId == fromChainId && o.ToChainId == toChainId &&
            o.TransferTransactionId == transferTransactionId);
    }

    public async Task<int> CalculateCrossChainProgressAsync(CrossChainTransfer transfer)
    {
        return await _crossChainIndexingInfoAppService.CalculateCrossChainProgressAsync(
            transfer.FromChainId, transfer.ToChainId, transfer.TransferBlockHeight,
            transfer.TransferTime);
    }

    public async Task<string> SendReceiveTransactionAsync(CrossChainTransfer transfer)
    {
        var txResult =
                await _blockchainAppService.GetTransactionResultAsync(transfer.FromChainId,
                    transfer.TransferTransactionId);
            var parentHeight = txResult.BlockHeight;
            
            var paramsJson = JsonNode.Parse(txResult.Transaction.Params);
            var param = new AElf.Contracts.MultiToken.CrossChainTransferInput
            {
                To = Address.FromBase58(paramsJson["to"].ToString()),
                Amount = long.Parse(paramsJson["amount"].ToString()),
                Symbol = paramsJson["symbol"].ToString(),
                IssueChainId = int.Parse(paramsJson["issueChainId"].ToString()),
                ToChainId = int.Parse(paramsJson["toChainId"].ToString())
            };
            if (paramsJson["memo"] != null)
            {
                param.Memo = paramsJson["memo"].ToString(); 
            }

            var transaction = new Transaction
            {
                From = Address.FromBase58(txResult.Transaction.From),
                To = Address.FromBase58(txResult.Transaction.To),
                Params = param.ToByteString(),
                Signature = ByteString.FromBase64(txResult.Transaction.Signature),
                MethodName = txResult.Transaction.MethodName,
                RefBlockNumber = txResult.Transaction.RefBlockNumber,
                RefBlockPrefix = ByteString.FromBase64(txResult.Transaction.RefBlockPrefix)
            };

            var merklePath = await GetMerklePathAsync(transfer.FromChainId, transfer.TransferTransactionId);
            if (transfer.FromChainId != CrossChainServerConsts.AElfMainChainId)
            {
                var merkleProofContext = await _crossChainContractAppService.GetBoundParentChainHeightAndMerklePathByHeightAsync(
                    transfer.FromChainId, txResult.BlockHeight);
                parentHeight = merkleProofContext.BoundParentChainHeight;
                merklePath.MerklePathNodes.AddRange(merkleProofContext.MerklePathFromParentChain.MerklePathNodes);
            }

            
            var fromChain = await _chainAppService.GetAsync(transfer.FromChainId);
            return fromChain == null
                ? ""
                : await _tokenContractAppService.CrossChainReceiveTokenAsync(transfer.ToChainId,
                    fromChain.AElfChainId, parentHeight, transaction.ToByteArray().ToHex(), merklePath);
    }
    
    private async Task<MerklePath> GetMerklePathAsync(string chainId, string txId)
    {
        var merklePathDto = await _blockchainAppService.GetMerklePathAsync(chainId,txId);
        var merklePath = new MerklePath();
        foreach (var node in merklePathDto.MerklePathNodes)
        {
            merklePath.MerklePathNodes.Add(new MerklePathNode()
            {
                Hash = new Hash() { Value = AElf.Types.Hash.LoadFromHex(node.Hash).Value },
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        return merklePath;
    }
}