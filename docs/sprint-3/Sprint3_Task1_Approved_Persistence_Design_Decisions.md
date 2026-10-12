# WP-6 Sprint 3 Task 1 Approved Persistence Design Decisions

## 1. Purpose and Engineering Authority

This record consolidates the five Team-approved Sprint 3 Task 1 persistence decisions into the implementation authority for subsequent Sprint 3 persistence work. It is subordinate to the approved SRS, SDD, amended DBDS, and realigned WP-6 execution plan. It resolves the open questions recorded by the original Task 1 design gate without rewriting that historical evidence.

This document authorizes no code, package, migration, SQL Server, or database change. Sprint 3 Task 2 requires explicit Team authorization.

Authority order:

1. Approved SRS for required behavior.
2. Approved SDD for architecture and responsibility boundaries.
3. Amended DBDS for persistence structure.
4. Realigned WP-6 plan and Team-reviewed handoff for execution sequencing.
5. This approved-decision consolidation for the bounded Task 1 implementation choices.

Historical investigation reports retain the status that was correct when written. The current Team-approved decisions in this record supersede their provisional/open status only; their evidence remains unchanged.

## 2. Approved Sprint 2 Starting Baseline

- Branch at closure analysis: `feature/wp6-sprint3-core-domain`.
- HEAD: `1be2fbee70d94be8ae21b5b3128fe71610ca00b3` (`WP-6 Sprint 2: complete domain model and manufacturing rules`).
- Historical accepted regression baseline: 159 passing tests.
- Current architecture: Presentation → Application → Domain, with Infrastructure implementing inner-layer contracts.
- Current Domain behavior and Application repository/actor/unit-of-work contracts are accepted.
- No EF Core/SQL Server package or persistence implementation exists in the current source.
- Approved persistence inventory: exactly 15 core tables.

Sources: `src/SyncBridge.Core.Domain`, `src/SyncBridge.Core.Application/Contracts`, project files under `src`, `docs/WP6_Sprint2_Task8_Sprint_Closure_Report.md`, and `docs/testing/Sprint2_Final_Evidence_Index.md`.

## 3. Source and Document Register

| Source | Actual location | Authority/use |
|---|---|---|
| Corrected SRS | `C:\Engineering\Golden_Documents\SyncBridge_Core_SRS_Corrected.docx` | FR-002–FR-011, BR-001–BR-008, immutable evidence, transactional consistency |
| Corrected SDD | `C:\Engineering\Golden_Documents\SyncBridge_Core_SDD_Corrected.docx` | §§4.4–4.5 and 5.1–5.4 layer responsibilities and persistence boundary |
| Amended DBDS | `C:\Engineering\Golden_Documents\Indy-1-SyncBridge-DBDS_v2.docx` | §§5–7, Appendix A, Appendix B / DBDS-AMD-001, Appendix C / DBDS-AMD-002 |
| Realigned WP-6 plan | `C:\Engineering\Golden_Documents\WP-6_Implementation_Execution_Plan_Realigned.docx` | §4.3 implementation sequence, exact schema, explicit/eager loading, round trips |
| Team-reviewed handoff | `docs/WP6_Sprint2_to_Sprint3_Engineering_Handoff_Team_Reviewed.docx` | Final Domain-to-DBDS mapping, explicit loading, evidence/timestamp obligations |
| Accepted source | `src/SyncBridge.Core.Domain`; `src/SyncBridge.Core.Application/Contracts` | Actual implemented behavior and current interfaces |
| Original design gate | `docs/sprint-3/Sprint3_Task1_Persistence_Mapping_Design_Gate.md` | Initial mapping inventory and OQ register |
| OQ-1 evidence | `docs/sprint-3/Sprint3_Task1_OQ1_Impact_Analysis.md` and completion report | Quantity/UOM alternatives and file/test impact |
| OQ-3 evidence | `docs/sprint-3/Sprint3_Task1_OQ3_Compatibility_Analysis.md` and completion report | Identifier inventory, signature mapping, audit identity analysis |
| OQ-4 evidence | `docs/sprint-3/Sprint3_Task1_OQ4_Rehydration_Impact_Analysis.md` and completion report | Constructor side effects, factory/assembler design, OQ-3A calculation |

