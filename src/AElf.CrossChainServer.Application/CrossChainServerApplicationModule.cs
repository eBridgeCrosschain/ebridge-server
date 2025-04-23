using AElf.AetherlinkApi;
using AElf.Client.Service;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.Chains.Ton;
using AElf.CrossChainServer.Contracts.Bridge;
using AElf.CrossChainServer.Contracts.CrossChain;
using AElf.CrossChainServer.Contracts.Report;
using AElf.CrossChainServer.Contracts.Token;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.HttpClient;
using AElf.CrossChainServer.Indexer;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.TokenPrice;
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
    typeof(CrossChainServerAetherlinkApiModule),
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
        Configure<PoolLiquiditySyncOptions>(configuration.GetSection("PoolLiquiditySync"));
        Configure<AetherLinkOption>(configuration.GetSection("AetherLink"));
        Configure<ApiKeyOptions>(configuration.GetSection("ApiKey"));
        Configure<TokenWhitelistOptions>(configuration.GetSection("TokenWhitelist"));
        Configure<LarkNotifyTemplateOptions>(configuration.GetSection("LarkNotifyTemplate"));
        Configure<ChainIdMapOptions>(configuration.GetSection("ChainIdMap"));
        Configure<TonConfigOption>(configuration.GetSection("TonConfig"));
        Configure<LimitSyncOptions>(configuration.GetSection("LimitSync"));
        
        context.Services.AddSingleton<IBlockchainClientFactory<AElfClient>, AElfClientFactory>();
        context.Services.AddSingleton<IBlockchainClientFactory<Nethereum.Web3.Web3>, EvmClientFactory>();
        context.Services.AddSingleton<IGraphQLClientFactory, GraphQLClientFactory>();
        context.Services.AddTransient<IBlockchainClientProvider, AElfClientProvider>();
        context.Services.AddTransient<IBlockchainClientProvider, EvmClientProvider>();
        context.Services.AddTransient<IBlockchainClientProvider, TonClientProvider>();
        
        context.Services.AddTransient<IBridgeContractProvider, EvmBridgeContractProvider>();
        context.Services.AddTransient<IBridgeContractProvider, AElfBridgeContractProvider>();
        context.Services.AddTransient<IReportContractProvider, AElfReportContractProvider>();
        context.Services.AddTransient<ICrossChainContractProvider, AElfCrossChainContractProvider>();
        context.Services.AddTransient<ITokenContractProvider, AElfTokenContractProvider>();
        context.Services.AddTransient<ICheckTransferProvider, CheckTransferProvider>();
        context.Services.AddTransient<ITokenInvokeProvider, TokenInvokeProvider>();
        context.Services.AddTransient<IHttpProvider, HttpProvider>();
        context.Services.AddTransient<IAetherLinkProvider, AetherLinkProvider>();
        context.Services.AddTransient<ILarkRobotNotifyProvider,LarkRobotNotifyProvider>();
        context.Services.AddTransient<ITokenPriceProvider, TokenPriceProvider>();
        context.Services.AddTransient<IScanProvider, ScanProvider>();
        context.Services.AddTransient<IAwakenProvider, AwakenProvider>();
        context.Services.AddTransient<IAggregatePriceProvider, AggregatePriceProvider>();
        context.Services.AddTransient<ITokenImageProvider, TokenImageProvider>();
        context.Services.AddTransient<ITokenLiquidityMonitorProvider, TokenLiquidityMonitorProvider>();
        context.Services.AddTransient<ITokenInfoCacheProvider, TokenInfoCacheProvider>();
    }
}
