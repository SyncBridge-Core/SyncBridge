# Sprint 3 Batch C Integrated Verification Report

## 1. Objective and scope

Batch C verified the Sprint 3 SQL Server and EF Core persistence increment against the approved Sprint 3 design decisions, corrected SRS and SDD, amended DBDS, WP-6 execution plan §4.3, and accepted Sprint 2 Domain baseline. Verification was limited to persistence, reconstruction, integrity, transaction behavior, reproducibility, and architecture. It does not claim completion of secured workflows, REST services, UI, or Electronic Batch Record reporting.

Final recommendation: **READY FOR TEAM ACCEPTANCE**. This is an engineering recommendation, not Team acceptance.

## 2. Accepted baseline and repository state

- Branch: `feature/wp6-sprint3-core-domain`
- HEAD before and after verification: `62b615e1068e65de36f1ec10e5949de1eedd4a88`
- The pre-existing uncommitted Task 3 and Batch A/B implementation was preserved.
- No commit, push, merge, rebase, reset, stash, clean, or branch switch was performed.
- EF Core SQL Server and Design packages remain at `10.0.12`.
- Accepted migration: `20261013010705_InitialCreate`.

## 3. Actual database and schema verification

The verified target was `SyncBridgeCore` on `localhost` using Windows authentication and a process-local connection setting. A read-only `DB_NAME()` check returned `SyncBridgeCore`. The legacy `SyncBridge_Core` database was not connected to, inspected, enumerated, or modified.

The deployed schema contains exactly the 15 approved `dbo` tables: AppUser, Recipe, RecipeStep, RecipeMaterialRequirement, RecipeEquipmentRequirement, Material, Equipment, ProductionOrder, Batch, BatchStepExecution, MaterialVerification, EquipmentVerification, Deviation, ElectronicSignature, and AuditEvent. The database has 23 foreign keys and all 23 use restricted/no-action deletion. No user trigger, stored procedure, or view exists. Migration history contains exactly `20261013010705_InitialCreate`.

Column types, nullability, primary keys, composite keys, unique indexes, supporting indexes, and foreign keys were reconciled against the amended DBDS and migration/model metadata. The deployed Batch status mapping is the approved five-state representation: Created, Ready, In Progress, Exception, and Completed.

## 4. SQL Server constraint results

| Verification | Result | Evidence |
|---|---|---|
| Primary-key uniqueness | Passed | Duplicate AppUser primary key rejected |
| Composite primary keys | Passed | Duplicate Recipe/material requirement rejected; model and deployed keys inspected |
| Required foreign keys | Passed | Invalid Recipe FK in an atomic command rejected |
| Restricted deletes | Passed | Referenced Recipe deletion rejected; all 23 deployed FKs are NO ACTION |
| Unique business identifiers | Passed | RecipeCode, MaterialCode, EquipmentCode, OrderNumber, BatchNumber, and DeviationCode duplicates rejected |
| Recipe step sequence | Passed | Duplicate Recipe/StepNumber rejected |
| Material verification cardinality | Passed | Duplicate Batch/material rejected |
| Equipment verification cardinality | Passed | Duplicate Batch/equipment rejected |
| Batch step sequence | Passed | Duplicate Batch/StepNumber rejected |
| Required/null fields | Passed | Required Material.Name null rejected |
| Decimal precision/scale | Passed | `decimal(18,3)` overflow rejected; 1.2346 stored/reconstructed as 1.235 |
| Date/time persistence | Passed | `date` and `datetimeoffset(7)` values round-tripped exactly |
| Invalid enum/technical IDs | Passed | Closed enum converters and canonical GUID boundary tests reject unsupported inputs |
| Field lengths | Passed | Overlength MaterialCode and 257-unit AuditEvent identifier rejected without truncation |
| Unauthorized schema objects | Passed | Zero triggers, procedures, and views |

Relational constraints remain in Infrastructure/SQL Server. No manufacturing workflow rule was moved into a trigger or stored procedure.

## 5. Domain-to-database round-trip matrix

| Type | Verified outcome |
|---|---|
| Recipe | Identity, code, version, approval, ordered steps, material requirement, and equipment requirement reconstructed |
| ProductionOrder | Stable technical/business identities, quantity, timestamp, Recipe association, and all five statuses reconstructed |
| Batch | All five approved states reconstructed without command replay or added evidence |
| BatchStepExecution | Pending/In Progress/Completed shapes covered by Domain/model tests; completed SQL evidence reconstructed |
| MaterialVerification | Material, actor, timestamp, result, lot, comments, and audit evidence reconstructed |
| EquipmentVerification | Equipment, actor, timestamp, result, calibration value, comments, and audit evidence reconstructed |
| Deviation | Stable surrogate and code, lifecycle states, audit ownership, optional review/final disposition, and resolution shape covered; Open/Closed live SQL aggregates reconstructed |
| ElectronicSignature | Stable signature identity, actor, meaning, timestamp, same-Batch step target, and canonical audit evidence reconstructed |
| AuditEvent | Stable identity, actor, action, entity, affected identity, timestamp, resulting state, and details reconstructed without duplication |
| Material/Equipment/AppUser | Reference associations and actor references verified through repository aggregates and live FKs |

