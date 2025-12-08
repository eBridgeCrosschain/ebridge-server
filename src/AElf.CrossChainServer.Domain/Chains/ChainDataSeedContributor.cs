using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace AElf.CrossChainServer.Chains;

public class ChainDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IChainRepository _chainRepository;
    private readonly INESTRepository<ChainIndex, string> _chainIndexRepository;

    public ChainDataSeedContributor(
        IConfiguration configuration, IChainRepository chainRepository,
        INESTRepository<ChainIndex, string> chainIndexRepository)
    {
        _configuration = configuration;
        _chainRepository = chainRepository;
        _chainIndexRepository = chainIndexRepository;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        var configurationSection = _configuration.GetSection("Chain");
        foreach (var section in configurationSection.GetChildren())
        {
            var id = section.GetValue<string>("Id");
            var name = section.GetValue<string>("Name");
            var type = section.GetValue<BlockchainType>("Type");
            var isMainChain = section.GetValue<bool>("IsMainChain");
            var aelfChainId = section.GetValue<int>("AElfChainId");
            var blockChain = section.GetValue<string>("BlockChain");

            var chain = await _chainRepository.FindAsync(id);
            if (chain == null)
            {
                chain = await _chainRepository.InsertAsync(new Chain
                {
                    Id = id,
                    Name = name,
                    Type = type,
                    IsMainChain = isMainChain,
                    AElfChainId = aelfChainId,
                    BlockChain = blockChain
                });
            }
            else
            {
                chain.Name = name;
                chain.Type = type;
                chain.IsMainChain = isMainChain;
                chain.AElfChainId = aelfChainId;
                chain.BlockChain = blockChain;
                await _chainRepository.UpdateAsync(chain);
            }

            await _chainIndexRepository.AddOrUpdateAsync(new ChainIndex
            {
                Id = chain.Id,
                Name = chain.Name,
                Type = chain.Type,
                IsMainChain = chain.IsMainChain,
                AElfChainId = aelfChainId,
                BlockChain = blockChain
            });
        }
    }
}
