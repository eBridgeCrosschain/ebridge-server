using AElf.CrossChainServer.Localization;
using Microsoft.Extensions.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace AElf.CrossChainServer.Auth;

[Dependency(ReplaceServices = true)]
public class AElfCrossChainServerBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<CrossChainServerResource> _localizer;

    public AElfCrossChainServerBrandingProvider(IStringLocalizer<CrossChainServerResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AElfCrossChainServer"];
}