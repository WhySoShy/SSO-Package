using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SSO_Package;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// In-Built identity context from SSO-Nuget.
/// </summary>
public class BuiltInContext : IdentityDbContext
{
    /// <summary>
    /// Built in database context
    /// </summary>
    /// <param name="options"></param>
    public BuiltInContext(DbContextOptions<BuiltInContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
        => base.OnModelCreating(builder);
}
