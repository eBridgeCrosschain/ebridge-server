using AElf.AElfNode.EventHandler.BackgroundJob.Options;
using AElf.CrossChainServer.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.BlobStoring.Aliyun;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;

namespace AElf.CrossChainServer.ContractEventHandler
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpCachingStackExchangeRedisModule),
        typeof(CrossChainServerEntityFrameworkCoreModule),
        typeof(AbpAspNetCoreSerilogModule),
        typeof(CrossChainServerContractEventHandlerCoreModule),
        typeof(AbpBlobStoringAliyunModule),
        typeof(AbpEventBusRabbitMqModule)
    )]
    public class CrossChainServerContractEventHandlerModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            ConfigureCache(configuration);
            ConfigureRedis(context, configuration);
            
            context.Services.AddHostedService<CrossChainServerHostedService>();
            
            Configure<AElfProcessorOption>(options =>
            {
                configuration.GetSection("AElfEventProcessors").Bind(options);
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
            if (string.IsNullOrEmpty(config))
            {
                return;
            }

            var redis = ConnectionMultiplexer.Connect(config);
            context.Services
                .AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "CrossChainServer-Protection-Keys");
        }
        
        // private void ConfigureEthereumEventBackgroundJobOption(IConfiguration configuration)
        // {
        //     Configure<EthereumBackgroundJobOption>(options =>
        //     {
        //         var parallelConfigurationSection = configuration.GetSection("RabbitEthereumEventParallelConfiguration");
        //         options.ConnectionName = parallelConfigurationSection.GetSection("ConnectionName").Value;
        //         options.QueueName = parallelConfigurationSection.GetSection("QueueName").Value;
        //         options.ParallelWorker = int.Parse(parallelConfigurationSection.GetSection("WorkerCount").Value);
        //     });
        // }
    }
}