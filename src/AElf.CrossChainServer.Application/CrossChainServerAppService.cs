using AElf.CrossChainServer.Localization;
using Volo.Abp.Application.Services;

namespace AElf.CrossChainServer;

/* Inherit your application services from this class.
 */
public abstract class CrossChainServerAppService : ApplicationService
{
    protected CrossChainServerAppService()
    {
        LocalizationResource = typeof(CrossChainServerResource);
    }
}
