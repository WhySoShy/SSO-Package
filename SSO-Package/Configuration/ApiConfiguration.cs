namespace SSO_Package.Configuration;

/// <summary>
/// Route configuration for the controllers
/// </summary>
public class ApiConfiguration
{
    /// <summary>
    /// Route to our redirect controller
    /// </summary>
    public string RedirectRoute { get; set; } = "/api/sso/redirect";

    /// <summary>
    /// Route to our callback controller
    /// </summary>
    public string CallbackRoute { get; set; } = "/api/sso/callback";

    /// <summary>
    /// Route that our callback will redirect to, when done
    /// </summary>
    public string CallbackEndRoute { get; set; } = "/";

    /// <summary>
    /// Route to our logout controller
    /// </summary>
    public string LogoutRoute { get; set; } = "/api/sso/logout";
    /// <summary>
    /// Route that our logout will redirect to, when done
    /// </summary>
    public string LogoutEndRoute { get; set; } = "/";
}
