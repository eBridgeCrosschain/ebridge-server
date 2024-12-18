using AElf.CrossChainServer.Auth.Options;
using AElf.CrossChainServer.EntityFrameworkCore;
using AElf.CrossChainServer.Localization;
using Localization.Resources.AbpUi;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Auditing;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.Uow;

namespace AElf.CrossChainServer.Auth;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpDistributedLockingModule),
    typeof(AbpAccountApplicationModule),
    // typeof(CrossChainServerDomainModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(CrossChainServerEntityFrameworkCoreModule)
)]
public class AElfCrossChainServerAuthServerModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddServer(options =>
            {
                options.UseAspNetCore().DisableTransportSecurityRequirement();
                options.SetIssuer(new Uri(configuration["AuthServer:IssuerUri"]));
                int.TryParse(configuration["ExpirationHour"], out int expirationHour);
                if (expirationHour > 0)
                {
                    options.SetAccessTokenLifetime(DateTime.Now.AddHours(expirationHour) - DateTime.Now);
                }
            });

            builder.AddValidation(options =>
            {
                options.AddAudiences("AElfCrossChainServer");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        PreConfigure<OpenIddictServerBuilder>(builder =>
        {
            builder.Configure(openIddictServerOptions =>
            {
                openIddictServerOptions.GrantTypes.Add("signature");
                openIddictServerOptions.GrantTypes.Add(AuthConstant.GrantType);
            });
        });


        PreConfigure<OpenIddictValidationBuilder>(builder =>
        {
            //Set options here...		
        });
    }

    // public override void ConfigureServices(ServiceConfigurationContext context)
    // {
    //     Configure<AbpOpenIddictExtensionGrantsOptions>(options =>
    //     {
    //         options.Grants.Add("signature", new SignatureGrantHandler());
    //         options.Grants.Add(AuthConstant.GrantType, new LoginTokenExtensionGrant());
    //     });
    //     Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    // }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        Configure<AbpUnitOfWorkDefaultOptions>(options =>
        {
            options.TransactionBehavior = UnitOfWorkTransactionBehavior.Disabled;
        });

        context.Services.Configure<GraphQlOption>(configuration.GetSection("GraphQL"));
        context.Services.Configure<ChainOptions>(configuration.GetSection("Chains"));
        context.Services.Configure<RecaptchaOptions>(configuration.GetSection("Recaptcha"));
        Configure<ContractOptions>(configuration.GetSection("Contract"));
        context.Services.Configure<TimeRangeOption>(option =>
        {
            option.TimeRange = Convert.ToInt32(configuration["TimeRange"]);
        });

        Configure<AbpOpenIddictExtensionGrantsOptions>(options =>
        {
            options.Grants.Add("signature", new SignatureGrantHandler());
            options.Grants.Add(AuthConstant.GrantType, new LoginTokenExtensionGrant());
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources.Get<CrossChainServerResource>().AddBaseTypes(typeof(AbpUiResource));
            options.Languages.Add(new LanguageInfo("en", "en", "English"));
        });

        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(LeptonXLiteThemeBundles.Styles.Global,
                bundle => { bundle.AddFiles("/global-styles.css"); }
            );
        });

        Configure<AbpAuditingOptions>(options => { options.IsEnabled = false; });

        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"].Split(','));

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
        Configure<AbpBackgroundJobOptions>(options => { options.IsJobExecutionEnabled = false; });
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "CrossChainServer:Auth:"; });

        var dataProtectionBuilder = context.Services.AddDataProtection().SetApplicationName("CrossChainServer");
        if (!hostingEnvironment.IsDevelopment())
        {
            var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
            dataProtectionBuilder.PersistKeysToStackExchangeRedis(redis, "CrossChainServer-Protection-Keys");
        }

        context.Services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var connection = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
            return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
        });

        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        context.Services.AddHttpClient();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();
        app.UseUnitOfWork();
        app.UseAuthorization();
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints();

        DisplayConfigurationProviders(context);
    }

    public static void DisplayConfigurationProviders(ApplicationInitializationContext context)
    {
        try
        {
            var configuration = context.GetConfiguration();
            var configurationRoot = (IConfigurationRoot)configuration;
            foreach (var provider in configurationRoot.Providers.ToList())
            {
                Log.Warning("ConfigurationProvider: {0}", provider.ToString());
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "display configuration providers error.");
        }
    }
}