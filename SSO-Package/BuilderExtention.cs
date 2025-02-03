using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SSO_Package.Configuration;
using SSO_Package.MinimalAPI_Service;

namespace SSO_Package;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

public static class BuilderExtention
{
    #region AddSSO

    /// <summary>
    /// Uses the built in context and gets the connectionstring from [ConnectionString:DefaultConnection] inside 'appsettings.jar' 
    /// </summary>
    /// <param name="adfsSettings">Delegate to setup wsfd</param>
    /// <param name="authOptions">Delegate to setup policies etc</param>
    public static WebApplicationBuilder AddSSO(this WebApplicationBuilder provider, Action<WsFederationOptions>? adfsSettings = null, Action<AuthorizationOptions>? authOptions = null)
        => provider.AddSSO<BuiltInContext>(typeof(BuilderExtention).Assembly.FullName!, adfsSettings: adfsSettings, authOptions: authOptions);

    /// <summary>
    /// Uses <typeparamref name="TContext"/> and gets the connectionstring from [ConnectionString:DefaultConnection] inside 'appsettings.jar' 
    /// </summary>
    /// <param name="adfsSettings">Delegate to setup wsfd</param>
    /// <param name="authOptions">Delegate to setup policies etc</param>
    /// <param name="migrationAssembly">Project name / Assembly name</param>
    /// <exception cref="Exception">If no connectionstring was found.</exception>
    public static WebApplicationBuilder AddSSO<TContext>(this WebApplicationBuilder provider, string migrationAssembly, Action<WsFederationOptions>? adfsSettings = null, Action<AuthorizationOptions>? authOptions = null)
            where TContext : IdentityDbContext
        => provider.AddSSO<TContext>(migrationAssembly: migrationAssembly, connectionString: provider.Configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("Could not find [ConnectionString:DefaultConnection] in 'appsettings.jar'"), adfsSettings: adfsSettings, authOptions: authOptions);

  

    /// <summary>
    /// <para>
    ///     Uses a built in <see cref="BuiltInContext"/>.
    /// </para>
    /// overrided from <see cref="AddSSO{TContext, TIdentityUser, TIdentityRole}(WebApplicationBuilder, string, Action{WsFederationOptions}, string?, Action{AuthorizationOptions}?)"/>
    /// <param name="adfsSettings">Delegate to setup wsfd</param>
    /// <param name="authOptions">Delegate to setup policies etc</param>
    /// <param name="connectionString">Connectionstring for the database</param>
    /// <param name="migrationAssembly">Project name / Assembly name</param>
    /// </summary>
    public static WebApplicationBuilder AddSSO(this WebApplicationBuilder provider, string migrationAssembly, string connectionString, Action<WsFederationOptions>? adfsSettings = null, Action<AuthorizationOptions>? authOptions = null)
        => provider.AddSSO<BuiltInContext>(migrationAssembly: migrationAssembly, connectionString: connectionString, adfsSettings: adfsSettings, authOptions: authOptions);

    /// <summary>
    /// Overrided method of <see cref="AddSSO{TContext, TIdentityUser, TIdentityRole}(WebApplicationBuilder, string, Action{WsFederationOptions}, string?, Action{AuthorizationOptions}?)"/>
    /// </summary>
    /// <param name="adfsSettings">Delegate to setup wsfd</param>
    /// <param name="authOptions">Delegate to setup policies etc</param>
    /// <param name="connectionString">Connectionstring for the database</param>
    /// <param name="migrationAssembly">Project name / Assembly name</param>
    public static WebApplicationBuilder AddSSO<TContext>(this WebApplicationBuilder provider, string migrationAssembly, string connectionString, Action<WsFederationOptions>? adfsSettings = null, Action<AuthorizationOptions>? authOptions = null)
            where TContext : IdentityDbContext
    {
        // Ensure that adfsSettings is set.
        adfsSettings ??= x =>
        {
            x.UseSecurityTokenHandlers = true;
            x.MetadataAddress = "https://`Your ADFS Hostname`/FederationMetadata/2007-06/FederationMetadata.xml";
            x.Wtrealm = "https://localhost:7003";
        };

        return provider.AddSSO<TContext, IdentityUser, IdentityRole>(migrationAssembly: migrationAssembly, connectionString: connectionString, adfsSettings: adfsSettings, authOptions: authOptions);
    }

