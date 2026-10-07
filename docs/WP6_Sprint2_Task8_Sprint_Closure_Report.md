# WP-6 Sprint 2 Task 8 Sprint Closure Report

## Outcome

Sprint 2 is implementation-complete and ready for Team review and Sprint 3 handoff. The Domain now contains the approved manufacturing behavior and the minimal reference types required by the DBDS. The Application layer exposes only the repository, actor-context, and atomic-commit contracts needed by later use cases. No persistence implementation, application service, authentication implementation, reporting contract, UI, or API was added.

## Golden Documents Read

Before implementation, these authoritative documents were opened and reviewed:

- `C:\Engineering\Golden_Documents\SyncBridge_Core_SRS_Corrected.docx`
- `C:\Engineering\Golden_Documents\SyncBridge_Core_SDD_Corrected.docx`
- `C:\Engineering\Golden_Documents\Indy-1-SyncBridge-DBDS_v2.docx`
- `C:\Engineering\Golden_Documents\WP-6_Implementation_Execution_Plan_Realigned.docx`

## Source Sections Used

- SRS §§3.2–3.10, 3.13, 4, 5.3, 6.1–6.6, and 7.1–7.2: FR-002–FR-010, BR-001–BR-006/BR-008, NFR-003/NFR-005, actor attribution, lifecycle behavior, and acceptance.
- SDD §§3.2–3.3, 4.1–4.5, 5.1–5.4, 6, 8, and 9: inward dependencies, ADR-006–ADR-009, rich Domain behavior, Application-owned contracts, Infrastructure implementations, and traceability.
- DBDS §§2–7, 9–10, and Appendix A: the 15-table persistence contract, keys, relationships, optional semantics, transaction boundary, repository loading, and implementation order.
- WP-6 §§2–3, 4.2–4.3, 5.1–5.2, 6–8: Sprint 2 closure criteria, Sprint 3 inputs, use-case-oriented repository guidance, actor context, proportional patterns, and ADR allocation.

## Closure Mismatches and Resolution

1. `ProductionOrder` lacked the DBDS §5.8 order identity, Recipe association, quantity, product-name terminology, and creation timestamp. It also retained planned dates with no approved DBDS field. The type was realigned to the approved persistence-facing domain shape while retaining its accepted lifecycle behavior.
2. Material and Equipment existed only as codes on requirements and verification records. SDD §5.1 and DBDS §§5.6–5.7 require reference concepts, so minimal technology-independent `Material` and `Equipment` entities plus focused identities were added.
3. Application lacked the repository, actor-context, and commit contracts required by WP-6 §4.2 and ADR-008/ADR-009. Minimal use-case-oriented contracts were added in Application.
4. Generated `Class1.cs` files in Application and Infrastructure had no behavior or architectural purpose and were removed.
5. Batch does not directly retain DBDS timestamps or final disposition. Task 8 does not invent new behavior: creation/start/completion timestamps are reconstructable from immutable audit evidence, while optional `FinalDisposition` remains null until an approved later use case supplies it.

## Files Changed

### Added

- `src/SyncBridge.Core.Application/Contracts/Identity/IActorContext.cs`
- `src/SyncBridge.Core.Application/Contracts/Persistence/IApplicationUnitOfWork.cs`
- `src/SyncBridge.Core.Application/Contracts/Persistence/IBatchRepository.cs`
- `src/SyncBridge.Core.Application/Contracts/Persistence/IProductionOrderRepository.cs`
- `src/SyncBridge.Core.Application/Contracts/Persistence/IRecipeRepository.cs`
- `src/SyncBridge.Core.Domain/Entities/Material.cs`
- `src/SyncBridge.Core.Domain/Entities/Equipment.cs`
- `src/SyncBridge.Core.Domain/ValueObjects/ProductionOrderId.cs`
- `src/SyncBridge.Core.Domain/ValueObjects/MaterialId.cs`
- `src/SyncBridge.Core.Domain/ValueObjects/EquipmentId.cs`
- `tests/SyncBridge.Core.Tests/SprintClosureModelTests.cs`
- `docs/WP6_Sprint2_Task8_Sprint_Closure_Report.md`
- `docs/WP6_Sprint2_to_Sprint3_Handoff.md`
- `docs/testing/Sprint2_Final_Evidence_Index.md`
- `artifacts/test-evidence/sprint-2/task-8-sprint-closure/evidence.md`
- `artifacts/test-evidence/sprint-2/task-8-sprint-closure/build-initial-sandbox-failed.log`
- `artifacts/test-evidence/sprint-2/task-8-sprint-closure/build.log`
- `artifacts/test-evidence/sprint-2/task-8-sprint-closure/test-results.trx`

### Modified

