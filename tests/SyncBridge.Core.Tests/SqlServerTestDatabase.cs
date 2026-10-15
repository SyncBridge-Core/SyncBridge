using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Infrastructure.Persistence;
using SyncBridge.Core.Infrastructure.Persistence.Seeding;

namespace SyncBridge.Core.Tests;

/// <summary>Owns only the explicitly named disposable database for the serialized SQL collection.</summary>
public sealed class SqlServerTestDatabase : IAsyncLifetime
{
    internal const string DatabaseName = "SyncBridgeCore_IntegrationTests";
    internal const string ConnectionVariable = "SYNCBRIDGE_TEST_SQL_CONNECTION";
    private const string Owner = "SyncBridge.Core.Tests.Disposable.v1";
    private const string OwnerProperty = "SyncBridgeTestOwner";
    private static string? readyConnection;
    private SqlConnection? coordinator;
    private string? connectionString;
    private bool ownsDatabase;

    internal static string ConnectionString => readyConnection
        ?? throw new InvalidOperationException("The isolated SQL collection fixture has not initialized.");

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("SYNCBRIDGE_APPROVED_LIVE_SQL") != "1") return;
        connectionString = ValidateConnection(Environment.GetEnvironmentVariable(ConnectionVariable));
        var master = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        coordinator = new SqlConnection(master.ConnectionString);
        try
        {
            await coordinator.OpenAsync();
            await using (var command = coordinator.CreateCommand())
            {
                command.CommandText = """
                    DECLARE @result int;
                    EXEC @result = sys.sp_getapplock @Resource=N'SyncBridge.Core.Tests:SyncBridgeCore_IntegrationTests',
                        @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=0;
                    SELECT @result;
                    """;
                if (Convert.ToInt32(await command.ExecuteScalarAsync()) < 0)
                    throw new InvalidOperationException("Another isolated SQL regression owns the test database.");
            }
            if (await ExistsAsync())
            {
                await VerifyIdentityAsync(); // Refuse to reset any pre-existing, unmarked database.
                await DropAsync();
            }
            await ExecuteMasterAsync("CREATE DATABASE [SyncBridgeCore_IntegrationTests]");
            ownsDatabase = true;
            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                if (!string.Equals(connection.Database, DatabaseName, StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected database after creation.");
                await using var command = connection.CreateCommand();
                command.CommandText = "EXEC sys.sp_addextendedproperty @name=@name, @value=@owner";
                command.Parameters.AddWithValue("@name", OwnerProperty);
                command.Parameters.AddWithValue("@owner", Owner);
                await command.ExecuteNonQueryAsync();
            }
            await VerifyIdentityAsync();
            await using var provider = CreateProvider(connectionString);
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            await db.Database.MigrateAsync();
            var seeded = await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
            var tables = await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name <> '__EFMigrationsHistory'").SingleAsync();
            if (tables != 15 || seeded != 11)
                throw new InvalidOperationException($"Unexpected isolated schema/seed: {tables} tables, {seeded} rows.");
            var migrations = await db.Database.GetAppliedMigrationsAsync();
            if (!migrations.SequenceEqual(new[] { "20261013010705_InitialCreate" }))
                throw new InvalidOperationException("Unexpected isolated migration history.");
            readyConnection = connectionString;
            Console.WriteLine($"SQL isolation: {DatabaseName}; 15 manufacturing tables; 11 seed rows; migration 20261013010705_InitialCreate.");
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        var initialized = readyConnection is not null;
        readyConnection = null;
        try
        {
            if (ownsDatabase)
            {
                await VerifyIdentityAsync();
                if (!initialized)
                {
                    await DropAsync();
                    ownsDatabase = false;
                    return;
                }
                await using (var provider = CreateProvider(connectionString!))
                await using (var scope = provider.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
                    var history = await db.Batches.CountAsync() + await db.BatchStepExecutions.CountAsync()
                        + await db.MaterialVerifications.CountAsync() + await db.EquipmentVerifications.CountAsync()
                        + await db.Deviations.CountAsync() + await db.ElectronicSignatures.CountAsync()
                        + await db.AuditEvents.CountAsync();
                    var orders = await db.ProductionOrders.CountAsync();
                    Console.WriteLine($"SQL isolation cleanup: {history} history rows; {orders} reference orders; dropping owned {DatabaseName}.");
                    // Drop the owned database even if a failed test left residue, then report the defect.
                    await DropAsync();
                    ownsDatabase = false;
                    if (history != 0 || orders != 2)
                        throw new InvalidOperationException("SQL tests left unexpected history/reference orders before disposal.");
                }
            }
        }
        finally
        {
            if (coordinator is not null) await coordinator.DisposeAsync();
            coordinator = null;
        }
    }

    internal static string ValidateConnection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Explicit {ConnectionVariable} is required; application configuration is never used.");
        var settings = new SqlConnectionStringBuilder(value);
        if (!string.Equals(settings.InitialCatalog, DatabaseName, StringComparison.Ordinal)
            || !string.Equals(settings.DataSource, "localhost", StringComparison.Ordinal)
            || !settings.IntegratedSecurity || settings.AttachDBFilename.Length != 0
            || settings.FailoverPartner.Length != 0 || settings.UserInstance)
            throw new InvalidOperationException($"SQL tests require exactly localhost/{DatabaseName} with Windows Authentication.");
        // Build a canonical connection: no inherited routing, credentials, pooling or catalog changes.
        return new SqlConnectionStringBuilder
        {
            DataSource = "localhost", InitialCatalog = DatabaseName, IntegratedSecurity = true,
            Encrypt = true, TrustServerCertificate = true, Pooling = false,
            ApplicationName = "SyncBridge.Core.IsolatedTests"
        }.ConnectionString;
    }

    private static ServiceProvider CreateProvider(string value)
    {
        var services = new ServiceCollection();
        services.AddSyncBridgePersistence(value);
        return services.BuildServiceProvider();
    }

    private async Task VerifyIdentityAsync()
    {
        if (connectionString != ValidateConnection(connectionString))
            throw new InvalidOperationException("Unexpected test connection identity.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CONVERT(nvarchar(128), DB_NAME()), CONVERT(nvarchar(128), value) FROM sys.extended_properties WHERE class=0 AND name=@name";
        command.Parameters.AddWithValue("@name", OwnerProperty);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || reader.GetString(0) != DatabaseName || reader.GetString(1) != Owner)
            throw new InvalidOperationException("Database identity/ownership check failed; destructive operation refused.");
    }

    private async Task<bool> ExistsAsync()
    {
        await using var command = coordinator!.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name=@name";
        command.Parameters.AddWithValue("@name", DatabaseName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private async Task DropAsync()
    {
        await VerifyIdentityAsync();
        // No forced disconnect, SINGLE_USER, server-wide cleanup or computed database identifier.
        await ExecuteMasterAsync("DROP DATABASE [SyncBridgeCore_IntegrationTests]");
    }

    private async Task ExecuteMasterAsync(string sql)
    {
        if (coordinator?.Database != "master") throw new InvalidOperationException("Unexpected coordinator database.");
        await using var command = coordinator.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
