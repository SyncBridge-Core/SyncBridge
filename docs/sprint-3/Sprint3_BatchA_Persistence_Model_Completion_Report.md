# WP-6 Sprint 3 Accelerated Batch A Persistence Model Completion Report

## 1. Implementation Summary

Accelerated Batch A completes the approved 15-table EF Core model and the coordinated OQ-1, OQ-3, OQ-3A, and OQ-4 preparation authorized for Sprint 3 Tasks 4–5. Infrastructure now contains one row representation and one Fluent API configuration per DBDS table. Domain entities expose seven Infrastructure-only rehydration paths without replaying manufacturing behavior.

No database connection, migration, repository, unit-of-work implementation, seed data, SQL script, or schema operation was created or executed.

## 2. Task 3 Starting Baseline and Preserved Changes

| Item | Verified value |
|---|---|
| Repository root | `C:\Engineering\GitHub\SyncBridge.Core` |
| Branch | `feature/wp6-sprint3-core-domain` |
| HEAD | `62b615e1068e65de36f1ec10e5949de1eedd4a88` |
| Starting working tree | Intentionally dirty with accepted Task 3 changes |
| Task 3 mappings | Seven rows and seven configurations present |
| Task 3 tests | Ten Task 3 metadata/registration tests present at start |

All Task 3 source, tests, report, and evidence were preserved. No reset, clean, stash, commit, history rewrite, or branch operation occurred.

## 3. Tasks 4–5 Implementation Coverage

Batch A added the remaining eight DBDS row models and configurations for ProductionOrder, Batch, BatchStepExecution, MaterialVerification, EquipmentVerification, Deviation, ElectronicSignature, and AuditEvent. `SyncBridgeDbContext` now exposes and explicitly configures exactly 15 tables.

The implementation also:

- Applies DBDS-AMD-001 for `DeviationCode`.
- Applies DBDS-AMD-002 for `AuditEvent.AffectedEntityId`.
- Implements OQ-1 full-order Batch quantity derivation and removes unit of measure.
- Implements seven internal entity-specific OQ-4 rehydration factories.
- Grants friend access only to `SyncBridge.Core.Infrastructure`.
- Adds strict OQ-3 technical GUID conversion.
- Adds UTF-16 DBDS length validation without truncation.
- Expands metadata and boundary tests.

## 4. Fifteen-Table Mapping Inventory

| Table | Primary identity | Important mapping |
|---|---|---|
| AppUser | UserId | Unique UserName |
| Recipe | RecipeId | Unique RecipeCode |
| RecipeStep | RecipeStepId | Unique RecipeId and StepNumber |
| RecipeMaterialRequirement | RecipeId and MaterialId | Nullable decimal(18,3) |
| RecipeEquipmentRequirement | RecipeId and EquipmentId | Restricted Recipe and Equipment FKs |
| Material | MaterialId | Unique MaterialCode |
| Equipment | EquipmentId | Unique EquipmentCode; nullable date |
| ProductionOrder | OrderId | Unique OrderNumber; Recipe FK; decimal(18,3) quantity |
| Batch | BatchId | Unique BatchNumber; Order FK; five states; no quantity/unit columns |
| BatchStepExecution | StepExecutionId | Unique BatchId and StepNumber; three states |
| MaterialVerification | VerificationId | Unique BatchId and MaterialId |
| EquipmentVerification | VerificationId | Unique BatchId and EquipmentId |
| Deviation | DeviationId | Unique required DeviationCode; optional execution FK |
| ElectronicSignature | SignatureId | Batch/User FKs; no polymorphic related-record FK |
| AuditEvent | AuditEventId | Optional Batch; required User; 256-unit affected identity |

All relationships use restricted delete behavior. Timestamps use `datetimeoffset(7)`, quantities use `decimal(18,3)`, and identifiers use the approved GUID or bounded string representation.

## 5. Approved Domain Changes

`Batch` creation now accepts an associated `ProductionOrder`, copies its `ProductionOrderNumber` and authoritative positive `Quantity`, and retains no ProductionOrder object reference. An independently supplied Batch quantity is no longer possible. `PlannedQuantity` remains immutable derived state.

`Batch.UnitOfMeasure`, its constructor argument, and the unused `UnitOfMeasure` value object were removed. No Batch quantity or unit column was introduced. Existing manufacturing lifecycle, prerequisite, ordered-step, deviation, electronic-signature, audit, and completed-record behavior remains passing.

## 6. Rehydration Factory Implementation

Private full-state constructors and `internal static Rehydrate(...)` factories were implemented for:

1. ProductionOrder.
2. Batch.
3. BatchStepExecution.
4. MaterialVerification.
5. EquipmentVerification.
6. Deviation.
7. ElectronicSignature.

The factories restore supplied identifiers, statuses, actors, timestamps, evidence, and owned collections without invoking creation factories or lifecycle methods. They validate local enum/state shape, associations, duplicate identities, evidence correlation, and collection consistency. Collections are copied into entity-owned storage.

The Domain project grants internal access only to `SyncBridge.Core.Infrastructure`; it has no reverse project reference. Cross-row reconstruction, database consistency, and full repository assemblers remain Batch B work.

## 7. Identifier and Enum Conversion Support

`TechnicalGuidConverter` accepts only nonempty exact lowercase canonical GUID `D` text and formats nonempty GUIDs back to that representation. It rejects uppercase, compact, blank, invalid, and empty-GUID values without normalization or replacement.

Business identifiers remain strings. EF mappings use closed enum conversions for ProductionOrder, Batch, BatchStepExecution, Deviation, ElectronicSignature, and AuditEvent values. In-process database values use the approved spaced text such as `In Process`, `In Progress`, and `Under Review`.