- `src/SyncBridge.Core.Domain/Entities/ProductionOrder.cs`
- `docs/testing/Test_Evidence_Register.md`

### Removed

- `src/SyncBridge.Core.Application/Class1.cs`
- `src/SyncBridge.Core.Infrastructure/Class1.cs`

## Contracts Added and Placement Rationale

All contracts are in Application because SDD §§4.3, 5.2, and 5.4 require application use cases to depend on abstractions that Infrastructure later implements.

| Contract | Purpose | Why this boundary |
|---|---|---|
| `IProductionOrderRepository` | Retrieve by number, test existence, and add an order | Supports FR-002 use cases without table-oriented CRUD or infrastructure knowledge |
| `IRecipeRepository` | Retrieve Recipe by identity or business code | Supports order/batch orchestration and approved Recipe selection |
| `IBatchRepository` | Add Batch and load the execution aggregate with required evidence | Expresses use-case loading instead of one repository per persistence table |
| `IApplicationUnitOfWork` | Atomically commit one controlled command | Prepares DBDS §7.2 one-DbContext command boundary without implementing transactions |
| `IActorContext` | Supply the trusted current `OperatorId` | Enables later application services to attribute actions without authentication or claims dependencies |

No Domain repository interface was added because repositories coordinate Application use cases rather than Domain invariants. No generic repository, repository implementation, reporting contract, service contract, role/permission contract, or current-user singleton was added. Reporting remains Sprint 7 and application services remain Sprint 4.

The final repository-contract set is `IProductionOrderRepository`, `IRecipeRepository`, and `IBatchRepository`, with `IApplicationUnitOfWork` providing the atomic commit abstraction. The final actor-context contract is `IActorContext`. No additional service or reporting contract is justified in Sprint 2.

## Final Domain to DBDS Mapping

| DBDS object | Sprint 2 type / identity | Major mapping and Sprint 3 obligation |
|---|---|---|
| AppUser | No Domain entity; `OperatorId`; Application `IActorContext` | Persist `OperatorId.Value` as `UserId`; identity infrastructure supplies UserName, DisplayName, IsActive and roles outside manufacturing Domain |
| Recipe | `Recipe`; `RecipeId`, `RecipeCode` | Map Name, Version, IsApproved and owned definitions; convert the RecipeId value to `uniqueidentifier`; enforce unique RecipeCode |
| RecipeStep | `RecipeStep`; `RecipeStepId` | Map Recipe relation, StepNumber, Name, Instructions, RequiresSignature; enforce unique `(RecipeId, StepNumber)` |
| RecipeMaterialRequirement | Composite RecipeId + MaterialCode | Resolve MaterialCode to MaterialId; persist composite `(RecipeId, MaterialId)` and optional RequiredQuantity |
| RecipeEquipmentRequirement | Composite RecipeId + EquipmentCode | Resolve EquipmentCode to EquipmentId; persist composite `(RecipeId, EquipmentId)` |
| Material | `Material`; `MaterialId`, `MaterialCode` | Map Id, unique code, and Name as reference data |
| Equipment | `Equipment`; `EquipmentId`, `EquipmentCode` | Map Id, unique code, Name, and nullable CalibrationDueDate |
| ProductionOrder | `ProductionOrder`; `ProductionOrderId`, `ProductionOrderNumber` | Directly map product code/name, RecipeId, Quantity, status, CreatedAt; preserve unique order number |
| Batch | `Batch`; business identity `BatchNumber` | Infrastructure supplies `BatchId`; resolve ProductionOrderNumber to OrderId; map five-state status; RecipeCode and planned quantity/UOM remain useful Domain context, not Batch-table columns |
| BatchStepExecution | `BatchStepExecution`; `BatchStepExecutionId` | Resolve BatchNumber and RecipeStepId to FKs; map step number/status/timestamps/completing actor; `RecordedData` remains null until approved behavior exists |
| MaterialVerification | Composite BatchNumber + MaterialCode + OperatorId + VerifiedAt | Sprint 3 generates VerificationId, resolves BatchId/MaterialId/UserId, maps Passed/Failed to IsAccepted, leaves LotNumber/Comments null when absent, and enforces unique `(BatchId, MaterialId)` |
| EquipmentVerification | Composite BatchNumber + EquipmentCode + OperatorId + VerifiedAt | Sprint 3 generates VerificationId, resolves FKs, maps result to IsReady, leaves optional calibration evidence/comments null when absent, and enforces unique `(BatchId, EquipmentId)` |
| Deviation | `Deviation`; `DeviationCode` | Persistence requires a surrogate DeviationId mapping while retaining the Domain code; map immutable Batch origin, optional step, lifecycle fields, actor, times, review and disposition |
| ElectronicSignature | `ElectronicSignature`; `ElectronicSignatureId` | Convert/generate DB GUID representation, resolve Batch/User, map generic related record fields and enforce immutable rows |
| AuditEvent | `AuditEvent`; `AuditEventId` | Parse the generated GUID-format identity, resolve optional Batch/User, map action string, affected type/ID, time, state and details; enforce append-only application path |

