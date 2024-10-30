using AElf.CrossChainServer.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Microsoft.Extensions.Configuration;
using AElf.CrossChainServer.EntityHandler.Core;
using AElf.CrossChainServer.Worker;
using AElf.ExceptionHandler.ABP;
using Volo.Abp.Auditing;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Aliyun;

namespace AElf.CrossChainServer.EntityHandler
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpCachingStackExchangeRedisModule),
        typeof(AbpAspNetCoreSerilogModule),
        typeof(CrossChainServerEntityFrameworkCoreModule),
        typeof(AbpEventBusRabbitMqModule),
        typeof(CrossChainServerEntityHandlerCoreModule),
        typeof(AbpBlobStoringAliyunModule),
        typeof(CrossChainServerWorkerModule),
        typeof(CrossChainServerApplicationModule),
        typeof(AOPExceptionModule)
    )]
    public class CrossChainServerEntityHandlerModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            ConfigureCache(configuration);
            ConfigureRedis(context, configuration);
            ConfigureBlob(configuration);
            ConfigureAuditLog(configuration);
            context.Services.AddHostedService<CrossChainServerHostedService>();
        }
        
        private void ConfigureBlob(IConfiguration configuration)
        {
            Configure<AbpBlobStoringOptions>(options =>
            {
                options.Containers.ConfigureDefault(container =>
                {
                    container.UseAliyun(_ =>
                    {
                    });
                });
            });
        }

        private void ConfigureCache(IConfiguration configuration)
        {
            Configure<AbpDistributedCacheOptions>(options =>
            {
                options.KeyPrefix = "CrossChainServer:";
            });
        }

        private void ConfigureRedis(
            ServiceConfigurationContext context,
            IConfiguration configuration)
        {
            var config = configuration["Redis:Configuration"];
            var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
            context.Services
                .AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "CrossChainServer-Protection-Keys");
        }
        
        private void ConfigureAuditLog(IConfiguration configuration)
        {
            Configure<AbpAuditingOptions>(options =>
            {
                options.IsEnabled = false;
            });
        }
    }
}