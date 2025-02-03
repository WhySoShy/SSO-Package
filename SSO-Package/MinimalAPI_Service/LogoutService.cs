using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace SSO_Package.MinimalAPI_Service;
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Service that our Logout controller uses for its logic
/// </summary>
public class LogoutService : ILogoutService
{
    #region Dependency injection

    protected readonly SignInManager<IdentityUser> _signInManager;

    public LogoutService(SignInManager<IdentityUser> signInManager)
        => _signInManager = signInManager;

    #endregion

    /// <summary>
    /// Main method that our controller calls.
    /// </summary>
    /// <param name="returnUrl">Url that it will return to after the user has been logged out</param>
    public virtual async Task LogoutAsync(HttpContext context, string returnUrl)
    {
        // Clear the user cookies.
        await _signInManager.SignOutAsync();

        Dictionary<string, string?> query = new() { { "wa", "wsignout1.0" }, { "wreply", $"{context.Request.Scheme}://{context.Request.Host}{returnUrl}" } };

        context.Response.Redirect(QueryHelpers.AddQueryString("https://`Your ADFS Hostname`/adfs/ls/", query), true);
    }
}

/// <summary>
/// Holds the main method that the controller should call.
/// </summary>
public interface ILogoutService
{
    /// <summary>
    /// Main method that our controller calls.
    /// </summary>
    Task LogoutAsync(HttpContext context, string returnUrl);
}