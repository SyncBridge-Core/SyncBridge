# Isolated SQL integration tests

The serialized `SQL Server recovery` xUnit collection owns the disposable database `SyncBridgeCore_IntegrationTests` on `localhost`. All 17 SQL tests use `SqlServerTestDatabase`; the 261 non-SQL tests retain their existing fixtures. The application development database `SyncBridgeCore` and protected legacy database are never selected by this fixture.

## Repeatable execution

Use an approved local Windows account with permission to create/drop the dedicated test database and apply its existing EF migration. Run from the repository root using the installed .NET 10 SDK and SQL Server instance:

```powershell
$env:SYNCBRIDGE_APPROVED_LIVE_SQL = '1'
$env:SYNCBRIDGE_TEST_SQL_CONNECTION = 'Server=localhost;Database=SyncBridgeCore_IntegrationTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet restore src/SyncBridge.Core.slnx
dotnet build src/SyncBridge.Core.slnx --no-restore --disable-build-servers
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-build --no-restore
# A second independent SQL-only invocation proves reprovisioning and cleanup.
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-build --no-restore --filter 'Category=LiveSqlServer'
Remove-Item Env:SYNCBRIDGE_TEST_SQL_CONNECTION
Remove-Item Env:SYNCBRIDGE_APPROVED_LIVE_SQL
```

The test connection is selected only from `SYNCBRIDGE_TEST_SQL_CONNECTION`. No fallback to `ConnectionStrings__SyncBridgeCore`, appsettings or User Secrets exists. Missing explicit configuration fails before a SQL connection opens when opted in. Without the approval flag, all 17 SQL tests skip without provisioning. No application configuration or stored credentials need changing.

## Identity and cleanup safeguards

The connection parser requires ordinal exact server `localhost`, database `SyncBridgeCore_IntegrationTests` and Windows authentication. Attached files, user instances and failover partners are rejected. The fixture then constructs a canonical connection with encryption, the accepted local certificate-trust setting and no pooling; extra routing or credentials in the input are not forwarded. These settings apply only to test connections.

A session-owned SQL application lock in `master` prevents concurrent cooperating regression processes from owning the database. Only explicit, constant `CREATE DATABASE [SyncBridgeCore_IntegrationTests]` and `DROP DATABASE [SyncBridgeCore_IntegrationTests]` statements are used. There is no server-wide cleanup, forced disconnect or computed database name.

The created database receives the database-level extended property `SyncBridgeTestOwner=SyncBridge.Core.Tests.Disposable.v1`. Before every drop, including recovery from an interrupted run, a connection to the exact test catalog must return the expected `DB_NAME()` and ownership marker. An existing unmarked database is refused and preserved. If permissions or ownership fail, stop and investigate rather than clearing another database or bypassing the guard.

The fixture applies only `20261013010705_InitialCreate` through `Database.MigrateAsync` and calls the accepted `DevelopmentDataSeeder`. It verifies 15 manufacturing tables, 11 seeded reference rows and the exact migration history. The ownership property adds no manufacturing table/entity or migration. Existing transaction rollbacks and exact test-owned order cleanup remain unchanged. At collection disposal, history must total zero and only the two seeded orders may remain; the owned database is dropped, and unexpected residue is reported as a fixture failure. A marked database left by an interrupted process is recreated on the next opted-in run after acquiring the lock and verifying ownership. No database remains after successful disposal.

The empty-history seed assertions remain intact. The ten recovery tests now use the same explicit opt-in attribute as the seven existing service/Batch B tests. Their manufacturing assertions were not weakened.

## Development evidence preservation

`artifacts/test-evidence/sprint-5/batch-a/A4_ReadOnly_Preservation.ps1` connects only to `SyncBridgeCore` for SELECT queries. It records primary-key-ordered row hashes/counts for all 15 manufacturing tables, migration history, and the retained A3 order/batch/evidence counts. Compare the generated before/after JSON files to verify that all records, including reference data and A3 evidence, remain unchanged. The script contains no provisioning, update, delete or cleanup SQL. Run it while the local development host is stopped to avoid unrelated developer writes during comparison.
