using AElf.CrossChainServer.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AElf.CrossChainServer.Permissions;

public class CrossChainServerPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(CrossChainServerPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(CrossChainServerPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<CrossChainServerResource>(name);
    }
}