## 4. Approved Decision Register

### 4.1 S3-T1-OQ1 Batch quantity and UnitOfMeasure

**Status: APPROVED**

- ProductionOrder.Quantity is the sole authoritative planned quantity.
- Every Batch executes the full associated ProductionOrder quantity; no allocation among Batches is introduced.
- Batch.PlannedQuantity remains immutable derived Domain state.
- Batch.UnitOfMeasure and `ValueObjects/UnitOfMeasure.cs` are removed during coordinated implementation.
- Batch retains ProductionOrderNumber and gains no ProductionOrder navigation property.
- No Batch quantity or unit column is added.
- Creation and rehydration must copy quantity from the loaded ProductionOrder and prevent arbitrary independent Batch quantity input.
- Existing Batch lifecycle rules are unchanged.

Sources: `Entities/ProductionOrder.cs`, `Entities/Batch.cs`, OQ-1 analysis §§2–9, DBDS §§5.8–5.9, OQ-4 analysis §§5 and 12.

### 4.2 S3-T1-OQ2 deviation technical and business identities

**Status: APPROVED — DBDS-AMD-001**

- DeviationCode remains the human-readable Domain/business identity.
- DeviationId remains the database `uniqueidentifier` primary key and is generated/retained by Infrastructure.
- Existing Deviation table gains `DeviationCode nvarchar(40) NOT NULL` and `UQ_Deviation_DeviationCode`.
- Infrastructure resolves code ↔ stable surrogate reversibly.
- Batch association and optional originating StepExecution association remain unchanged.
- No additional table or deviation behavior is authorized.

Sources: `Entities/Deviation.cs`, `ValueObjects/DeviationCode.cs`, DBDS Appendix B §§B.3–B.9, OQ-3 analysis §§4, 7–8.

### 4.3 S3-T1-OQ3 technical identifier representation

**Status: APPROVED**

- Existing Domain identifier types remain unchanged.
- Persistence-bound technical string IDs must exactly equal `Guid.Parse(value).ToString("D")` using ordinal comparison; this establishes lowercase canonical `D` format.
- Infrastructure strictly validates and reversibly maps them to SQL `uniqueidentifier`.
- Invalid/noncanonical values fail before commit; no hashing, replacement, or silent normalization.
- Native Guid value objects map directly.
- Business numbers/codes and OperatorId remain unchanged strings.
- Pure Domain tests may retain readable non-GUID technical fixtures.
- Version 0.1 supports only `RelatedRecordType.BatchStepExecution`; its RelatedRecordId maps directly to StepExecutionId and must belong to the same Batch.
- AuditEvent.AffectedEntityId retains entity-specific display/traceability semantics.

Sources: identifier value objects, `Entities/ElectronicSignature.cs`, `Entities/Batch.cs`, `Entities/AuditEvent.cs`, OQ-3 analysis §§4–12, DBDS §§5.10, 5.13–5.14.

### 4.4 S3-T1-OQ3A audit identifier length

**Status: APPROVED WITH MANDATORY PERSISTENCE-BOUND VALIDATION — DBDS-AMD-002**

- AuditEvent.AffectedEntityId becomes `nvarchar(256) NOT NULL`.
- Domain serialization is unchanged.
- The longest generated DBDS-valid format is verification evidence:
  `BatchNumber:MaterialOrEquipmentCode:OperatorId:{DateTimeOffset:O}`.
- Maximum: `40 + 1 + 40 + 1 + 64 + 1 + 33 = 180` UTF-16 code units; capacity remaining: 76.
- Infrastructure must validate all component DBDS limits and the final 256-unit capacity before commit.
- No truncation, hashing, replacement, or reformatting.
- Arbitrary strings constructible in the Domain are not claimed to fit persistence.
- Later SQL Server integration tests must cover BMP and supplementary Unicode/surrogate-pair storage.

