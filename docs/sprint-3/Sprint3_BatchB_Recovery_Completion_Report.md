# Sprint 3 Accelerated Batch B — Recovery Completion Report

## 1. Original connectivity blocker

The original Batch B preflight timed out because it ran inside Codex's network-restricted Windows sandbox. The SQL Server service, Windows authentication, and permissions were healthy. The approved connectivity diagnostic established that local Shared Memory access succeeds through an explicitly approved unsandboxed Windows process.

## 2. Approved recovery approach

Deployment used the Team-approved unsandboxed process only for SQL connectivity, migration, seed, and integration-test commands. The approved connection was supplied through the process-local `ConnectionStrings__SyncBridgeCore` environment variable. No tracked connection value, credential, user secret, SQL authentication change, service restart, protocol change, firewall change, or Administrator elevation was used.

## 3. Actual execution context and connection results

- Native Windows/.NET execution on the development VM.
- Local default SQL Server instance reached as `localhost` through Shared Memory.
- Windows integrated authentication succeeded under the approved current identity.
- SQL Server version observed: 17.0.1135.8.
- The account name and full connection value were excluded from committed evidence.

## 4. Target database preflight

Immediately before deployment:

- Branch was `feature/wp6-sprint3-core-domain` at expected HEAD `62b615e1068e65de36f1ec10e5949de1eedd4a88`.
- All existing uncommitted work remained present.
- The exact target `SyncBridgeCore` did not exist.
- The executing identity retained create-database permission.
- The connection's initial database was `master`; the approved configuration selected `SyncBridgeCore` for EF operations.
- Exactly one migration existed: `20261013010705_InitialCreate`.
- The migration still contained exactly 15 core table operations, no cascade delete, and both approved DBDS amendments.

No unexpected pre-existing schema or data existed, so deployment was safe to continue.

## 5. Migration deployment outcome

EF Core tooling 10.0.12 created `SyncBridgeCore` and applied `20261013010705_InitialCreate` successfully. EF recorded product version 10.0.12 in `__EFMigrationsHistory`. The application completed with exit code 0.

The existing migration was not regenerated or edited. No destructive reset, drop/recreate, `EnsureCreated`, or `EnsureDeleted` workflow was used.

## 6. Deployed 15-table schema verification

Actual SQL Server metadata—not EF model metadata alone—confirmed:

- 15 approved `dbo` core tables and no additional core table.
- 15 primary keys, including both approved composite primary keys.
- 23 foreign keys, all with `NO_ACTION` restricted-delete behavior.
- 11 unique non-primary indexes and 18 approved nonunique indexes.
- Correct column names, SQL types, lengths, nullability, decimal precision/scale, and timestamp scale.
- 12 manufacturing `datetimeoffset(7)` columns.
- No `Batch.Quantity` or `Batch.UnitOfMeasure` column.
- No user trigger, stored procedure, or view.

The separate EF migration-history table is framework metadata, not a manufacturing core table.

## 7. Fictional seed execution and idempotency

The existing `DevelopmentDataSeeder` was exercised repeatedly, including two consecutive calls in its integration test. The resulting persistent dataset contains:

- 2 fictional AppUsers.
- 1 Material and 1 Equipment record.
- 1 approved Recipe with 2 ordered RecipeSteps.
- 1 material requirement and 1 equipment requirement.
- 2 demonstration ProductionOrders.

The dataset totals 11 reference/demo rows. The second consecutive seed call returned zero changes; subsequent runs introduced no duplicates. Deterministic identity verification passed. Final database checks showed zero Batches, executions, verifications, deviations, signatures, and audit events.

## 8. Repository integration tests

Six new SQL Server integration tests passed. They verify:

- Recipe aggregate loading with ordered steps and requirements.
- ProductionOrder creation and reconstruction with stable technical/business identity.
- Batch insertion and reconstruction with stable relational identity.
- Duplicate business-code, composite-key, missing-FK, required-field, restricted-delete, overlength, and DeviationCode-uniqueness enforcement.
- Invalid same-Batch signature-target rejection.
- Completed-record and immutable-evidence guards remain covered by the full regression suite.

All test manufacturing fixtures used rollback-scoped transactions. Only the approved seed rows remain.

## 9. Aggregate reconstruction evidence

