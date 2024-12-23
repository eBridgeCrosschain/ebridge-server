namespace AElf.CrossChainServer.Auth;

public static class ApolloConfigurationExtension
{
    public static IHostBuilder UseApollo(this IHostBuilder builder)
    {
        return builder
            .ConfigureAppConfiguration(config =>
            {
                var apolloOption = config.Build().GetSection("apollo");
                if (!apolloOption.GetSection("UseApollo").Get<bool>()) return;

                config.AddApollo(apolloOption);
            });
    }
}