Sources: `Entities/MaterialVerification.cs`, `Entities/EquipmentVerification.cs`, `Entities/AuditEvent.cs`, OQ-4 analysis §13, DBDS Appendix C §§C.3–C.9.

### 4.5 S3-T1-OQ4 aggregate rehydration

**Status: APPROVED**

- EF Core maps 15 Infrastructure-owned row representations.
- Explicit repository assemblers validate rows and reconstruct Domain graphs.
- ProductionOrder, Batch, BatchStepExecution, MaterialVerification, EquipmentVerification, Deviation, and ElectronicSignature gain entity-specific `internal static Rehydrate(...)` factories backed by private full-state constructors.
- Domain grants `InternalsVisibleTo` only to `SyncBridge.Core.Infrastructure`; it does not reference Infrastructure.
- Normal creation/lifecycle methods are never replayed during loading.
- Loading generates no new evidence, identity, timestamp, deviation, verification, signature, or transition.
- Original state and immutable evidence are preserved exactly.
- Cross-row corruption produces a typed Infrastructure persistence-integrity failure; no silent repair.
- Current Application contracts remain unchanged absent later approved use-case evidence.
- No new public Batch-owned verification/signature collections.
- Reflection, uninitialized objects, replay, and generic persistence frameworks are prohibited.

Safe existing constructors remain the reconstruction route for Recipe, RecipeStep, both Recipe requirement types, Material, Equipment, and AuditEvent.

Sources: Domain constructors/methods in `src/SyncBridge.Core.Domain/Entities`, OQ-4 analysis §§4–10 and 15, SDD §§4.4–4.5, Team-reviewed handoff §§F–L.

## 5. Complete 15-Table Domain-to-DBDS Mapping

The exact columns, SQL types, lengths, nullability, PK/FK/UQ definitions, indexes, and relationships in DBDS §5 and §6 are mandatory. Appendix B amends Deviation; Appendix C amends AuditEvent. All FKs use restricted delete behavior unless the DBDS explicitly states otherwise (DBDS §5 introduction).

