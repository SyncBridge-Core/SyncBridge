# Sprint 4 final engineering verification report

Date: 2026-10-13 (America/New_York). Branch: `feature/wp6-sprint4-application-services`.

**Batch B completed: 252/252 tests passed, including 17/17 live SQL tests; no failures or skips. Final build has zero warnings/errors, and the published HTTPS authentication host starts successfully. Sprint 4 is recommended for Team acceptance. No commit, merge or push was performed.**

## Authority and repository baseline

Executed `Z:\InstallationPackages\Transfer_Folder\Sprint_4\CodexPrompt_Sprint_4_Batch_B.txt`. It explicitly authorizes controlled localhost SyncBridgeCore tests, unsandboxed Windows execution where required, routine verified-defect corrections and closure evidence. It excludes the legacy database, destructive development-database operations, golden-document edits, migrations and Git integration.

HEAD remains `c42fce5a345c5fe2bb8b58843302a170f0d841c1`; ancestry verification confirms the accepted Sprint 3 main baseline. Accepted implementation commit `a4bc59658b00ec464480eb6b346afd90dbaae5f6` remains in history. Batch A production source, its reports and raw evidence were present. The user-uploaded Sprint 3 handoff remains preserved. No unrelated change, staged file, schema/migration modification or rewritten historical commit was found.

Source authority remains corrected SRS, corrected SDD and amended DBDS in `C:\Engineering\Golden_Documents`, followed by the realigned execution plan, uploaded handoff and Batch A reports. Relevant coverage: SRS §2.2, FR-001–010, UC-001–004, BR-001–006/008, NFR-001/002/003/005; SDD §§3.2, 4.2–4.5, 5.1–5.4 and ADR-001/003/004/006/007/008/009; DBDS §5, §7.2, §8.2 and AMD-001/002; realigned plan §4.4. The source register stores SHA-256 fingerprints and actual locations. Golden documents were not edited.

## SQL target verification and isolation

The approved target was explicitly configured for each test process as:

```powershell
$env:ConnectionStrings__SyncBridgeCore='Server=localhost;Database=SyncBridgeCore;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$env:SYNCBRIDGE_APPROVED_LIVE_SQL='1'
```

No credential-bearing environment value was printed or saved. The sandboxed process did not inherit an approved connection variable/user-secret file, so the test process was configured directly from the Team-authorized endpoint, database and Windows Authentication settings. The guard was process-local. No secret store or machine/user environment setting was modified.

Read-only preflight/postflight used `sqlcmd -S localhost -d SyncBridgeCore -E -b -C` with the retained `Sql_Preflight.sql`. The connection reported `SyncBridgeCore` and NTLM Windows Authentication. The legacy database was not connected to or inspected.

Verified deployed structure:

- Exactly 15 core dbo tables, in addition to the EF migration history table.
- Migration `20261013010705_InitialCreate`, EF version 10.0.12.
- 23 foreign keys; all 23 have restricted/no-action deletes.
- No user views, procedures or DML triggers.
- DeviationCode: required nvarchar(40), unique `UQ_Deviation_DeviationCode`.
- AuditEvent.AffectedEntityId: required nvarchar(256), reported as 512 bytes in SQL metadata.

Before executing existing tests, their source was inspected. Writes are rollback-isolated except the accepted idempotent seed operation; all 11 seed rows were already present, so seed calls were no-ops. The existing restricted-delete test is an intentionally rejected FK operation inside a rollback transaction, not a successful deletion. No migration, drop, truncation, recreation or schema operation was run.

Five additional Batch B SQL cases use `S4B-` plus a canonical GUID N order number, with records associated exclusively with that unique test order. `finally` cleanup validates the number and removes only that exact order and its associated test-owned children/audits in one transaction. The corruption case additionally deletes its own recipe using its exact generated RecipeId and code. Shared recipes, reference users/materials/equipment, existing orders and any non-test manufacturing history are excluded from cleanup. This is test-fixture cleanup, not a manufacturing correction/deletion service.

Postflight confirms the original row counts: AppUser 2; Recipe 1; RecipeStep 2; recipe material/equipment requirements 1 each; Material 1; Equipment 1; ProductionOrder 2. All Batch, execution, verification, deviation, signature and audit tables contain zero rows. No test residue remains, and the original baseline is unchanged.

