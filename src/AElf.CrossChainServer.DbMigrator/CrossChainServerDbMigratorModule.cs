using AElf.CrossChainServer.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AElf.CrossChainServer.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(CrossChainServerEntityFrameworkCoreModule),
    typeof(CrossChainServerApplicationContractsModule)
)]
public class CrossChainServerDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
    }
}