Repository loads did not replay Domain commands. Audit/signature counts remained unchanged during reconstruction, and Infrastructure surrogate identifiers remained stable.

## 6. Audit and signature integrity

- `ApplicationUnitOfWork` rejected update and delete tracking states for existing AuditEvent and ElectronicSignature rows.
- Unsupported signature target types and missing/cross-Batch execution targets were rejected during aggregate assembly.
- Signature identity, user, timestamp, meaning, and related execution were preserved.
- Audit identities were deduplicated by identity only; distinct legitimate events remained distinct.
- Deviation-owned events and Batch-direct evidence were reconstructed once per persisted identity.
- Ordinary updates to a persisted Completed Batch were rejected.
- Completed remains terminal under normal Domain operations. No correction/reopening workflow was added.

## 7. Transaction and rollback results

The scoped DbContext/unit-of-work boundary passed a successful multi-record commit inside a rollback-scoped fixture. A command containing a valid AppUser and a ProductionOrder with a missing Recipe FK failed atomically; a new DbContext confirmed the AppUser marker was absent. Duplicate unique identifiers and invalid Batch/evidence combinations were rejected, transaction rollbacks left no partial evidence, and failed tracked commands could not become a later accidental success. No distributed transaction framework or new command layer was introduced.

## 8. DBDS amendments and Unicode

DBDS-AMD-001 passed: DeviationCode is required, unique, Unicode-capable, and independent of stable DeviationId; no extra table exists.

DBDS-AMD-002 passed: AuditEvent.AffectedEntityId is deployed as `nvarchar(256) NOT NULL`. A 180-UTF-16-unit identifier containing BMP and supplementary characters round-tripped unchanged. Values over 256 UTF-16 units were rejected without truncation. Domain audit serialization was unchanged.

## 9. Seed idempotency and reproducibility

The deterministic development seed contains 11 fictional rows: 2 AppUsers, 1 Material, 1 Equipment, 1 Recipe, 2 RecipeSteps, 1 material requirement, 1 equipment requirement, and 2 ProductionOrders. Two successive seed calls produced no duplicate rows; the second returned zero writes. No Batch, BatchStepExecution, verification, Deviation, ElectronicSignature, or AuditEvent row was seeded. Existing manufacturing data is not overwritten.

The migration source and model snapshot are present in the working tree, and setup commands are documented by the prior Batch B reports. Clean database recreation was not reexecuted because the accepted database could not be destructively reset; migration/source review and the accepted successful deployment provide reproducibility evidence.

## 10. Optional-field preservation

MaterialVerification.LotNumber and Comments, EquipmentVerification.CalibrationValid and Comments, BatchStepExecution.RecordedData, and Batch.FinalDisposition survived repository load unchanged. Load-only operations did not write back or overwrite database-only values. Secured update contracts remain deferred to Sprint 4 Application services.

## 11. Performance baseline

Representative rollback-scoped fixture: 1 Batch, 1 step execution, 1 material verification, 1 equipment verification, 1 signature, and 4 direct audit events, plus the deterministic reference dataset.

| Operation | Elapsed |
|---|---:|
| Controlled multi-record SaveChanges | 3.952 ms |
| ProductionOrder lookup | 1.390 ms |
| Recipe aggregate retrieval | 14.215 ms |
| Batch aggregate retrieval with evidence | 16.222 ms |

These bounded local measurements establish a Sprint 3 baseline only. They do not prove NFR-002 at production capacity and do not measure EBR generation.

## 12. Architectural review

- Domain and Application contain no EF Core, SQL Server, DbContext, or provider references.
- Infrastructure owns DbContext, configurations, migrations, row models, converters, assemblers, repositories, seeding, and unit of work.
- Repository and unit-of-work contracts remain Application-owned.
- Presentation contains no manufacturing persistence logic.
- No generic repository, new architectural layer, Domain workflow, Application service, UI, API, or report generator was introduced.
- Clean Architecture dependency boundaries remain intact.

## 13. Defects discovered and corrections made

