using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SyncBridge.Core.Infrastructure.Persistence;

/// <summary>
/// Creates the persistence context for authorized EF Core design-time operations.
/// </summary>
public sealed class SyncBridgeDbContextFactory : IDesignTimeDbContextFactory<SyncBridgeDbContext>
{
    /// <summary>
    /// Creates a context using the untracked SyncBridgeCore environment configuration.
    /// </summary>
    /// <param name="args">The design-time arguments.</param>
    /// <returns>The configured context.</returns>
    public SyncBridgeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SyncBridgeCore");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__SyncBridgeCore must be configured for EF Core design-time operations.");
        }

        var options = new DbContextOptionsBuilder<SyncBridgeDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new SyncBridgeDbContext(options);
    }
}
