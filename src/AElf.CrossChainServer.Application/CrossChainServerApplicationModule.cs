using AElf.Client.Service;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Contracts.Bridge;
using AElf.CrossChainServer.Contracts.CrossChain;
using AElf.CrossChainServer.Contracts.Report;
using AElf.CrossChainServer.Contracts.Token;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.HttpClient;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.Tokens;
using AElf.ExceptionHandler.ABP;
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
    typeof(AOPExceptionModule)
)]
public class CrossChainServerApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CrossChainServerApplicationModule>(); });

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
        Configure<CrossChainOptions>(configuration.GetSection("CrossChain"));
        Configure<GraphQLClientOptions>(configuration.GetSection("GraphQLClients"));
        Configure<EvmTokensOptions>(configuration.GetSection("EvmTokens"));
        Configure<CrossChainLimitsOptions>(configuration.GetSection("CrossChainLimits"));
        Configure<ReportQueryTimesOptions>(configuration.GetSection("ReportQueryTimes"));
        Configure<AutoReceiveConfigOptions>(configuration.GetSection("AutoReceiveConfig"));
        Configure<SyncStateServiceOption>(configuration.GetSection("SyncStateService"));
        Configure<TokenPriceIdMappingOptions>(configuration.GetSection("TokenPriceIdMapping"));
        Configure<TokenAccessOptions>(configuration.GetSection("TokenAccess"));

        context.Services.AddSingleton<IBlockchainClientFactory<AElfClient>, AElfClientFactory>();
        context.Services.AddSingleton<IBlockchainClientFactory<Nethereum.Web3.Web3>, EvmClientFactory>();
        context.Services.AddSingleton<IGraphQLClientFactory, GraphQLClientFactory>();
        context.Services.AddTransient<IBlockchainClientProvider, AElfClientProvider>();
        context.Services.AddTransient<IBlockchainClientProvider, EvmClientProvider>();

        context.Services.AddTransient<IBridgeContractProvider, EvmBridgeContractProvider>();
        context.Services.AddTransient<IBridgeContractProvider, AElfBridgeContractProvider>();
        context.Services.AddTransient<IReportContractProvider, AElfReportContractProvider>();
        context.Services.AddTransient<ICrossChainContractProvider, AElfCrossChainContractProvider>();
        context.Services.AddTransient<ITokenContractProvider, AElfTokenContractProvider>();
        context.Services.AddTransient<ICheckTransferProvider, CheckTransferProvider>();
        context.Services.AddTransient<ITokenInvokeProvider, TokenInvokeProvider>();
        context.Services.AddTransient<IHttpProvider, HttpProvider>();
    }
}