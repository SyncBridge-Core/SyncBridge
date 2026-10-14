# Sprint 4 final evidence index

Date: 2026-10-13. Final result: **252 passed, 0 failed, 0 skipped; 17 live SQL cases passed; build 0 warnings/errors; local publish/HTTPS host passed.** Team acceptance and Git integration remain pending.

## Current closure documents

- [Final engineering verification](Sprint4_Final_Engineering_Verification_Report.md): commands, source authority, live SQL safety/schema, actual failures/correction, concurrency and host results.
- [Requirements and evidence matrix](Sprint4_Requirements_and_Evidence_Matrix.md): FR-001–010, applicable NFR/BR/ADR obligations and deferred FR-011/012/BR-007.
- [Implementation decision register](Sprint4_Implementation_Decision_Register.md): approved D1–D4, resolved Exception rehydration clarification and bounded implementation choices.
- [Final closure report](Sprint4_Final_Closure_Report.md): acceptance recommendation, exact counts/size, limits and Sprint 5 inputs.
- [Git readiness report](Sprint4_Git_Readiness_Report.md): exact proposed file inventory, exclusions and integration gates.

## Authoritative inputs and historical evidence

- Corrected SRS/SDD, amended DBDS and realigned execution plan: `C:\Engineering\Golden_Documents`.
- [Accepted uploaded handoff](../sprint-3/WP6_Sprint3_to_Sprint4_Engineering_Handoff.docx).
- [Batch A A1 inspection/role gate](Sprint4_BatchA_A1_Inspection_and_Authorization_Gate.md).
- [Batch A completion report](Sprint4_BatchA_Completion_Report.md).
- [Sprint 3 evidence index](../sprint-3/Sprint3_Final_Evidence_Index.md), including accepted OQ decisions and historical 201-test/10-SQL results.
- Execution prompts: `Z:\InstallationPackages\Transfer_Folder\Sprint_4\CodexPrompt_Sprint_4_Batch_A.txt`, `CodexPrompt_Sprint_4_Batch_A2.txt`, `CodexPrompt_Sprint_4_Batch_B.txt`.
- Current source paths/SHA-256 fingerprints: [Source_Register.json](../../artifacts/test-evidence/sprint-4/batch-b/Source_Register.json). Golden files were not edited.

## Machine-readable verification

All paths in this section are under `artifacts/test-evidence/sprint-4/batch-b/`.

| Artifact | Meaning |
|---|---|
| [Sprint4_BatchB_Final_252.trx](../../artifacts/test-evidence/sprint-4/batch-b/Sprint4_BatchB_Final_252.trx) | Authoritative final full suite: 252/252, no skips/failures |
| `Final_252_Test.log` | Exact final build/test console output |
| [Final_SQL_Test_Results.json](../../artifacts/test-evidence/sprint-4/batch-b/Final_SQL_Test_Results.json) | Extracted 17 live SQL outcomes/durations from final TRX |
| [SQL_Scenario_Summary.txt](../../artifacts/test-evidence/sprint-4/batch-b/SQL_Scenario_Summary.txt) | Captured live manufacturing, rollback, SQL515, SQL1205, corruption and 43.455 ms read observations |
| `Sprint4_BatchB_Initial_Full.trx` | Independently verified original 247-case Batch A/Sprint 3 inventory |
| `Sprint4_BatchB_Provider_Focused.trx` | First four added live-provider cases, all passing |
| `Sprint4_BatchB_Final_Full.trx` | Intermediate 251-case passing suite before adding corruption regression |
| `CorruptData_Initial.trx`, `CorruptData_Initial.log` | Retained actual failing classification regression; not final passing evidence |
| `Sprint4_BatchB_Provider_Retest.trx` | Five focused provider cases passing after correction |
| `Final_Build.log`, `Final_Publish.log` | Zero-warning/error build and successful local publish |
| [PublishedHost_Smoke_Result.txt](../../artifacts/test-evidence/sprint-4/batch-b/PublishedHost_Smoke_Result.txt) | Final HTTPS GET 200 and unconfigured login 401; real runtime DI activation |
| `PublishedHost.stdout.log`, `PublishedHost.stderr.log` | Runtime startup and expected development-certificate warning; no deployment secrets |

## SQL and review evidence

- [Sql_Preflight.sql](../../artifacts/test-evidence/sprint-4/batch-b/Sql_Preflight.sql): read-only guarded schema/migration/amendment/count inspection, scoped only to the connected accepted database.
- [Sql_Postflight_Output.txt](../../artifacts/test-evidence/sprint-4/batch-b/Sql_Postflight_Output.txt): SyncBridgeCore/NTLM; 15 core tables; initial migration; 23 restricted FKs; no added views/procedures/triggers; both amendments; unchanged 11 reference/demo rows and zero manufacturing history.
- `Sql_Preflight_Output_Commit_Sanitized.txt`: initial successful preflight, with SQLCMD trailing-space padding removed only for commit preparation. The original `Sql_Preflight_Output.txt` is preserved locally and intentionally uncommitted. `Sql_Preflight_Initial_Syntax_Failure.txt` retains the rejected output-alias syntax attempt.
- `Architecture_and_Code_Review.md`: consolidated review findings, dependency/scope checks and bounded correction.
- `Working_Tree_Inventory.txt`, `Production_Inventory.json`, `Git_Diff_Check.txt`: final source/evidence inventory and whitespace/ancestry observations.
- `Final_Verification_Summary.json`: compact final result/counter/inventory summary.

Batch A raw artifacts remain under `artifacts/test-evidence/sprint-4/batch-a/`. Their originally unexecuted live-SQL gap is now closed by Batch B; historical reports retain the status valid when written.

## Source and tests

Production service/result/DTO sources: `src/SyncBridge.Core.Application/Services/ProductionOrderService.cs`, `BatchService.cs`, `ApplicationOperation.cs`, `OperationResult.cs`. Identity contracts are under Application `Contracts/Identity`; repository/UoW contracts under `Contracts/Persistence`.

Infrastructure source: `ApplicationComposition.cs`, `Identity/ManufacturingIdentity.cs`, existing `Persistence/ApplicationUnitOfWork.cs`, three `Persistence/Repositories` implementations with `BatchCommandPersistence.cs`/`AuditPersistence.cs`, accepted assembly/factory path and `PersistenceIntegrityException.cs`. Presentation's minimal authentication composition is `src/SyncBridge.Core/Program.cs`; manufacturing rules remain under Domain.

New Sprint 4 test sources: SecuredServiceFixture, SecuredApplicationServiceTests, SecuredPersistenceIntegrityTests, AuthenticationBoundaryTests, SqlServerApplicationServiceTests and SqlServerBatchBIntegrationTests, all under `tests/SyncBridge.Core.Tests`. Existing SqlServerRecoveryIntegrationTests supplies the ten accepted provider cases; its assertions remain unchanged.

The final matrix identifies method-level passing evidence. Binary publish/build outputs, credentials and local caches are excluded from the proposed commit. No staging, commit, merge or push occurred.
