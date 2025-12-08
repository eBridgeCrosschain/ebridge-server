using AElf.CrossChainServer.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AElf.CrossChainServer.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class CrossChainServerController : AbpControllerBase
{
    protected CrossChainServerController()
    {
        LocalizationResource = typeof(CrossChainServerResource);
    }
}
