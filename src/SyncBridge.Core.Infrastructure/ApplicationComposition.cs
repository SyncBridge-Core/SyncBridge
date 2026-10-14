using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Infrastructure.Identity;
using SyncBridge.Core.Infrastructure.Persistence;

namespace SyncBridge.Core.Infrastructure;

/// <summary>Deployment composition entry point. Presentation loads this at its composition boundary only.</summary>
public static class ApplicationComposition
{
    public static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSyncBridgePersistence(configuration.GetConnectionString("SyncBridgeCore") ?? throw new InvalidOperationException("SyncBridgeCore connection configuration is required."));
        services.Configure<ManufacturingIdentityOptions>(configuration.GetSection("ManufacturingIdentity"));
        services.AddHttpContextAccessor();
        services.AddScoped<IPasswordHasher<ManufacturingAccount>, PasswordHasher<ManufacturingAccount>>();
        services.AddScoped<ManufacturingIdentity>();
        services.AddScoped<IActorContext>(s => s.GetRequiredService<ManufacturingIdentity>());
        services.AddScoped<ICredentialAuthenticator>(s => s.GetRequiredService<ManufacturingIdentity>());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ApplicationOperation>();
        services.AddScoped<ProductionOrderService>();
        services.AddScoped<BatchService>();
    }
}