    /// <summary>
    /// <para>
    ///     Adds the identity part, EntityFrameworkStores and Authentication with WsFederation.
    /// </para>
    /// </summary>
    /// <param name="adfsSettings">Set the settings of WsFederation. You have to set <see cref="WsFederationOptions.Wtrealm"/> and <seealso cref="WsFederationOptions.MetadataAddress"/>.</param>
    /// <param name="authOptions">Delegate to setup policies etc</param>
    /// <param name="connectionString">Connectionstring for the database</param>
    /// <param name="migrationAssembly">Project name / Assembly name</param>
    /// <exception cref="Exception">If connectionString is not passed as parameter.</exception>
    public static WebApplicationBuilder AddSSO<TContext, TIdentityUser, TIdentityRole>(this WebApplicationBuilder provider, string migrationAssembly ,Action<WsFederationOptions> adfsSettings, string? connectionString = null, Action<AuthorizationOptions>? authOptions = null)
        where TContext : IdentityDbContext
        where TIdentityRole : class
        where TIdentityUser : class
    {
        provider.Services.AddIdentity<TIdentityUser, TIdentityRole>()
            .AddEntityFrameworkStores<TContext>();

        provider.Services.AddAuthentication()
            .AddWsFederation(adfsSettings);

        // Ensure that the authOptions has been set.
        authOptions ??= x => { };
        provider.Services.AddAuthorization(authOptions);


        // Ensure that the connectionString has been set.
        connectionString ??= provider.Configuration.GetConnectionString("DefaultConnection");
        provider.Services.AddDbContext<TContext>(options =>
            options.UseSqlServer(
                !string.IsNullOrEmpty(connectionString) ? connectionString : throw new Exception("Connectionstring was not found"),
                y => y.MigrationsAssembly(migrationAssembly)));

        return provider;
    }

    #endregion

    #region Configure controllers
    /// <summary>
    /// Configures what services the controllers should use. <br />
    /// <see cref="ICallBackService"/>: <see cref="CallbackService"/> <br />
    /// <see cref="ILogoutService"/>: <see cref="LogoutService"/>
    /// </summary>
    public static IServiceCollection ConfigureMinimalAPIServices(this IServiceCollection services)
    {
        services.AddScoped<ICallBackService, CallbackService>();
        services.AddScoped<ILogoutService, LogoutService>();
        return services;
    }

    /// <summary>
    /// Configures what services the different controllers should use.
    /// </summary>
    /// <typeparam name="TCallbackService">Flexible class, that should inherit from <see cref="CallbackService"/></typeparam>
    /// <typeparam name="TLogoutService">Flexible class, that should inherit from <see cref="LogoutService"/></typeparam>
    public static IServiceCollection ConfigureMinimalAPIServices<TCallbackService, TLogoutService>(this IServiceCollection services)
        where TCallbackService : class, ICallBackService
        where TLogoutService : class, ILogoutService
    {
        services.AddScoped<ICallBackService, TCallbackService>();
        services.AddScoped<ILogoutService, TLogoutService>();

        return services;
    }

    #endregion

    private readonly static ApiConfiguration _configuration = new();

    /// <summary>
    /// Configures custom API routes or default routes.
    /// </summary>
    public static WebApplication ConfigureApiRoutes(
            this WebApplication app, 
            Action<ApiConfiguration>? configuration = null
        )
    {
        configuration?.Invoke(_configuration);

        app.MapGet(_configuration.RedirectRoute, Redirect);
        app.MapGet(_configuration.CallbackRoute, CallBackAsync);
        app.MapGet(_configuration.LogoutRoute, Logout);

        return app;
    }

    #region MinimalAPI 

    private static string _provider = "WsFederation";

    private static async Task Redirect(
        [FromServices] SignInManager<IdentityUser> signInManager,
        HttpContext context)
    => await context.ChallengeAsync(_provider, signInManager.ConfigureExternalAuthenticationProperties(_provider, _configuration.CallbackRoute));


    private static async Task CallBackAsync(
            [FromServices] ICallBackService callback,
            HttpContext context
        )
    => await callback.CallbackAsync(context, _configuration.CallbackEndRoute);

    public static async Task Logout(
            [FromServices] ILogoutService logoutService,
            HttpContext context
        )
    => await logoutService.LogoutAsync(context, _configuration.LogoutEndRoute);

    #endregion
}