Domain string identifier value objects remain technology-independent. Sprint 3 must use reviewed converters or Infrastructure surrogate mappings without changing business identity semantics.

## AuditEventType Mapping Decision

Each current `AuditEventType` name maps faithfully to DBDS `AuditEvent.Action` using its stable enum name: `BatchCreated`, `BatchMarkedReady`, `BatchProcessingStarted`, `MaterialVerified`, `EquipmentVerified`, `StepStarted`, `StepCompleted`, `ElectronicSignatureApplied`, `DeviationOpened`, `DeviationReviewStarted`, `DeviationApproved`, `DeviationRejected`, `DeviationResolved`, `BatchExceptionEntered`, `BatchProcessingResumed`, and `BatchCompleted`. Sprint 3 must store these names as `nvarchar(100)` and must not convert them into database workflow logic.

## Verification Identity Mapping Decision

The accepted Domain model intentionally has no dedicated verification ID. Material verification identity is represented by BatchNumber, MaterialCode, OperatorId, and VerifiedAt; equipment uses BatchNumber, EquipmentCode, OperatorId, and VerifiedAt. DBDS requires a `VerificationId uniqueidentifier` plus unique current-record constraints. Sprint 3 must generate the surrogate persistence key when first storing a record, resolve business codes to reference FKs, and preserve one current record per Batch/item. This does not authorize an attempt-history subsystem or change Task 3 gates.

## Batch Reconciliation

| DBDS field | Sprint 2 source / decision |
|---|---|
| BatchId | Infrastructure-managed surrogate; Domain business identity remains BatchNumber |
| BatchNumber | `Batch.BatchNumber.Value` |
| OrderId | Resolve `Batch.ProductionOrderNumber` through ProductionOrder |
| Status | Convert the five-value BatchStatus; persist `InProgress` as DBDS `In Progress` |
| CreatedAt | OccurredAt of the immutable `BatchCreated` audit event |
| StartedAt | OccurredAt of `BatchProcessingStarted`; nullable before execution |
| CompletedAt | OccurredAt of `BatchCompleted`; nullable before completion |
| FinalDisposition | Null in current Sprint 2 scope; no approved Batch-level disposition operation exists |

## ProductionOrder Reconciliation

ProductionOrder now separates `ProductionOrderId` from unique `ProductionOrderNumber` and contains ProductCode, ProductName, RecipeId, Quantity, status, and CreatedAt exactly as required by DBDS §5.8. Existing Planned → Released → InProcess → Completed and cancellation behavior is retained. Order management application services, authorization, batch collection navigation, and persistence remain deferred.

## Material and Equipment Reconciliation

`Material` contains MaterialId, MaterialCode, and Name. `Equipment` contains EquipmentId, EquipmentCode, Name, and nullable CalibrationDueDate. Recipe requirements and verification evidence continue to use business codes; Sprint 3 repositories resolve codes to reference IDs. No inventory, lot, maintenance, calibration policy, or equipment-control behavior was added.

## Sprint 2 Traceability Matrix

