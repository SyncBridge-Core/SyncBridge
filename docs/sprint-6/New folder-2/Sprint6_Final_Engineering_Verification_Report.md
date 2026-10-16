# Sprint 6 final engineering verification report

Date: 2026-10-15, America/New_York. **Disposition: PASS for bounded Batch B engineering verification; Team acceptance PENDING; Git integration NOT AUTHORIZED / NOT EXECUTED.**

## Final execution results

| Fresh Batch B execution | Discovered/executed | Passed | Failed | Skipped | Evidence under artifacts/test-evidence/sprint-6/batch-b |
|---|---:|---:|---:|---:|---|
| Final integrated full suite | 302 | 302 | 0 | 0 | Integrated_Final.log/.trx; Test_Catalog_Final.json |
| Non-SQL subset of integrated suite | 284 | 284 | 0 | 0 | Catalog: existing 261 + Batch A REST 13 + Batch B non-SQL 10 |
| Live SQL subset of integrated suite | 18 | 18 | 0 | 0 | Existing SQL 17 + new actual HTTP SQL equivalence 1 |
| Independent final SQL repeat | 18 | 18 | 0 | 0 | SQL_Final_Repeat.log/.trx |

Unique tested cases: **302**, not 320. Independent repeat executions are not added to the unique count. Initial B focused 9/9, initial integrated 302/302 and initial SQL repeat 18/18 are retained separately. No executed test failure remains or is hidden. Batch A's 291 unique / 13 REST repeat and Sprint 5's 278 are historical evidence, not substitutes for these fresh executions.

Restore succeeded. Full solution builds passed with **0 warnings / 0 errors** (`Build.log`, `Build_Final.log`, `Build_ClockCorrected.log`); final integrated invocation also compiled the last audit-normalization test change without diagnostics. Transitive advisory check reported no vulnerable packages from configured feeds (`Package_Advisories.log`). No package/project-reference changes were made.

## Exact commands and chronology

Working directory: `C:\Engineering\GitHub\SyncBridge.Core`. Commands used the approved elevated execution path already established in Batch A. Logs are sanitized factual outputs; no credential values are included. Opt-in variables were set only in each SQL test command's child process.

```powershell
& artifacts/test-evidence/sprint-5/batch-a/A4_ReadOnly_Preservation.ps1 -OutputPath artifacts/test-evidence/sprint-6/batch-b/Development_Before.json
dotnet restore src/SyncBridge.Core.slnx *> artifacts/test-evidence/sprint-6/batch-b/Restore.log
dotnet build src/SyncBridge.Core.slnx --no-restore --disable-build-servers *> artifacts/test-evidence/sprint-6/batch-b/Build.log
dotnet list src/SyncBridge.Core.slnx package --vulnerable --include-transitive *> artifacts/test-evidence/sprint-6/batch-b/Package_Advisories.log
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~UiApiEquivalenceTests|FullyQualifiedName~RestBoundaryRegressionTests' --logger 'trx;LogFileName=Equivalence_Focused.trx' --results-directory artifacts/test-evidence/sprint-6/batch-b *> artifacts/test-evidence/sprint-6/batch-b/Equivalence_Focused.log
dotnet build src/SyncBridge.Core.slnx --no-restore --disable-build-servers *> artifacts/test-evidence/sprint-6/batch-b/Build_Final.log
$env:SYNCBRIDGE_APPROVED_LIVE_SQL='1'
$env:SYNCBRIDGE_TEST_SQL_CONNECTION='Server=localhost;Database=SyncBridgeCore_IntegrationTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-build --no-restore --logger 'trx;LogFileName=Integrated.trx' --results-directory artifacts/test-evidence/sprint-6/batch-b *> artifacts/test-evidence/sprint-6/batch-b/Integrated.log
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-build --no-restore --filter 'Category=LiveSqlServer' --logger 'trx;LogFileName=SQL_Repeat.trx' --results-directory artifacts/test-evidence/sprint-6/batch-b *> artifacts/test-evidence/sprint-6/batch-b/SQL_Repeat.log
dotnet build src/SyncBridge.Core.slnx --no-restore --disable-build-servers *> artifacts/test-evidence/sprint-6/batch-b/Build_ClockCorrected.log
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-restore --disable-build-servers --logger 'trx;LogFileName=Integrated_Final.trx' --results-directory artifacts/test-evidence/sprint-6/batch-b *> artifacts/test-evidence/sprint-6/batch-b/Integrated_Final.log
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-build --no-restore --filter 'Category=LiveSqlServer' --logger 'trx;LogFileName=SQL_Final_Repeat.trx' --results-directory artifacts/test-evidence/sprint-6/batch-b *> artifacts/test-evidence/sprint-6/batch-b/SQL_Final_Repeat.log
& artifacts/test-evidence/sprint-5/batch-a/A4_ReadOnly_Preservation.ps1 -OutputPath artifacts/test-evidence/sprint-6/batch-b/Development_After.json
git ls-remote origin refs/heads/main refs/heads/codex/wp6-sprint6-rest-api
git diff --check
& artifacts/test-evidence/sprint-6/batch-b/Readiness_Inventory.ps1
```