| Table | Domain/Application counterpart | Identifier and resolution | Reconstruction responsibility | Decision |
|---|---|---|---|---|
| AppUser | OperatorId and `IActorContext`; no Domain user aggregate | PK `UserId nvarchar(64)`; preserve OperatorId unchanged | Resolve actors for evidence; authentication/roles remain outside manufacturing tables | OQ-3/OQ-3A |
| Recipe | Recipe | `RecipeId uniqueidentifier` ↔ canonical RecipeId string; unique RecipeCode `nvarchar(40)` | Existing constructor; then add validated steps/requirements | OQ-3/OQ-4 |
| RecipeStep | RecipeStep | `RecipeStepId uniqueidentifier`; Recipe FK; unique `(RecipeId, StepNumber)` | Existing constructor; assembler verifies recipe ownership/order | OQ-3/OQ-4 |
| RecipeMaterialRequirement | RecipeMaterialRequirement | Composite PK `(RecipeId, MaterialId)`; resolve MaterialCode through unique Material row | Existing constructor; optional quantity maps `decimal(18,3)` | OQ-3/OQ-4 |
| RecipeEquipmentRequirement | RecipeEquipmentRequirement | Composite PK `(RecipeId, EquipmentId)`; resolve EquipmentCode | Existing constructor | OQ-3/OQ-4 |
| Material | Material | Native MaterialId Guid; unique MaterialCode `nvarchar(40)` | Existing constructor | OQ-3/OQ-4 |
| Equipment | Equipment | Native EquipmentId Guid; unique EquipmentCode `nvarchar(40)` | Existing constructor; nullable calibration date preserved | OQ-3/OQ-4 |
| ProductionOrder | ProductionOrder | Native ProductionOrderId ↔ OrderId; unique OrderNumber; canonical RecipeId FK | Internal Rehydrate restores persisted status without lifecycle replay | OQ-1/OQ-3/OQ-4 |
| Batch | Batch | Infrastructure BatchId surrogate retained by unique BatchNumber; OrderNumber resolves to OrderId | Internal Rehydrate restores derived quantity, recipe code, state, owned collections, and canonical audit history | OQ-1/OQ-4 |
| BatchStepExecution | BatchStepExecution | Canonical StepExecutionId; BatchId/RecipeStepId FKs; unique `(BatchId, StepNumber)` | Internal Rehydrate restores exact state/timestamps/completing actor | OQ-3/OQ-4 |
| MaterialVerification | MaterialVerification | Stable Infrastructure VerificationId; unique `(BatchId, MaterialId)`; codes resolve FKs | Internal Rehydrate attaches original MaterialVerified audit evidence | OQ-3/OQ-3A/OQ-4 |
| EquipmentVerification | EquipmentVerification | Stable Infrastructure VerificationId; unique `(BatchId, EquipmentId)` | Internal Rehydrate attaches original EquipmentVerified evidence | OQ-3/OQ-3A/OQ-4 |
| Deviation | Deviation | DeviationId surrogate plus unique DeviationCode; optional StepExecutionId | Internal Rehydrate restores lifecycle fields, origin, and original event list | OQ-2/OQ-4; AMD-001 |
| ElectronicSignature | ElectronicSignature | Canonical SignatureId; BatchId; canonical BatchStepExecution RelatedRecordId; UserId | Internal Rehydrate preserves immutable signature and original signature audit event | OQ-3/OQ-4 |
| AuditEvent | AuditEvent | Canonical AuditEventId; optional BatchId; UserId; entity-specific AffectedEntityId | Existing public constructor; one canonical object per row/ID | OQ-3/OQ-3A/OQ-4; AMD-002 |

Approved practical indexes remain: order number, Batch number/status, ordered BatchStepExecution, both verification Batch/item paths, Deviation Batch/status, and AuditEvent Batch/time, plus PK/UQ indexes including `UQ_Deviation_DeviationCode` (DBDS §6.2 and Appendix B).

## 6. Identifier, Business-Code, and Surrogate Strategy

### 6.1 Direct/native GUID identities

ProductionOrderId, MaterialId, and EquipmentId are Guid-valued Domain records and map directly to OrderId, MaterialId, and EquipmentId. `Guid.Empty` remains rejected by current entity constructors (source: corresponding entity files).

### 6.2 Technical string identities

RecipeId, RecipeStepId, BatchStepExecutionId, ElectronicSignatureId, AuditEventId, and the supported BatchStepExecution RelatedRecordId map using the strict OQ-3 canonical converter. Database reads use `Guid.ToString("D")`; writes reject values not already equal to that lowercase representation.

### 6.3 Human-readable identities

BatchNumber, ProductionOrderNumber/OrderNumber, DeviationCode, RecipeCode, MaterialCode, EquipmentCode, ProductCode, and OperatorId remain strings. When a DB FK is GUID-valued, Infrastructure resolves through the approved unique business column in the same scoped context.

### 6.4 Infrastructure surrogates

BatchId, both VerificationIds, and DeviationId are generated once and retained. They do not replace Domain identity and do not leak into Domain APIs. A missing/ambiguous business lookup or unstable surrogate is persistence corruption.

## 7. Batch Quantity and Timestamp Reconstruction

- Load Batch with its ProductionOrder and Recipe.
- Set Batch.PlannedQuantity from ProductionOrder.Quantity under OQ-1.
- Set ProductionOrderNumber from OrderNumber and RecipeCode through the order's Recipe relationship.
- Do not add a Batch quantity/UOM column or object navigation.

Authoritative timestamps:

- BatchCreated → Batch.CreatedAt.
- BatchProcessingStarted → Batch.StartedAt.
- BatchCompleted → Batch.CompletedAt.
- BatchProcessingResumed never replaces StartedAt.