No production defect was found. Four focused SQL integration tests were added to close evidence gaps for remaining uniqueness/composite constraints, scalar/date/status round trips, live immutable-evidence and Completed-Batch guards, and bounded performance measurements. During verification, one test-fixture-only string slice exceeded its generated identifier length; it was corrected before the final passing run. No schema, production mapping, repository, Domain, or migration correction was required.

## 14. Requirements and design traceability

| Requirement/design item | Sprint 3 status | Evidence/qualification |
|---|---|---|
| FR-001 | Deferred | Authentication/authorization is later-sprint scope; AppUser persistence foundation only |
| FR-002 | Persistence support verified | ProductionOrder repository, all statuses, recipe FK, uniqueness, and round trip; secured management deferred |
| FR-003 | Domain/persistence verified | Five-state Batch lifecycle, repository persistence, reconstruction, and terminal protection |
| FR-004 | Domain/persistence support verified | Ordered steps and prerequisite/evidence model persist; guided UI/application orchestration deferred |
| FR-005 | Domain/persistence verified | Material verification, actor, timestamp, result, optional DB fields, uniqueness, and evidence |
| FR-006 | Domain/persistence verified | Equipment verification, actor, timestamp, result, optional DB fields, uniqueness, and evidence |
| FR-007 | Persistence integrity verified | Immutable signatures and same-Batch step association; authentication ceremony deferred |
| FR-008 | Persistence integrity verified | Append-only audit identity, actor, action, entity, timestamp, state, and canonical reconstruction |
| FR-009 | Domain/persistence verified | Deviation code, Batch ownership, lifecycle shape, actor/time, audit evidence, and disposition |
| FR-010 | Persistence support only | Status/index foundation exists; review-by-exception queries/UI deferred |
| FR-011 | Persistence support only | Final data can be read; EBR generation/reporting deferred |
| FR-012 | Persistence support only | Shared repository foundation can support later REST services; no API implemented |
| NFR-002 | Partially verified | Local bounded timings recorded; production capacity and EBR target deferred |
| NFR-003 | Verified for Sprint 3 | Constraints, restricted FKs, atomic rollback, immutable evidence, and residue checks passed |
| NFR-005 | Verified for Sprint 3 | Layer boundaries, Application contracts, Infrastructure ownership, DI registrations, and regression tests |
| BR-001/BR-002 | Domain/persistence verified | Material then equipment prerequisites and durable evidence |
| BR-003 | Domain/persistence verified | Ordered step numbers and execution-state model |
| BR-004 | Domain/persistence verified | Required step signature gate and immutable related evidence |
| BR-005 | Domain/persistence verified | Significant lifecycle/evidence actions retain canonical audit events |
| BR-006 | Domain/persistence verified | Deviations remain associated with originating Batch/step |
| BR-007 | Persistence support only | Final state is durable; EBR reporting is future scope |
| BR-008 | Verified for ordinary path | Completed record and evidence mutation guards; formal correction workflow deferred |
| ADR-001 | Verified | Approved Clean Architecture dependency direction retained |
| ADR-003/ADR-004 | Verified | EF Core 10.0.12 and SQL Server persistence implemented |
| ADR-006/ADR-007 | Verified | Rich Domain behavior and explicit Batch state machine preserved |
| ADR-008 | Verified | Specific Application-owned repositories implemented in Infrastructure |
| ADR-009 | Verified | Scoped DbContext, repositories, seeder, and unit of work registered through DI |
| DBDS 15 tables | Verified | Exact deployed table set, keys, indexes, relationships, types, and nullability reconciled |
| DBDS-AMD-001/002 | Verified | Live SQL and boundary evidence passed |

## 15. Automated validation

- `dotnet restore src/SyncBridge.Core.slnx`: exit 0; all projects up to date.
- `dotnet build src/SyncBridge.Core.slnx --no-restore --verbosity minimal`: exit 0; 0 errors, 0 warnings.
- Focused SQL Server suite: 10 passed, 0 failed, 0 skipped.
- Full solution regression: 201 passed, 0 failed, 0 skipped.
- `git diff --check`: exit 0.
- Final database residue: all eight manufacturing-history tables contain zero rows; the 11 approved seed rows remain intact.

## 16. Outstanding risks and deferred functionality

- Authentication/authorization, secured Application commands, REST APIs, UI workflows, review-by-exception, and EBR generation remain Sprint 4+ work.
- Clean-environment database recreation was not rerun against the accepted database.
- Performance figures are local development baselines, not load/capacity qualification.
- Database constraints intentionally do not encode all manufacturing workflow rules; those remain Domain responsibilities.

## 17. Recommendation

No blocking Sprint 3 persistence defect remains. Batch C and the consolidated Sprint 3 persistence increment are **READY FOR TEAM ACCEPTANCE**.
