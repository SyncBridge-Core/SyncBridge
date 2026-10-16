# Sprint 6 Batch A verification report

Date: 2026-10-15, America/New_York. New Sprint 6 execution results, not inherited Sprint 5 totals. Working directory for commands: `C:\Engineering\GitHub\SyncBridge.Core`.

| Final verification | Passed | Failed | Skipped | Evidence |
|---|---:|---:|---:|---|
| Non-SQL regression, including REST | 274 | 0 | 0 | NonSql_Final.log/.trx |
| Accepted isolated live SQL suite | 17 | 0 | 0 | SQL_Final.log/.trx |
| Separate focused REST repeat | 13 | 0 | 0 | Rest_Verified.log/.trx |

Unique final cases: 291 = 261 existing non-SQL + 13 new REST + 17 existing SQL. The separate 13-case repeat is not added to the unique total. Restore succeeded; final solution build succeeded with zero warnings and zero errors. No remaining test failures.

Commands executed for successful final verification (all logs under `artifacts/test-evidence/sprint-6/batch-a`):

```powershell
dotnet restore src/SyncBridge.Core.slnx --disable-parallel *> artifacts/test-evidence/sprint-6/batch-a/Restore_Elevated.log
dotnet build src/SyncBridge.Core.slnx --no-restore --disable-build-servers *> artifacts/test-evidence/sprint-6/batch-a/Build_Final.log
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-restore --disable-build-servers --filter 'Category!=LiveSqlServer' --logger 'trx;LogFileName=NonSql_Final.trx' --results-directory artifacts/test-evidence/sprint-6/batch-a *> artifacts/test-evidence/sprint-6/batch-a/NonSql_Final.log
$env:SYNCBRIDGE_APPROVED_LIVE_SQL='1'
$env:SYNCBRIDGE_TEST_SQL_CONNECTION='Server=localhost;Database=SyncBridgeCore_IntegrationTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-build --no-restore --filter 'Category=LiveSqlServer' --logger 'trx;LogFileName=SQL_Final.trx' --results-directory artifacts/test-evidence/sprint-6/batch-a *> artifacts/test-evidence/sprint-6/batch-a/SQL_Final.log
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~RestApiTests --logger 'trx;LogFileName=Rest_Verified.trx' --results-directory artifacts/test-evidence/sprint-6/batch-a *> artifacts/test-evidence/sprint-6/batch-a/Rest_Verified.log
& artifacts/test-evidence/sprint-5/batch-a/A4_ReadOnly_Preservation.ps1 -OutputPath artifacts/test-evidence/sprint-6/batch-a/Development_Before.json
& artifacts/test-evidence/sprint-5/batch-a/A4_ReadOnly_Preservation.ps1 -OutputPath artifacts/test-evidence/sprint-6/batch-a/Development_After.json
```

The Before command ran before live SQL testing; After ran after. Opt-in variables were confined to the SQL command process. Elevated execution was approved for restore/build/test and read-only preservation after restricted execution could not complete normally or connect to local SQL. No guard bypass or legacy database access occurred.

Other execution attempts and corrections:

- Before final exception-handler review, the non-SQL command also ran with `--no-build --no-restore`, passing 272/272, and the separate focused repeat passed 11/11. Final review moved the API exception handler inside the browser handler's downstream path so API faults are intercepted first in Production, and added `UnhandledFault_IsGenericJsonInEveryEnvironment` for Development and Production. Rebuilt successfully, reran non-SQL and focused checks; final counts above supersede these earlier passing counts. Existing SQL results remain applicable because no persistence/service code changed.

