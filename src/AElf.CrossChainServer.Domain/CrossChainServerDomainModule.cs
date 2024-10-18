using AElf.CrossChainServer.CrossChain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AElf.CrossChainServer.MultiTenancy;
using AElf.ExceptionHandler.ABP;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Options;
using Volo.Abp.AuditLogging;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.Emailing;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.IdentityServer;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.PermissionManagement.IdentityServer;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AElf.CrossChainServer;

[DependsOn(
    typeof(CrossChainServerDomainSharedModule),
    typeof(AbpAuditLoggingDomainModule),
    typeof(AbpBackgroundJobsDomainModule),
    typeof(AbpFeatureManagementDomainModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpPermissionManagementDomainIdentityModule),
    typeof(AbpIdentityServerDomainModule),
    typeof(AbpPermissionManagementDomainIdentityServerModule),
    typeof(AbpSettingManagementDomainModule),
    typeof(AbpTenantManagementDomainModule),
    typeof(AbpEmailingModule),
    typeof(AElfIndexingElasticsearchModule),
    typeof(AOPExceptionModule)
)]
public class CrossChainServerDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CrossChainServerDomainModule>(); });
        
        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = MultiTenancyConsts.IsEnabled;
        });

#if DEBUG
        context.Services.Replace(ServiceDescriptor.Singleton<IEmailSender, NullEmailSender>());
#endif
        
        Configure<IndexCreateOption>(x =>
        {
            x.AddModule(typeof(CrossChainServerDomainModule));
        });

        Configure<AbpDistributedEntityEventOptions>(options =>
        {
            options.AutoEventSelectors.Add<CrossChainTransfer>();
            options.EtoMappings.Add<CrossChainTransfer, CrossChainTransferEto>();
            
            options.AutoEventSelectors.Add<CrossChainIndexingInfo>();
            options.EtoMappings.Add<CrossChainIndexingInfo, CrossChainIndexingInfoEto>();
            
            options.AutoEventSelectors.Add<OracleQueryInfo>();
            options.EtoMappings.Add<OracleQueryInfo, OracleQueryInfoEto>();
            
            options.AutoEventSelectors.Add<ReportInfo>();
            options.EtoMappings.Add<ReportInfo, ReportInfoEto>();
        });
    }
}