## Executed commands and outcomes

All raw evidence paths below are under `artifacts/test-evidence/sprint-4/batch-b/`.

| Execution | Actual result | Evidence |
|---|---|---|
| Initial complete existing inventory | 247 passed; 0 failed; 0 skipped | `Sprint4_BatchB_Initial_Full.trx`, `Initial_Full_Test.log` |
| Four additional provider cases | 4 passed; two xUnit assertion-style warnings | `Sprint4_BatchB_Provider_Focused.trx`, `Provider_Focused.log` |
| Expanded pre-correction full suite | 251 passed; 0 failed; 0 skipped | `Sprint4_BatchB_Final_Full.trx` |
| Persisted-data classification regression before fix | 1 failed: BusinessRuleRejected/InvalidInput instead of TechnicalFailure | `CorruptData_Initial.trx`, `CorruptData_Initial.log` |
| Provider retest including correction | 5 passed; 0 failed; 0 skipped | `Sprint4_BatchB_Provider_Retest.trx` |
| Final full suite after correction | **252 passed; 0 failed; 0 skipped** | `Sprint4_BatchB_Final_252.trx`, `Final_252_Test.log` |
| Final solution build | Exit 0; 0 warnings; 0 errors | `Final_Build.log` |
| Final local publish | Exit 0 | `Final_Publish.log` |
| Published HTTPS host | Authentication GET 200; unconfigured login with valid antiforgery token 401 | `PublishedHost_Smoke_Result.txt`, host logs |
| Git whitespace/reference checks | Passed; no staged changes or schema/migration source changes | Git readiness report and retained inventory/diff evidence |

The final inventory comprises all 201 accepted Sprint 3 cases, 44 new Batch A non-SQL cases, 2 Batch A SQL cases and 5 Batch B SQL cases. Thus 235 non-SQL and 17 SQL cases passed; no accepted assertion was removed or weakened.

Principal test commands, with the process configuration above:

```powershell
dotnet test src/SyncBridge.Core.slnx --no-restore --logger 'trx;LogFileName=Sprint4_BatchB_Initial_Full.trx' --results-directory artifacts/test-evidence/sprint-4/batch-b
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-restore --filter 'FullyQualifiedName~SqlServerBatchBIntegrationTests' --logger 'trx;LogFileName=Sprint4_BatchB_Provider_Retest.trx' --results-directory artifacts/test-evidence/sprint-4/batch-b
dotnet test src/SyncBridge.Core.slnx --no-restore --logger 'trx;LogFileName=Sprint4_BatchB_Final_252.trx' --results-directory artifacts/test-evidence/sprint-4/batch-b
dotnet build src/SyncBridge.Core.slnx --no-restore --verbosity minimal
dotnet publish src/SyncBridge.Core/SyncBridge.Core.csproj --no-restore --configuration Debug --output src/SyncBridge.Core/bin/sprint4-batchb-publish-smoke --verbosity minimal
git diff --check
git merge-base --is-ancestor c42fce5a345c5fe2bb8b58843302a170f0d841c1 HEAD
```

The read-only SQL script initially used an unquoted reserved output alias, RowCount. It failed compilation before executing statements; quoting the alias corrected the harness. Initial output is retained. Two xUnit2031 warnings were fixed by using the supported predicate overload of Assert.Single, preserving assertion meaning. Final warnings are zero.

## Manufacturing, security and integrity findings

The live end-to-end case authenticates an existing fictional AppUser against transient test-only identity configuration, creates/releases an order, initiates a Batch, enters Ready, records failed and successful prerequisites, executes ordered steps, interrupts with a deviation, performs Quality review/approval/resolution, reloads the resolved Exception state, resumes separately through Operator permission, signs the required step, completes it and completes the Batch through Supervisor permission. Each operation uses fresh service/DbContext scopes.

Observed final evidence: Completed Batch, 19 Batch audit entries and 2 standalone order audit entries. The signature and deviation resolution are attributed to `demo.quality`; execution and the explicitly multi-role Supervisor action are attributed to the actual `demo.operator`. Historical IDs remain unique. Current verification IDs remain stable across retry updates. Lot/comment, calibration/comment, recorded process data and final disposition fields survive unrelated commands. Review by Exception identifies prerequisite/deviation issues and no longer lists the completed fixture.

