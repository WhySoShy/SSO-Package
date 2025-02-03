using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace SSO_Package.MinimalAPI_Service;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// TODO: Make the service generic, so it can use any form for class if we have a custom identity class for example.

/// <summary>
/// Service that our Logout controller uses for its logic
/// </summary>
public class CallbackService : ICallBackService
{
    #region Dependency Injection

    protected readonly SignInManager<IdentityUser> _signInManager;
    protected readonly UserManager<IdentityUser> _userManager;
    protected readonly RoleManager<IdentityRole> _roleManager;
    protected readonly IConfiguration _config;

    public CallbackService(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config
        )
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _config = config;
    }

    #endregion

    /// <summary>
    /// Main method that our controller calls.
    /// </summary>
    public virtual async Task CallbackAsync(HttpContext context, string returnUrl)
    {
        // Set the url it should redirect to
        context.Response.Redirect(returnUrl);

        // Clear the user cookies.
        await _signInManager.SignOutAsync();

        // Handle the creation of the user, and find the login information.
        var userInformation = await HandleUsermanagementAsync();

        // No need to handle roles and sign the user in, if they are not logged into adfs web.
        if (userInformation.user is null || userInformation.externalLoginInfo is null)
            return;

        // Handle the roles of the user.
        await HandleRoleManagementAsync(userInformation.user, userInformation.externalLoginInfo.Principal.Claims.Where(x => x.Type.Contains("Group")));

        // Sign the user in
        await _signInManager.SignInAsync(userInformation.user, isPersistent: false, userInformation.externalLoginInfo.LoginProvider);
    }

    /// <summary>
    /// Adds roles from 'appsettings' inside group 'Roles'
    /// </summary>
    protected virtual async Task HandleRoleManagementAsync(IdentityUser user, IEnumerable<Claim> userRoles)
    {
        // Loop through the roles inside the section, create and add them to the user.
        foreach (var item in _config.GetSection("Roles").GetChildren())
        {
            // If the user does not have the role, continue.
            if (!userRoles.Any(x => x.Value == item.Value) || item.Value is null)
                continue;

            // Create the role if it does not exist.
            if (!await _roleManager.RoleExistsAsync(item.Value))
                await _roleManager.CreateAsync(new(item.Value));

            // Add the role if the user does not have it.
            if (!await _userManager.IsInRoleAsync(user, item.Value))
                await _userManager.AddToRoleAsync(user, item.Value);
        }
    }

    /// <summary>
    /// Handles the user creation and login.
    /// </summary>
    protected virtual async Task<(IdentityUser? user, ExternalLoginInfo? externalLoginInfo)> HandleUsermanagementAsync()
    {
        // Get login information from cookie.
        ExternalLoginInfo? info = await _signInManager.GetExternalLoginInfoAsync();

        // Return null if the user is not signed in on adfs web.
        if (info is null)
            return (null, null);

        // Create a new temporary identityUser.
        IdentityUser userModel = new() { Email = info.Principal.FindFirstValue(ClaimTypes.Email), UserName = info.ProviderKey };
        
        // Check if the user has been registered using a third party service (wsfed)
        if (!(await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true)).Succeeded)
        {
            await _userManager.CreateAsync(userModel);
            await _userManager.AddLoginAsync(userModel, info);
        }

        return (await _userManager.FindByEmailAsync(info.Principal.FindFirstValue(ClaimTypes.Email)!), info);
    }
}

/// <summary>
/// Holds the main method that the controller should call.
/// </summary>
public interface ICallBackService
{    
    /// <summary>
     /// Main method that our controller calls.
     /// </summary>
    Task CallbackAsync(HttpContext context, string returnUrl);
}