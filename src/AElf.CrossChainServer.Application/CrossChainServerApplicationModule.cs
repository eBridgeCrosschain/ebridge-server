using AElf.Client.Service;
using AElf.AElfNode.EventHandler.Core;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts.Bridge;
using AElf.CrossChainServer.Contracts.CrossChain;
using AElf.CrossChainServer.Contracts.Report;
using AElf.CrossChainServer.Contracts.Token;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AElf.CrossChainServer;

[DependsOn(
    typeof(CrossChainServerDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(CrossChainServerApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AElfNodeEventHandlerCoreModule)
    )]
public class CrossChainServerApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<CrossChainServerApplicationModule>();
        });
        
        var configuration = context.Services.GetConfiguration();
        Configure<ChainApiOptions>(configuration.GetSection("ChainApi"));
        Configure<BridgeContractOptions>(configuration.GetSection("BridgeContract"));
        Configure<ReportContractOptions>(configuration.GetSection("ReportContract"));
        Configure<BlockConfirmationOptions>(configuration.GetSection("BlockConfirmation"));
        Configure<AccountOptions>(configuration.GetSection("Account"));
        Configure<ReportJobCategoryOptions>(configuration.GetSection("ReportJobCategory"));
        Configure<TokenContractOptions>(configuration.GetSection("TokenContract"));
        Configure<CrossChainContractOptions>(configuration.GetSection("CrossChainContract"));
        Configure<TokenSymbolMappingOptions>(configuration.GetSection("TokenSymbolMapping"));
        
        context.Services.AddSingleton<IBlockchainClientFactory<AElfClient>, AElfClientFactory>();
        context.Services.AddSingleton<IBlockchainClientFactory<Nethereum.Web3.Web3>, EvmClientFactory>();
        context.Services.AddSingleton<IBlockchainClientFactory<TronClient.TronClient>, TronClientFactory>();
        context.Services.AddTransient<IBlockchainClientProvider, AElfClientProvider>();
        context.Services.AddTransient<IBlockchainClientProvider, EvmClientProvider>();
        context.Services.AddTransient<IBlockchainClientProvider, TronClientProvider>();
        
        context.Services.AddTransient<IBridgeContractProvider, EvmBridgeContractProvider>();
        context.Services.AddTransient<IBridgeContractProvider, AElfBridgeContractProvider>();
        context.Services.AddTransient<IBridgeContractProvider, TronBridgeContractProvider>();
        context.Services.AddTransient<IReportContractProvider, AElfReportContractProvider>();
        context.Services.AddTransient<ICrossChainContractProvider, AElfCrossChainContractProvider>();
        context.Services.AddTransient<ITokenContractProvider, AElfTokenContractProvider>();
        context.Services.AddTransient<ICheckTransferProvider, CheckTransferProvider>();

    }
}
