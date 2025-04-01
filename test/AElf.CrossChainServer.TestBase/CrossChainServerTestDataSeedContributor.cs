using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace AElf.CrossChainServer;

public class CrossChainServerTestDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    protected ICurrentUser _currentUser;
    private readonly IChainRepository _chainRepository;
    private readonly ICrossChainUserRepository _crossChainUserRepository;

    public CrossChainServerTestDataSeedContributor(IChainRepository chainRepository,
        ICrossChainUserRepository crossChainUserRepository)
    {
        _chainRepository = chainRepository;
        _crossChainUserRepository = crossChainUserRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        /* Seed additional test data... */
        await _chainRepository.InsertAsync(new Chain
        {
            Id = "Ethereum",
            Type = BlockchainType.Evm,
            Name = "Ethereum",
            IsMainChain = true,
            AElfChainId = 0
        });

        await _chainRepository.InsertAsync(new Chain()
        {
            Id = "MainChain_AELF",
            Type = BlockchainType.AElf,
            Name = "Main Chain AELF",
            IsMainChain = true,
            AElfChainId = 9992731
        });

        await _chainRepository.InsertAsync(new Chain()
        {
            Id = "SideChain_tDVV",
            Type = BlockchainType.AElf,
            Name = "Side Chain tDVV",
            IsMainChain = false,
            AElfChainId = 1866392
        });

        await _chainRepository.InsertAsync(new Chain()
        {
            Id = "SideChain_tDVW",
            Type = BlockchainType.AElf,
            Name = "Side Chain tDVW",
            IsMainChain = false,
            AElfChainId = 1931928
        });

        await _chainRepository.InsertAsync(new Chain()
        {
            Id = "Ton",
            Type = BlockchainType.Tvm,
            Name = "Ton",
            IsMainChain = true,
            AElfChainId = 0
        });
        
        await _chainRepository.InsertAsync(new Chain()
        {
            Id = "Solana",
            Type = BlockchainType.Svm,
            Name = "Solana",
            IsMainChain = true,
            AElfChainId = 0
        });

        await _crossChainUserRepository.InsertAsync(new()
        {
            Id = new Guid("d3d94468-2d38-4b1f-9dcd-fbfc7ddcab1b"),
            AppId = "test_app_id",
            CaHash = "test_ca_hash",
            AddressInfos = new List<AddressInfoDto>
            {
                new()
                {
                    ChainId = "AELF",
                    Address = "test_user_address",
                    Id = Guid.NewGuid()
                },
                new()
                {
                    ChainId = "tDVW",
                    Address = "side_test_user_address",
                    Id = Guid.NewGuid()
                }
            },
            CreateTime = 0,
            ModificationTime = 0
        });
    }
}