`DbdsStringLengthValidator` validates required persistence strings by UTF-16 code-unit count and returns the original value unchanged. Tests cover the approved 180-unit verification identity, the 256-unit amended boundary, supplementary Unicode, and rejection above the limit.

## 8. EF Core Metadata Verification

Nineteen focused metadata tests passed. They inspect the SQL Server relational model without connecting to a database and verify:

- Exactly 15 `dbo` tables and no extra entity table.
- All primary, composite, foreign, and unique keys.
- Restricted deletes.
- Approved practical and supporting indexes.
- Exact column types, lengths, precision, scale, and nullability.
- `datetimeoffset(7)` and nullable `date` mappings.
- Five Batch states and three execution states through closed converters.
- Required unique `DeviationCode nvarchar(40)`.
- Required `AuditEvent.AffectedEntityId nvarchar(256)`.
- Absence of Batch quantity and unit columns.
- Absence of a polymorphic related-record foreign key.

This is model metadata validation, not SQL Server integration or generated-schema validation.

## 9. Actual Files Created Modified or Deleted

Batch A created:

- Eight row files under `src/SyncBridge.Core.Infrastructure/Persistence/Rows/` for the remaining tables.
- Eight matching configuration files under `src/SyncBridge.Core.Infrastructure/Persistence/Configurations/`.
- `src/SyncBridge.Core.Infrastructure/Persistence/Conversion/DbdsEnumConverters.cs`.
- `src/SyncBridge.Core.Infrastructure/Persistence/Conversion/TechnicalGuidConverter.cs`.
- `src/SyncBridge.Core.Infrastructure/Persistence/Validation/DbdsStringLengthValidator.cs`.
- `tests/SyncBridge.Core.Tests/PersistenceBoundaryTests.cs`.
- This completion report.
- Six evidence files under `artifacts/test-evidence/sprint-3/batch-a/`.

Batch A modified:

- `src/SyncBridge.Core.Domain/SyncBridge.Core.Domain.csproj`.
- The seven Domain entity files listed in Section 6.
- `src/SyncBridge.Core.Infrastructure/Persistence/SyncBridgeDbContext.cs`.
- `tests/SyncBridge.Core.Tests/PersistenceModelTests.cs`.
- `tests/SyncBridge.Core.Tests/DomainTestFixtureExtensions.cs`.
- Eight existing Batch-related test files: BatchAuditEvidenceTests, BatchDeviationLifecycleTests, BatchLifecycleTests, BatchPrerequisiteVerificationTests, BatchStepExecutionTests, CompletedRecordProtectionTests, ElectronicSignatureGateTests, and OrderedWorkflowExecutionTests.

Batch A deleted:

- `src/SyncBridge.Core.Domain/ValueObjects/UnitOfMeasure.cs`.

The prior Task 3 working-tree inventory remains as recorded in `docs/sprint-3/Sprint3_Task3_Completion_Report.md`; its files remain uncommitted and preserved.

## 10. Build and Test Results

Build command:

`dotnet build src\SyncBridge.Core.slnx --no-restore --verbosity minimal`

Result: exit 0, 0 errors, 0 warnings.

Full test command:

`dotnet test src\SyncBridge.Core.slnx --no-restore --no-build --verbosity minimal`

Result: 188 passed, 0 failed, 0 skipped. This preserves the accepted 159-test baseline and the Task 3 additions.

Targeted metadata tests: 19 passed. Targeted OQ-1/conversion/length boundary tests: 10 passed.

## 11. Architectural Compliance

- Domain and Application remain free of EF Core, SQL Server, DbContext, and SQL client dependencies.
- Domain references no Infrastructure type or project.
- Infrastructure alone owns rows, Fluent configuration, DbContext, provider registration, and persistence conversions.
- Application and Presentation source were not modified.
- No repository, unit-of-work implementation, aggregate assembler, generic persistence framework, navigation subsystem, Domain service, or new business state was introduced.
- Electronic signatures retain only the accepted BatchStepExecution target behavior and no polymorphic FK.
- The model contains exactly the approved 15 tables.

## 12. Known Issues and Tasks 6–8 Prerequisites

- No live SQL Server constraint, Unicode, migration, or round-trip validation was executed in Batch A.
- Rehydration factories are implemented and compile under the friend grant, but repository assembler round trips are awaiting Batch B authorization.
- Stable Infrastructure surrogates, cross-row correlation, same-Batch signature validation during loading, append-only persistence operations, and database-only optional-field preservation require the future repository/assembler implementation.
- Actual connection configuration must be supplied outside tracked source before authorized database operations.
- The pre-existing repository-wide formatting findings outside the scoped Batch A source set remain unchanged.

Tasks 6–8 should begin only after Team acceptance and explicit Batch B authorization.

## 13. Git Branch HEAD and Working-Tree Status

| Item | Final value |
|---|---|
| Branch | `feature/wp6-sprint3-core-domain` |
| HEAD | `62b615e1068e65de36f1ec10e5949de1eedd4a88` |
| HEAD changed | No |
| Working tree | Intentionally contains preserved Task 3 and Batch A changes |
| `git diff --check` | Exit 0 |

No commit, push, merge, rebase, branch change, reset, stash, clean, tag, migration, or database operation was performed.

## 14. Final Result

**READY FOR TEAM REVIEW.**

Batch A meets the authorized implementation and evidence scope. The next action is Team review and acceptance, followed by separate Batch B authorization. Batch B has not begun.