Exactly one required authoritative event must exist for each applicable timestamp. Missing, duplicate, or conflicting row/event timestamps fail reconstruction. No current-time fallback is permitted (sources: original design gate §6, OQ-4 §5, Team-reviewed handoff §F).

## 8. Restricted Domain Rehydration Design

The Domain project adds an SDK friend-assembly declaration for `SyncBridge.Core.Infrastructure`; no reverse project reference is added. The seven approved entities use internal factory methods and private full-state constructors. Factories validate local structural shape and defensively copy aggregate collections. Infrastructure assemblers perform cross-row validation before calling them.

Public constructors continue to represent new business creation. Public lifecycle methods continue to decide new transitions. Rehydration restores already-decided historical facts and cannot be called by Presentation/Application.

Recipe and reference objects use current safe constructors. AuditEvent uses its current side-effect-free public constructor with the persisted ID; `AuditEvent.Create` is never called by loading.

## 9. Immutable Audit and Signature Reconstruction

Assemblers create one canonical AuditEvent object for each persisted AuditEventId. They use that instance for verification/signature evidence and aggregate history.

- Batch direct audit collection contains Batch/step events and verification/signature evidence.
- Deviation lifecycle events remain owned by the corresponding Deviation and are concatenated by Batch's existing AuditEvents view.
- Deviation-owned events are not also inserted into Batch's direct collection.
- Distinct valid events are never merged merely because fields are similar.
- Duplicate IDs, conflicting copies, or missing evidence fail loading.

ElectronicSignature remains immutable and supports only BatchStepExecution. The target execution must exist in the same Batch, and its canonical RelatedRecordId must be preserved. Loading never creates a replacement signature audit event.

Sources: `Entities/Batch.cs`, `Entities/Deviation.cs`, `Entities/ElectronicSignature.cs`, `Entities/AuditEvent.cs`, OQ-3 §§7–9, OQ-4 §6.

## 10. Structural Integrity and Failure Semantics

Infrastructure validates:

- required rows/FKs/users/reference records exist;
- enum strings use closed approved mappings;
- identifiers/codes satisfy OQ-2/OQ-3/OQ-3A rules;
- Batch/order/recipe and child Batch associations agree;
- step IDs, numbers, ordering, recipe membership, status/timestamps/actor are coherent;
- verification rows match item, actor, result, time, and audit evidence;
- deviation state matches review/disposition/resolution/origin and evidence;
- signature target and Batch ownership agree;
- audit IDs are unique and authoritative timestamp events are complete; and
- completed state agrees with required completed steps and no blocking deviation.

These are stored-state consistency checks, not replayed manufacturing decisions. Failure raises a typed Infrastructure persistence-integrity exception, returns no partial aggregate, and performs no mutation/repair.

## 11. DbContext, Repository, and Transaction Boundaries

- One scoped SyncBridgeDbContext per controlled command.
- One DbSet/Infrastructure row type for each of the 15 tables.
- Fluent configurations implement the exact amended DBDS.
- Explicit/eager use-case queries; no lazy-loading requirement.
- Existing `IRecipeRepository`, `IProductionOrderRepository`, and `IBatchRepository` remain use-case-specific; no per-table/generic repository.
- Existing `IApplicationUnitOfWork.CommitAsync` is the atomic boundary.
- Related manufacturing state, audit, and signature evidence commit or roll back together.
- Database-only optional values remain in tracked row state and are not overwritten because Domain lacks a property.

Sources: Application contracts, DBDS §§7.1–7.2, SRS transactional requirements, SDD §5.4, WP-6 plan §4.3.

## 12. Approved DBDS Amendments

### DBDS-AMD-001 Appendix B

Adds `DeviationCode nvarchar(40) NOT NULL` and `UQ_Deviation_DeviationCode` to the existing Deviation table. All other Deviation columns/relationships remain. Table count remains 15.

### DBDS-AMD-002 Appendix C

