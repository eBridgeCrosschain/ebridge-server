﻿using System.Collections.Generic;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts.Bridge;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.EntityHandler.Core;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.CrossChainServer;

[DependsOn(
    typeof(CrossChainServerApplicationModule),
    typeof(CrossChainServerDomainTestModule),
    typeof(CrossChainServerEntityHandlerCoreModule)
)]
public class CrossChainServerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.RemoveAll<IBlockchainClientProvider>();

        context.Services.AddTransient<IBlockchainClientProvider, MockAElfClientProvider>();
        context.Services.AddTransient<IBlockchainClientProvider, MockEvmClientProvider>();
        context.Services.AddTransient<ICheckTransferProvider, MockCheckTransferProvider>();
        
        Configure<ChainApiOptions>(o =>
        {
            o.ChainNodeApis = new Dictionary<string, string>
            {
                { "Ethereum", "https://kovan.infura.io/v3/" },
                { "MainChain_AELF", "https://aelf.io" }
            };
        });

        Configure<BridgeContractOptions>(o =>
        {
            o.ContractAddresses = new Dictionary<string, BridgeContractAddress>
            {
                {
                    "Ethereum", new BridgeContractAddress
                    {
                        BridgeInContract = "0x164322657FC57EA95CAc4bF6623E53CA0f952E11",
                        BridgeOutContract = "0x26a44A0383F15f7f83D84eb95c4b7762d8de995A"
                    }
                }
            };
        });

        Configure<ReportJobCategoryOptions>(o =>
        {
            o.Mapping = new Dictionary<string, string>
            {
                { "MainChain_AELF", "CrossChain" }
            };
        });
        
        Configure<TokenSymbolMappingOptions>(o =>
        {
            o.Mapping = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            o.Mapping["Ethereum"] = new Dictionary<string, Dictionary<string, string>>();
            o.Mapping["Ethereum"]["MainChain_AELF"] = new Dictionary<string, string>
            {
                { "WETH", "ETH" }
            };
        });

        Configure<GraphQLClientOptions>(o =>
        {
            o.Mapping = new Dictionary<string, string>
            {
                { "CrossChainServerClient", "http://192.168.67.84:8083/AElfIndexer_DApp/CrossChainServerIndexerCASchema/graphql" },
                { "CrossChainClient", "http://192.168.67.84:8083/AElfIndexer_DApp/CrossChainIndexerCASchema/graphql" }
            };
        });
    }
}