- `dotnet build src/SyncBridge.Core.slnx --no-restore`: restricted attempt exited 1, reported Build FAILED with zero warnings/errors and no compiler diagnostic. An elevated `dotnet build src/SyncBridge.Core.slnx --no-restore --disable-build-servers` then passed (Build.log), followed by the final build above after corrections.
- `dotnet restore src/SyncBridge.Core.slnx --disable-parallel *> artifacts/test-evidence/sprint-6/batch-a/Restore.log`: restricted attempt exited 1 without useful diagnostic; elevated retry passed.
- Initial `dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --filter FullyQualifiedName~RestApiTests --logger 'trx;LogFileName=Rest_Focused.trx' --results-directory artifacts/test-evidence/sprint-6/batch-a`: 1 passed / 10 failed / 0 skipped. Corrected a record validation attribute target and removed native cookie challenge Location headers for API requests. Initial TRX output bodies were stripped to remove transient cookie/token diagnostics; case identities/outcomes are retained.
- Restricted `dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-restore --disable-build-servers --filter FullyQualifiedName~RestApiTests --logger 'trx;LogFileName=Rest_Retest.trx' --results-directory artifacts/test-evidence/sprint-6/batch-a *> artifacts/test-evidence/sprint-6/batch-a/Rest_Retest.log` stalled and was interrupted; no completed test count exists.
- `dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~RestApiTests --logger 'trx;LogFileName=Rest_Final.trx' --results-directory artifacts/test-evidence/sprint-6/batch-a *> artifacts/test-evidence/sprint-6/batch-a/Rest_Final.log`: 10 passed / 1 failed / 0 skipped. Test setup attempted a deviation in Ready; Domain correctly returned 422 before fault injection. Corrected setup to InProgress, then verified rollback in the final passing runs.
- Initial restricted invocation of the same read-only preservation script failed to connect (Named Pipes error 40). Elevated before/after commands both succeeded.
- Initial inspection attempted `Get-Content docs/testing/Isolated_SQL_Test_Setup.md`; corrected location is `docs/sprint-5/Isolated_SQL_Test_Setup.md`.

HTTP evidence uses real HTTPS TestServer clients, cookie credential login, antiforgery tokens, trusted active AppUsers, secured Application services and real SQLite repositories. `RestApiTests` verifies five anonymous routes return 401 without redirects; three non-Operator roles cannot forge any selected mutation; additive Operator/Quality access; context retrieval/missing context/Admin rejection; invalid JSON/required fields/identifiers; unreleased-order rejection; initiation and trusted audit attribution; duplicate initiation; missing/failed prerequisites; out-of-sequence start; required approval; deviation batch/activity association and Exception; explicit non-API resolution/resume; successful completion/data; Completed immutability; single signature and unique audit identities; controlled 503 with atomic rollback and no leaked diagnostic. Existing non-SQL security, Domain and HTTP suites cover revocation, wrong-target approvals, detailed prerequisite gates, immutable evidence and UI behavior.

SQL evidence: all 17 accepted cases passed through SqlServerTestDatabase, which requires exact localhost/SyncBridgeCore_IntegrationTests Windows authentication, explicit opt-in, exclusive application lock and ownership marker before create/drop. It validates the original migration, 15 manufacturing tables and seed, exercises actual SQL mapping/transactions/rollback and disposes its owned test database. New REST HTTP tests use SQLite; integrated live SQL UI/API equivalence remains Batch B work.

Preservation: Development_Before.json and Development_After.json are byte-identical. All 15 tables retain identical primary-key-ordered row hashes/counts, migration `20261013010705_InitialCreate`, and retained completed A3 demonstration history (2 steps, 1 material, 1 equipment, 1 deviation, 1 signature, 17 batch audits, 4 order audits). No legacy catalog was accessed. No migration/schema/mapping changes exist; AMD-001/002 and OQ decisions remain unchanged.

Architecture/scope checks: project references reviewed; existing architecture regression passed within 274 cases; `rg -n 'DbContext|EntityFramework|Repository|SqlConnection' src/SyncBridge.Core/Controllers` returned no matches; `git diff --name-only -- src/SyncBridge.Core.Infrastructure src/SyncBridge.Core.Application src/SyncBridge.Core.Domain` was empty; `git diff --check` passed. No packages, API data store, workflow engine, unsupported states/entities, EBR, SSRS or enterprise integration were added.

Final Git: same branch/HEAD/upstream as baseline; `git rev-list --left-right --count 'HEAD...@{u}'` returned `0 0` against local upstream refs. No fetch/remote synchronization claim is made. Index remains untouched. See Working_Inventory.txt for tracked/untracked/ignored inventory and Inherited_Artifacts.json for all ten preserved inherited files. Batch A implementation is ready for Sam/Team review and Batch B integrated equivalence work; no Sprint acceptance or Git authorization is claimed.