Changes only `AuditEvent.AffectedEntityId` from `nvarchar(80)` to `nvarchar(256) NOT NULL`. Infrastructure enforces component and final capacity limits. All other AuditEvent columns/relationships/indexes remain. Table count remains 15.

## 13. Cross-Decision Consistency Assessment

| Check | Result |
|---|---|
| OQ-1 quantity with OQ-4 | Consistent: assembler loads ProductionOrder first and supplies its quantity to Batch.Rehydrate |
| OQ-2 code/surrogate | Consistent and reversible through amended unique DeviationCode |
| OQ-3 technical vs business IDs | Consistent: only technical strings parse to GUID; codes/numbers stay human-readable |
| Signature target scope | Consistent: only same-Batch BatchStepExecution is configured |
| OQ-3A formats | Consistent for DBDS-valid components; 180 ≤ 256 with mandatory boundary validation |
| OQ-4 evidence preservation | Consistent: factories accept canonical existing evidence and never call generation paths |
| Surrogate isolation | Consistent: row/assembler concern only |
| Existing contracts | Sufficient for approved reconstruction operations; no generic expansion |
| Layer dependencies | Current source and approved design keep EF/SQL out of Domain/Application |
| Schema/workflow scope | Exactly 15 tables, five Batch states, no new deviation flow or event store |

No unresolved contradiction was identified.

## 14. Architectural Constraints and Excluded Scope

Mandatory constraints:

- Domain owns manufacturing state and rules.
- Application owns use-case contracts/orchestration.
- Infrastructure owns EF Core, SQL Server, row models, integrity validation, repositories, and transactions.
- Presentation has no direct persistence access.
- Audit is append-only; ElectronicSignature is immutable.
- Restricted FK deletes remain the default.

Excluded:

- new tables, Batch states, deviation workflows, event store, mapping table, generic repository, CQRS/specification framework, rules engine, navigation from Batch to ProductionOrder, public rehydration, reflection/uninitialized construction, lazy-loading requirement, authentication implementation, UI/API/report implementation, and database changes outside AMD-001/002.

## 15. Implementation Sequence and Task 2 Prerequisites

After explicit Team acceptance/authorization:

1. Task 2 adds approved EF Core/SQL Server packages to Infrastructure only.
2. Later tasks add uniform row types/context/configurations in DBDS dependency order.
3. Coordinate OQ-1 Domain change and seven OQ-4 rehydration paths before aggregate mappings depend on them.
4. Add converters, assemblers, repositories, unit of work, migrations, and tests in the approved Sprint 3 order.
5. Inspect generated migration/schema against all §5 fields, constraints, indexes, restricted deletes, AMD-001, and AMD-002.

Task 2 prerequisites:

- Team accepts this Task 1 closure package.
- Actual package versions are selected within the target framework/support policy.
- No source implementation begins merely from this record.

## 16. Remaining Implementation Risks and Bounded Decisions

- Friend access is assembly-wide; reviews must limit internal factory calls to Infrastructure assemblers.
- Persistence-bound string length validation is mandatory because Domain strings remain unbounded.
- Supplementary Unicode capacity requires real SQL Server integration evidence.
- Batch does not expose typed verification/signature collections; later use cases must justify any read/write contract without altering this approved increment silently.
- Database-only optional fields need preservation/diff discipline.
- Explicit row-to-Domain updates require careful audit de-duplication and stable surrogate retention.
- Actual EF Core/SQL Server package versions and migration naming belong to Task 2+ implementation planning, not this gate.

None of these items contradicts the approved design; each is bounded by the acceptance criteria and requires implementation evidence.

## 17. Team Acceptance

**Status: PENDING**

Proposed Team disposition:

- Accept the consolidated Task 1 persistence design decisions.
- Authorize Sprint 3 Task 2 separately.
- Retain this record as the single repository implementation decision authority subordinate to the SRS, SDD, and amended DBDS.

Codex closure recommendation: **READY FOR TEAM ACCEPTANCE**. Formal acceptance belongs exclusively to the Team.