The final development check occurs after final SQL disposal. The initial focused invocation preceded adding the optional-activity case and stronger provider/foreign-step checks, so it contains nine cases; all eleven final B cases are in the full run. `Build_Final.log` was refreshed for the explicit SQL-provider assertion before the first integrated run; all builds succeeded. SQL opt-in/connection assignments shown above were repeated for every standalone SQL-enabled invocation, not persisted in repository configuration.

The long initial full run was inspected using elevated `Get-Process` plus read-only SELECTs for request status/counts in the exact isolated catalog. It finished with exit 0, 302 passed and complete TRX before a stop request took effect. No hang dump or failed/aborted test is claimed. This interim diagnostic did not modify data or bypass ownership safeguards.

## UI/API, security and SQL evidence

`UiApiEquivalenceTests` executes paired native-form and JSON workflows in Development and Production using real middleware, identity, Application services, Domain and repositories. `RestSqlEquivalenceTests` executes the same comparison in a Production HTTP host, explicitly verifies SQL Server provider/exact isolated catalog and reconciles actual persisted rows. `Equivalence_Results_Final.txt` and `SQL_HTTP_Reconciliation.txt` include normalized evidence and cleanup assertions.

All four capabilities have positive and negative behavioral evidence: order retrieval/read-only counts; eligible/unreleased/duplicate initiation; Created state and trusted audit; mandatory material/equipment; ordered execution; unsigned rejection and existing Quality signature; step data/timestamps/completing user; deviation originating batch/activity, Open/Exception/Closed and explicit Operator resume; missing/foreign execution rejection; Completed protection; induced post-INSERT failure and unchanged view/audit snapshot. No SQL state preparation bypasses the approved services; only existing exact test-owned cleanup deletes rows in the disposable database.

Ten new non-SQL cases add paired Development/Production context, workflow, token/revocation checks, three forbidden-role permutations and optional batch-level deviation. They reject spoofed actor/role/approval bodies, query values and identity headers, missing/invalid/cross-user tokens, revoked active-user cookies and unauthorized mutations; additive roles remain valid. Existing `RestApiTests` repeats all route authentication, structured HTTP outcomes and generic unhandled 500 errors in both environments. Existing Razor/domain/service/authentication suites remain passing.

Existing 17 SQL cases independently preserve unique/FK constraints, original migration, seed, all five state reconstruction, optional fields/amendments, immutable signatures, append-only audit, Completed protection, rollback and concurrency semantics. The new SQL HTTP case adds exact persisted user/association/timestamp/data/count evidence and no duplicate signatures/audits. Both final SQL-enabled invocations use the original fixture's exact server/catalog/authentication checks, application lock, owner marker and safe creation/disposal. No safeguards were weakened.

## Correction and impact

One test-only composition defect was discovered in passing output: direct fixture setup used its advancing clock while the HTTP host used system time. Added the existing TimeProvider to ConfigureTestHost and stronger timestamp assertions. Normalize simultaneous audit events by action rather than relying on unspecified database tie ordering. No production code, Domain behavior, role policy, service design, persistence mapping or accepted Batch A result was changed.

Batch B source impact: `UiApiEquivalenceTests.cs` +280 lines, `RestBoundaryRegressionTests.cs` +144, `SecuredServiceFixture.cs` +2, `SqlServerBatchBIntegrationTests.cs` +1/-1 (internal reuse of existing guarded cleanup): **+427/-1 test lines, 0 production lines**, 11 new cases. Sprint 6 production remains Batch A's +85 lines; no dependencies added. Readiness manifests give every actual candidate path.

## Architecture, preservation and readiness

`Architecture.json` records zero controller persistence references, unchanged inner layers/projects and the original migration files. Existing architecture regression passes. Source-document hashes match Batch A. Development Before/After JSON are compared byte-for-byte, preserving all 15 ordered-row hashes/counts, original migration and retained completed A3 evidence: 2 steps, 1 material, 1 equipment, 1 deviation, 1 signature, 17 batch audits and 4 order audits. No test history enters development; no protected legacy access occurs. Isolated database absence after final disposal is recorded separately.

Fresh Git/readiness checks preserve all ten inherited originals, ignored outputs and user-established Batch A document location. Existing decision-register text is retained with a labeled B addendum. The exact proposed inclusion list excludes all protected originals, secrets and build/database outputs. Pattern scan and whitespace check results are recorded without exposing sensitive content. No staged files or Git integration actions exist. Read-only remote refs match HEAD for main/feature.

Requirements and design align with FR-012, supporting FR-001–009, applicable BR-001–006/008, NFR-001/003/005 and accepted ADRs/decisions; see final matrix. FR-011, BR-007, UC-005 EBR/SSRS remain Sprint 7. TestServer/fixture results do not certify deployed external-network behavior, production load or formal security/reliability targets. These are factual scope limits, not unresolved failures. Recommend Sam final engineering review and Team acceptance consideration; Git closure remains a separate explicit authorization.