Live negative cases reject missing prerequisites, skipped steps, unsigned completion, missing signature targets, unresolved resumption, incorrect review/completion roles and normal Completed changes. The full non-SQL suite additionally exercises all four roles, inactive/missing mappings, duplicate/caller role claims, Administrator-only denial, additive roles, no invented self-approval restriction, cross-Batch/wrong-record signatures, immutable signatures and append-only audits. These use the real identity/service/repository boundary, not mocked authorization.

Real SQL rollback evidence includes interruption after an executed INSERT for order/audit, current verification/audit and signature/audit. The failed verification retains its previous result; signatures and their audits are absent after failure. A forced null ProductName parameter exercises the actual SQL NOT NULL constraint (error 515). Its result is sanitized TechnicalFailure, neither order nor audit persists, tracking is empty and the same service scope subsequently succeeds.

The bounded concurrent Ready-command case uses two independent providers/scopes against the same uniquely owned Batch. Both final and focused runs observed one Success and one sanitized TechnicalFailure from SQL deadlock error 1205. Exactly one Ready transition/audit persists; no timeout (-2), partial update or duplicate evidence occurred. The deadlock victim is an expected rejected transaction, not a manufacturing state defect. Automatic retries and production-scale concurrency guarantees are not claimed.

A fresh-scope secured Batch read took **43.455 ms** in the final SQL scenario and met the approximate two-second demonstration interaction target. This is a measured local observation, not a load/capacity qualification. UI usability and EBR timing remain for later increments.

## Verified defect and minimal correction

A review regression deliberately corrupted only test-owned persisted data. Batch reads with an impossible Exception state returned BusinessRuleRejected, and an order with zero stored quantity returned InvalidInput. Those are incorrect categories for valid requests encountering invalid persisted structure. The regression failed before correction.

Batch, order and recipe repository reconstruction boundaries now translate Domain/argument exceptions raised by assembly into PersistenceIntegrityException, retaining the original exception for logging. Request validation outside reconstruction and legitimate Domain command rejections keep their existing categories. The expanded case verifies Batch, order and recipe corruption returns sanitized TechnicalFailure with diagnostic errors. No Domain manufacturing rule, schema, new abstraction or dependency was changed.

Final production inventory: **9 added files, 16 modified files, approximately +902 physical lines** against accepted HEAD, including comments, blank lines and project XML. Batch A was +875; Batch B adds **27 net production lines** for this exception-boundary correction. Growth is limited to three existing repositories and the existing persistence exception. The detailed inventory is in the Git readiness report.

## Consolidated review and architecture

Reviewed identity provenance/roles, command ordering, cancellation/rollback and tracking cleanup, signature associations, evidence immutability, optional-field synchronization, DTO boundaries, runtime composition and requirement scope. The only verified production defect found in Batch B was corrected above. No speculative refactoring or accepted Sprint 2/3 cosmetic rewrite was undertaken.

Domain still has no EF/SQL/ASP.NET Core/Infrastructure reference. Application has no concrete persistence/Presentation reference. Infrastructure implements inner contracts. Presentation's Infrastructure project dependency is build/runtime delivery only (`ReferenceOutputAssembly=false`); the runtime assembly-reference test and published-host check pass. Domain gates remain authoritative; exactly five Batch states remain. Fifteen approved tables, both amendments and the original migration remain intact.

The published host was checked over localhost HTTPS using the existing development certificate, with certificate verification bypass confined to the smoke client. The untrusted-development-certificate warning is expected; application TLS/cookie policy was not weakened and no certificate was installed/trusted. Secure cookie, antiforgery, inactive-user rejection and rate-limit tests passed in the actual test host. No deployment credentials were provisioned or committed.

## Disposition

No known failing implemented requirement remains in the executed checks. Remaining limits: demonstration-scale performance/concurrency only; deployment identity hashes/roles require approved secrets and active AppUser mappings; the role store is intentionally small configured infrastructure; rejected deviations remain blocking under accepted Domain rules. FR-011, FR-012 and BR-007 are deferred. Manufacturing screens and their usability are Sprint 5 work.

Recommend Team acceptance of Sprint 4 and review of the six closure deliverables. The rehydration correction is recorded for possible future SDD clarification; golden documents remain unchanged. The Team must separately authorize the consolidated commit, main merge and GitHub push. No such action was performed.
