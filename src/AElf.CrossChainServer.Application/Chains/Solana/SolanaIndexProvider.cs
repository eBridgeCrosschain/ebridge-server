using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Solnet.Rpc.Models;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChainServer.Chains.Ton;

public interface ISolanaIndexProvider
{
    Task<List<string>> GetSignaturesForAddressAsync(string chainId, string accountPubKey, ulong limit = 1000, 
        string before = null, string until = null);
    Task<TransactionMetaSlotInfo> GetTransactionAsync(string chainId, string signature);
    Task<BlockInfo> GetBlockAsync(string chainId, ulong slot);
}

public class SolanaIndexProvider : SolanaClientProvider, ISolanaIndexProvider, ITransientDependency
{
    public SolanaIndexProvider(ISolanaIndexClientProvider indexClientProvider) : base(indexClientProvider)
    {
    }

    public async Task<List<string>> GetSignaturesForAddressAsync(string chainId, string accountPubKey, 
        ulong limit = 1000, string before = null, string until = null)
    {
        var signatures = await _indexClientProvider.GetClient(chainId).GetSignaturesForAddressAsync(
            accountPubKey,
            limit: limit,
            before: before,
            until: until);
        return signatures.Result?.ConvertAll(t => t.Signature).ToList();
    }
    
    public async Task<TransactionMetaSlotInfo> GetTransactionAsync(string chainId, string signature)
    {
        var tx = await _indexClientProvider.GetClient(chainId).GetTransactionAsync(
            signature);
        if (tx.Result.Meta.Error != null) return null; 
        return tx.Result;
    }
    
    public async Task<BlockInfo> GetBlockAsync(string chainId, ulong slot)
    {
        var block = await _indexClientProvider.GetClient(chainId).GetBlockAsync(slot);
        if (block.ErrorData?.Error != null) return null; 
        return block.Result;
    }
}