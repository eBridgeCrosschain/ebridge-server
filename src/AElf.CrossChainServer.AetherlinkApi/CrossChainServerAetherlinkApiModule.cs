using AElf.CrossChainServer.TokenPrice;
using Aetherlink.PriceServer;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.AetherlinkApi;

[DependsOn(
    typeof(AetherlinkPriceServerModule)
)]
public class CrossChainServerAetherlinkApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddSingleton<ITokenPriceProvider, TokenPriceProvider>();
    }
}