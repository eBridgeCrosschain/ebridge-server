using System.Collections.Immutable;
using System.Text;
using AElf.CrossChainServer.Auth.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.OpenIddict.ExtensionGrantTypes;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace AElf.CrossChainServer.Auth;

public class LoginTokenExtensionGrant : ITokenExtensionGrant
{
    public string Name => AuthConstant.GrantType;

    public async Task<IActionResult> HandleAsync(ExtensionGrantContext context)
    {
        // var scopeManager = context.HttpContext.RequestServices.GetRequiredService<IOpenIddictScopeManager>();
        var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<IdentityUser>>();
        var userManager = context.HttpContext.RequestServices.GetRequiredService<IdentityUserManager>();
        var _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<LoginTokenExtensionGrant>>();
        var source = context.Request.GetParameter("source").ToString();
        var name = context.Request.GetParameter("user_name")?.ToString();
        var password = context.Request.GetParameter("password").ToString();
        _logger.LogInformation("Reconciliation name:{name}, {source}", name, source);
        if (source != AuthConstant.ReconciliationSource)
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "invalid source");
        }

        if (name.IsNullOrEmpty() || password.IsNullOrEmpty())
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "invalid name or password");
        }

        var user = await userManager.FindByNameAsync(name);
        if (user == null)
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "invalid user");
        }

        password = Encoding.UTF8.GetString(ByteArrayHelper.HexStringToByteArray(password));
        var result = await userManager.CheckPasswordAsync(user, password);
        if (!result)
        {
            return GetForbidResult(OpenIddictConstants.Errors.InvalidRequest, "invalid name or password");
        }

        var principal = await signInManager.CreateUserPrincipalAsync(user);
        principal.SetScopes(context.Request.GetScopes());
        // principal.SetResources(await GetResourcesAsync(context.Request.GetScopes(), scopeManager));
        principal.SetAudiences("AElfCrossChainServer");

        await context.HttpContext.RequestServices.GetRequiredService<AbpOpenIddictClaimsPrincipalManager>()
            .HandleAsync(context.Request, principal);

        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
    }
    
    private ForbidResult GetForbidResult(string errorType, string errorDescription)
    {
        return new ForbidResult(
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
            properties: new AuthenticationProperties(new Dictionary<string, string>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = errorType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = errorDescription
            }!));
    }

    protected virtual async Task<IEnumerable<string>> GetResourcesAsync(ImmutableArray<string> scopes,
        IOpenIddictScopeManager scopeManager)
    {
        var resources = new List<string>();
        if (!scopes.Any())
        {
            return resources;
        }

        await foreach (var resource in scopeManager.ListResourcesAsync(scopes))
        {
            resources.Add(resource);
        }

        return resources;
    }
}