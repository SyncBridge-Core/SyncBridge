using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Infrastructure;
using SyncBridge.Core.Infrastructure.Identity;
using SyncBridge.Core.Infrastructure.Persistence;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using SyncBridge.Core.Infrastructure.Persistence.Seeding;

namespace SyncBridge.Core.Tests;

/// <summary>Actual repositories/identity/services; SQLite by default, guarded SQL Server only with explicit approval.</summary>
internal sealed class SecuredServiceFixture : IAsyncDisposable
{
    internal const string Operator = "demo.operator";
    internal const string Supervisor = "demo.supervisor";
    internal const string Quality = "test.quality";
    internal const string Administrator = "test.admin";
    internal const string MultiRole = "test.multirole";
    internal const string Password = "Fictional-test-only-password!";
    private readonly SqliteConnection connection = new("Data Source=:memory:;Foreign Keys=True");
    private readonly ServiceProvider provider;
    private readonly FailureInterceptor failures = new();
    internal IConfiguration Configuration { get; }
    internal System.Collections.Concurrent.ConcurrentQueue<Exception> Errors { get; } = new();
    private readonly DbContextOptions<SyncBridgeDbContext> dbOptions;

    private SecuredServiceFixture(string? sqlConnection = null)
    {
        var data = new Dictionary<string, string?> { ["ConnectionStrings:SyncBridgeCore"] = sqlConnection ?? "Server=not-used.invalid;Database=SyncBridgeCore;Integrated Security=True" };
        string[] ids = [Operator, Supervisor, Quality, Administrator, MultiRole];
        string[][] roles = [[ManufacturingRoles.Operator], [ManufacturingRoles.Supervisor], [ManufacturingRoles.Quality], [ManufacturingRoles.Administrator], [ManufacturingRoles.Operator, ManufacturingRoles.Quality]];
        if (sqlConnection is not null)
        {
            ids = [Operator, "demo.quality"];
            roles = [[ManufacturingRoles.Operator, ManufacturingRoles.Supervisor], [ManufacturingRoles.Quality]];
        }
        for (var i = 0; i < ids.Length; i++)
        {
            data[$"ManufacturingIdentity:Accounts:{i}:UserId"] = ids[i];
            data[$"ManufacturingIdentity:Accounts:{i}:UserName"] = ids[i];
            data[$"ManufacturingIdentity:Accounts:{i}:PasswordHash"] = new PasswordHasher<ManufacturingAccount>().HashPassword(new(), Password);
            for (var j = 0; j < roles[i].Length; j++) data[$"ManufacturingIdentity:Accounts:{i}:Roles:{j}"] = roles[i][j];
        }
        Configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var services = new ServiceCollection().AddLogging(b => b.AddProvider(new ErrorCapture(Errors)));
        ApplicationComposition.Configure(services, Configuration);
        var dbBuilder = new DbContextOptionsBuilder<SyncBridgeDbContext>().AddInterceptors(failures);
        dbOptions = sqlConnection is null ? dbBuilder.UseSqlite(connection).ReplaceService<IModelCustomizer, PortableModelCustomizer>().Options : dbBuilder.UseSqlServer(sqlConnection).Options;
        services.AddScoped(_ => new SyncBridgeDbContext(dbOptions));
        services.AddSingleton<TimeProvider>(new AdvancingTimeProvider());
        provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    internal static async Task<SecuredServiceFixture> CreateAsync()
    {
        var fixture = new SecuredServiceFixture();
        await fixture.connection.OpenAsync();
        await using var scope = fixture.Scope();
        var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        await db.Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
        // Add fictional security test actors only; no execution history is seeded.
        foreach (var id in new[] { Supervisor, Quality, Administrator, MultiRole })
            if (!await db.AppUsers.AnyAsync(row => row.UserId == id)) db.AppUsers.Add(new AppUserRow { UserId = id, UserName = id, DisplayName = "Fictional security test actor", IsActive = true });
        await db.SaveChangesAsync();
        return fixture;
    }

    internal static SecuredServiceFixture CreateSqlServer()
    {
        return new SecuredServiceFixture(SqlServerTestDatabase.ConnectionString);
    }

    internal AsyncServiceScope Scope(string? actor = null, params Claim[] extraClaims)
    {
        var principal = actor is null ? new ClaimsPrincipal(new ClaimsIdentity()) : new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, actor) }.Concat(extraClaims), CookieAuthenticationDefaults.AuthenticationScheme));
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext { User = principal };
        return provider.CreateAsyncScope();
    }

    internal async Task<OperationResult<T>> Order<T>(string? actor, Func<ProductionOrderService, Task<OperationResult<T>>> command)
    { await using var scope = Scope(actor); return await command(scope.ServiceProvider.GetRequiredService<ProductionOrderService>()); }
    internal async Task<OperationResult<T>> Batch<T>(string? actor, Func<BatchService, Task<OperationResult<T>>> command)
    { await using var scope = Scope(actor); return await command(scope.ServiceProvider.GetRequiredService<BatchService>()); }
    internal void FailAfterFirstWrite() => failures.FailNext = true;
    internal void FailNextOrderWithSqlConstraint() => failures.InvalidOrderNameNext = true;

    internal void ConfigureTestHost(IServiceCollection services)
    {
        // Test hosts must not append accounts from a developer's real User Secrets.
        services.PostConfigure<ManufacturingIdentityOptions>(options =>
            options.Accounts = Configuration.GetSection("ManufacturingIdentity:Accounts").Get<List<ManufacturingAccount>>()!);
        services.AddScoped(_ => new SyncBridgeDbContext(dbOptions));
    }

    internal async Task PrepareAsync(string batch = "B-TEST")
    {
        Assert.True((await Order(Supervisor, s => s.CreateAsync(new("PO-TEST", "PRODUCT-TEST", "Fictional product", "RCP-DEMO-001", 100.125m)))).Succeeded);
        Assert.True((await Order(Supervisor, s => s.ChangeStatusAsync("PO-TEST", ProductionOrderAction.Release))).Succeeded);
        Assert.True((await Batch(Operator, s => s.InitiateAsync("PO-TEST", batch))).Succeeded);
        Assert.True((await Batch(Operator, s => s.MarkReadyAsync(batch))).Succeeded);
    }

    internal async Task StartAsync(string batch = "B-TEST")
    {
        await PrepareAsync(batch);
        Assert.True((await Batch(Operator, s => s.VerifyMaterialAsync(batch, "MAT-DEMO-001", Domain.Enumerations.VerificationResult.Passed))).Succeeded);
        Assert.True((await Batch(Operator, s => s.VerifyEquipmentAsync(batch, "EQ-DEMO-001", Domain.Enumerations.VerificationResult.Passed))).Succeeded);
        Assert.True((await Batch(Operator, s => s.StartProcessingAsync(batch))).Succeeded);
    }

    public async ValueTask DisposeAsync() { await provider.DisposeAsync(); await connection.DisposeAsync(); }

    public sealed class PortableModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetSchema(null);
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnType(null);
                    if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTimeOffset))
                        property.SetValueConverter(new ValueConverter<DateTimeOffset, string>(v => v.ToString("O", CultureInfo.InvariantCulture), v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture)));
                }
            }
        }
    }

    private sealed class AdvancingTimeProvider : TimeProvider
    {
        private long calls;
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero).AddMilliseconds(Interlocked.Increment(ref calls));
    }

    private sealed class FailureInterceptor : DbCommandInterceptor
    {
        internal bool FailNext;
        internal bool InvalidOrderNameNext;
        public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(System.Data.Common.DbCommand command,
            CommandEventData eventData, InterceptionResult<System.Data.Common.DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (InvalidOrderNameNext && command.CommandText.Contains("INSERT INTO [dbo].[ProductionOrder]", StringComparison.Ordinal))
            {
                var match = System.Text.RegularExpressions.Regex.Match(command.CommandText,
                    @"INSERT INTO \[dbo\]\.\[ProductionOrder\] \(([^)]*)\)[\s\S]*?VALUES \(([^)]*)\)");
                if (!match.Success) throw new InvalidOperationException("Cannot identify the isolated constraint-test parameter.");
                var columns = match.Groups[1].Value.Split(',').Select(s => s.Trim()).ToArray();
                var parameters = match.Groups[2].Value.Split(',').Select(s => s.Trim()).ToArray();
                command.Parameters[parameters[Array.IndexOf(columns, "[ProductName]")]].Value = DBNull.Value;
                InvalidOrderNameNext = false;
            }
            return new(result);
        }
        public override ValueTask<System.Data.Common.DbDataReader> ReaderExecutedAsync(System.Data.Common.DbCommand command, CommandExecutedEventData eventData,
            System.Data.Common.DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (FailNext && command.CommandText.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            { FailNext = false; result.Dispose(); throw new InvalidOperationException("Induced persistence failure with private diagnostic details."); }
            return new(result);
        }
    }

    private sealed class ErrorCapture(System.Collections.Concurrent.ConcurrentQueue<Exception> errors) : ILoggerProvider, ILogger
    {
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { if (exception is not null) errors.Enqueue(exception); }
        public void Dispose() { }
    }
}