| Source | Implementation artifacts | Test evidence | Status | Notes |
|---|---|---|---|---|
| FR-002 | ProductionOrder, Recipe, IProductionOrderRepository, IRecipeRepository | S2-T8-001–005; structural Task 2 evidence | Partially implemented by Sprint boundary | Domain and contracts complete; secured Application operations/persistence are later sprints |
| FR-003 | Batch lifecycle and completion gates | S2-T1-001–015; S2-T7-012–015 | Implemented | Persistence/UI deferred as planned |
| FR-004 | RecipeStep, BatchStepExecution, ordered Batch operations | S2-T2-012–017; S2-T4-001–024; S2-T7-008/013 | Implemented | Guided UI deferred |
| FR-005 | Material, requirements, MaterialVerification, gate | S2-T3-002–005/010/012/016–019; S2-T8-006–007 | Implemented | Durable evidence deferred |
| FR-006 | Equipment, requirements, EquipmentVerification, gate | S2-T3-006–010/011/020–023; S2-T8-008–009 | Implemented | Durable evidence deferred |
| FR-007 | ElectronicSignature and step gate | S2-T5-001–021; S2-T7-009/013 | Implemented | Authentication and persistence deferred |
| FR-008 | AuditEvent and controlled-action generation | S2-T7-001–012/015 | Implemented | Append-only persistence deferred |
| FR-009 | Deviation lifecycle/origin and Batch Exception integration | S2-T6-001–030; S2-T7-010/014 | Implemented | Application review workflow deferred |
| FR-010 | Blocking status and unresolved/failed evidence semantics | S2-T3 negative gates; S2-T6-022–030; S2-T7-011/014 | Partially implemented by Sprint boundary | Dashboard/query/report presentation deferred |
| BR-001 | Recipe material gate | S2-T3-001–005/010/012–015 | Implemented | — |
| BR-002 | Recipe equipment gate | S2-T3-001/006–011/013–015 | Implemented | — |
| BR-003 | Ordered step execution | S2-T4-007–024 | Implemented | — |
| BR-004 | Required signature gate | S2-T5-008–021 | Implemented | — |
| BR-005 | Significant-action audit evidence | S2-T7-005–012 | Implemented | Durable atomic persistence deferred |
| BR-006 | Permanent deviation Batch origin | S2-T6-001–006/015–017/024 | Implemented | — |
| BR-008 | Terminal Completed protection | S2-T7-012–015 | Implemented | No correction/reopen workflow exists |
| NFR-003 | Domain validation and failure atomicity | S2-T3/4/5/6/7 negative cases; S2-T8-002–004/007 | Partially implemented by Sprint boundary | Database transactions/rollback are Sprint 3 |
| NFR-005 | Four-layer boundaries and focused contracts | Full build; Task reports; contract compilation | Implemented | Continued enforcement required |
| ADR-006 | Rich Domain Model | Domain entities and behaviors | S2-T1–T8 domain evidence | Implemented | — |
| ADR-007 | Explicit state machine | Batch and step transition guards | S2-T1, T4, T6, T7 | Implemented | — |
| ADR-008 | Repository Pattern | Application repository contracts | Full build | Partially implemented by Sprint boundary | Infrastructure implementations are Sprint 3 |
| ADR-009 | Dependency Injection | Inner-layer interfaces ready for composition | Full build | Partially implemented by Sprint boundary | Registration and implementations are later work |

FR-001, FR-011, and FR-012 are not claimed as Sprint 2 implementations.

## Sprint 2 Definition of Done Review

| Check | Result | Evidence |
|---|---|---|
| Missing/failed prerequisites rejected | PASS | S2-T3-002–010 |
| Skipped steps rejected | PASS | S2-T4-016–018 |
| Missing signatures rejected | PASS | S2-T5-008 and related negative cases |
| Invalid state changes rejected | PASS | S2-T1-007–012/015 |
| Successful transitions match SRS §6.6 | PASS | S2-T1-002–006/013–014; S2-T6-023/026 |
| Deviation origin preserved | PASS | S2-T6-001–006/015–017 |
| Signature association Batch-consistent | PASS | S2-T5 wrong-Batch/record tests |
| Completed cannot resume normal operations | PASS | S2-T7-015 |
| Controlled actions produce audit evidence | PASS | S2-T7-005–012 |
| Domain free of EF Core/SQL/UI | PASS | project/reference scan and independent Domain build |
| Persistence remains pending | PASS | no DbContext, mapping, migration, repository implementation, SQL, or persistence package exists |

## Tests and Evidence

Task 8 adds nine focused tests, registered as `S2-T8-001` through `S2-T8-009`. The final full-suite result is 159 passed, 0 failed, 0 skipped. All 150 accepted Task 1–7 tests pass.

## Validation Results

- Full solution build: succeeded with 0 errors and 0 warnings.
- The first log-capture attempt was blocked by PowerShell sandbox drive initialization before compilation; `build-initial-sandbox-failed.log` is retained, and the unrestricted authoritative rerun succeeded.
- Domain project build: succeeded independently.
- Full test suite: 159 passed, 0 failed, 0 skipped.
- Task 8 tests: 9/9 passed.
- `git diff --check`: passed.
- No EF Core or SQL Server package/reference, DbContext, mapping, migration, repository implementation, application service, UI/API implementation, authentication, authorization, or SSRS implementation was introduced.

## Remaining Sprint 3 Obligations

Sprint 3 must implement the exact 15-table DBDS schema, Infrastructure repository/commit implementations, SyncBridgeDbContext, Fluent API mappings, converters/surrogate-key decisions, restricted deletes, unique constraints/indexes, migrations, fictional reference seed data, and integration/rollback tests. It must preserve Domain rules, audit append-only semantics, signature immutability, the five Batch states, and one scoped atomic command boundary.

## Unresolved Decisions

The Team must review the Infrastructure mapping approach for Domain string identifiers whose DBDS columns are `uniqueidentifier`, especially Batch/Deviation surrogate identities and Recipe/step/signature conversions. This is a Sprint 3 implementation decision within the approved schema, not a Sprint 2 behavior change.

**Acceptance status:** Pending AI-Agent / Team review