Actual SQL fixtures reconstructed all five approved Batch states: Created, Ready, InProgress, Exception, and Completed. Rich InProgress evidence verified:

- Ordered BatchStepExecution restoration.
- Material and equipment verification association and evidence.
- ElectronicSignature identity, actor, meaning, timestamp, and execution target validation.
- Deviation history and originating relationships.
- Canonical persisted AuditEvent identities and unchanged event count after loading.
- No AuditEvent or signature generation during reconstruction.

## 10. Transaction rollback evidence

One valid AppUser row and one ProductionOrder with an invalid Recipe FK were staged in the same scoped context. The unit-of-work `SaveChangesAsync` failed on the invalid relationship. The transaction was rolled back, and a new context confirmed that the valid marker row had not persisted. Final database counts showed no partial manufacturing or evidence residue.

A successful Batch add also demonstrated one-save persistence of the Batch and its creation AuditEvent.

## 11. DBDS amendment verification

- DBDS-AMD-001: deployed `DeviationCode` is `nvarchar(40) NOT NULL`, uniquely indexed, retains its human-readable Unicode value, and maps to a stable `DeviationId` surrogate.
- DBDS-AMD-002: deployed `AuditEvent.AffectedEntityId` is `nvarchar(256) NOT NULL`.
- An exact 180 UTF-16-unit identifier containing BMP and supplementary Unicode characters round-tripped unchanged.
- A 257-unit identifier was rejected by SQL Server without truncation.

## 12. Build and total regression results

- Restore: successful.
- Build: successful, 0 errors and 0 warnings.
- Focused SQL Server integration tests: 6 passed, 0 failed, 0 skipped.
- Full suite: 197 passed, 0 failed, 0 skipped.
- Accepted 191-test recovery baseline was retained and increased by six database tests.

## 13. Source and test files added/modified

Added:

- `tests/SyncBridge.Core.Tests/SqlServerRecoveryIntegrationTests.cs`
- `docs/sprint-3/Sprint3_BatchB_Recovery_Completion_Report.md`
- Eight evidence files under `artifacts/test-evidence/sprint-3/batch-b/recovery/`

No production source, Domain behavior, Application contract, EF mapping, migration, Presentation code, or golden engineering document required correction.

## 14. Database changes performed

Only the authorized `SyncBridgeCore` database was changed:

1. Database created by EF Core migration deployment.
2. Initial migration `20261013010705_InitialCreate` applied.
3. Exactly 15 approved core tables and migration history created.
4. Eleven approved fictional reference/demo seed rows persisted.

Integration fixtures were rolled back. No additional database, schema, trigger, procedure, view, or seed-management table was created.

## 15. Legacy database confirmation

The legacy database remained strictly untouched. It was not connected to, enumerated, inspected, queried, backed up, renamed, detached, altered, deleted, or maintained.

## 16. Git branch, HEAD, and working-tree status

- Branch: `feature/wp6-sprint3-core-domain`
- HEAD: `62b615e1068e65de36f1ec10e5949de1eedd4a88`
- Working tree: intentionally dirty with accepted Task 3, Batch A, Batch B, connectivity-recovery, and deployment-recovery work.
- No commit, push, merge, reset, stash, clean, rebase, or branch change occurred.

## 17. Unresolved defects or risks

- SQL integration tests intentionally require `ConnectionStrings__SyncBridgeCore`; they must run in an approved context with that process-local value supplied.
- The accepted repository contracts expose add/load operations, not general update commands. Optional DB-only field preservation was therefore validated across supported no-tracking load operations. No update contract was invented.
- Verification and signature command persistence beyond the accepted Batch add/load contract remains an Application-use-case concern for later authorized work; reconstruction and integrity enforcement are verified now.
- The supplied deployment prompt file ends mid-item in its raw-evidence list. All complete requirements preceding that truncation were implemented and the evidence categories implied by the complete text were retained.

No unresolved schema, migration, seed, repository, reconstruction, transaction, or regression defect was found within the authorized Batch B scope.

## 18. Final result

**READY FOR TEAM REVIEW** — connectivity, database creation, migration deployment, actual SQL schema reconciliation, repeatable seed execution, repository/integrity/rollback integration testing, and the complete 197-test regression suite succeeded. Batch C has not started and still requires explicit Team authorization.
