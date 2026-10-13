using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Infrastructure.Persistence.Repositories;
using SyncBridge.Core.Infrastructure.Persistence.Seeding;

namespace SyncBridge.Core.Infrastructure.Persistence;

/// <summary>
/// Provides dependency-injection registration for SyncBridge Core persistence.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SQL Server persistence context with a scoped lifetime.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="connectionString">The configured SyncBridge Core connection string.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddSyncBridgePersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<SyncBridgeDbContext>(
            options => options.UseSqlServer(connectionString));
        services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IBatchRepository, BatchRepository>();
        services.AddScoped<IApplicationUnitOfWork, ApplicationUnitOfWork>();
        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }
}
