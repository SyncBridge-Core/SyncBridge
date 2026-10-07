# WP-6 Sprint 2 to Sprint 3 Engineering Handoff

## A Accepted Sprint 2 Scope

Sprint 2 delivers the approved manufacturing Domain and the inner-layer contracts required to begin Sprint 3. It covers FR-002 through FR-010 at the Domain boundary, BR-001 through BR-006 and BR-008, NFR-003/NFR-005 to the Sprint 2 extent, and ADR-006 through ADR-009 to their scheduled extent. FR-001 authentication, FR-011 EBR generation, and FR-012 REST services are not claimed.

The handoff baseline is 159 passing tests: 150 accepted Task 1–7 regressions and nine Task 8 reconciliation cases.

## B Final Domain Capabilities

- ProductionOrder with DBDS-aligned identity, product, Recipe, quantity, status, and creation data.
- Recipe definitions with ordered RecipeSteps and material/equipment requirements.
- Minimal Material and Equipment reference entities.
- Five-state Batch lifecycle: Created, Ready, InProgress, Exception, Completed.
- Material and equipment prerequisite gates.
- Ordered BatchStepExecution with Pending, InProgress, Completed states.
- Immutable ElectronicSignature evidence and required-signature gate.
- Deviation origin, review, approval/rejection, resolution, Exception entry, and resolution-gated resume.
- Immutable AuditEvent evidence for significant controlled actions.
- Completion gates and terminal Completed protection.

## C Contracts Available to Sprint 3

| Contract | Sprint 3 implementation responsibility |
|---|---|
| `IProductionOrderRepository` | EF-backed order lookup, existence check, and add |
| `IRecipeRepository` | Load the complete Recipe definition by identity/code |
| `IBatchRepository` | Add Batch and load the execution aggregate with required steps/evidence |
| `IApplicationUnitOfWork` | Commit one controlled command through one scoped DbContext |
| `IActorContext` | Later Infrastructure identity adapter supplies trusted OperatorId; no authentication implementation is part of Sprint 3 persistence |

Contracts are owned by Application; Infrastructure must reference and implement them. Domain must not reference Application or Infrastructure.

## D Final Test Count and Evidence

- Final regression: 159 passed, 0 failed, 0 skipped.
- Task 8: 9/9 passed.
- Master register: `docs/testing/Test_Evidence_Register.md`.
- Final evidence index: `docs/testing/Sprint2_Final_Evidence_Index.md`.
- Raw closure evidence: `artifacts/test-evidence/sprint-2/task-8-sprint-closure/`.

## E Sprint 2 Requirements and Rule Coverage

FR-003 through FR-009 and BR-001 through BR-006/BR-008 are implemented at the Domain boundary. FR-002 is complete as a model and repository-contract input but awaits secured Application operations and persistence. FR-010 has blocking and exception semantics but awaits Application queries/UI/report visibility. NFR-003 has Domain failure atomicity but awaits Sprint 3 database transactions. NFR-005 and ADR-006/ADR-007 are implemented; ADR-008/ADR-009 have their inner contracts but await Infrastructure implementations and composition.

The detailed matrix and evidence IDs are in `docs/WP6_Sprint2_Task8_Sprint_Closure_Report.md`.

## F Final Domain to DBDS Mapping

| DBDS table | Domain/Application input | Key Sprint 3 mapping |
|---|---|---|
| AppUser | OperatorId + IActorContext | UserId is stable identity; roles stay in identity infrastructure |
| Recipe | Recipe | Map RecipeId/code/name/version/approval and owned definition rows |
| RecipeStep | RecipeStep | FK to Recipe; unique RecipeId + StepNumber |
| RecipeMaterialRequirement | RecipeMaterialRequirement | Resolve MaterialCode to MaterialId; composite key |
| RecipeEquipmentRequirement | RecipeEquipmentRequirement | Resolve EquipmentCode to EquipmentId; composite key |
| Material | Material | Direct Id/code/name reference mapping |
| Equipment | Equipment | Direct Id/code/name/nullable calibration mapping |
| ProductionOrder | ProductionOrder | Direct fields; unique OrderNumber; Recipe FK |
| Batch | Batch | Infrastructure BatchId; resolve ProductionOrderNumber; five-state conversion; timestamps from audit evidence |
| BatchStepExecution | BatchStepExecution | Resolve Batch/RecipeStep/user FKs; preserve ordered status/timestamps |
| MaterialVerification | MaterialVerification | Generate surrogate VerificationId; resolve FKs; one Batch/material row |
| EquipmentVerification | EquipmentVerification | Generate surrogate VerificationId; resolve FKs; one Batch/equipment row |
| Deviation | Deviation | Infrastructure surrogate mapping; permanent Batch and optional step links |
| ElectronicSignature | ElectronicSignature | Immutable row; same-Batch generic record link |
| AuditEvent | AuditEvent | Append-only row; optional Batch and required user/action/entity/time |

## G Explicit Sprint 3 Input

Sprint 3 receives:

- a buildable four-project solution;
- stable Domain behavior and focused identity/value types;
- use-case-oriented repository and commit contracts;
- the actor identity boundary;
- the exact 15-table DBDS and field reconciliation;
- 159 passing regression tests;
- Task 1–8 reports, raw evidence, and traceability register.

## H Known Mapping Decisions

1. `AuditEventType` maps to DBDS `AuditEvent.Action` using stable enum names. The database stores strings and does not own workflow rules.
2. Verification records have composite Domain identity but DBDS surrogate VerificationId. Infrastructure generates the surrogate once and enforces the DBDS unique Batch/item constraint.
3. Domain business identifiers remain technology-independent. Where DBDS expects `uniqueidentifier`, Sprint 3 uses reviewed converters or Infrastructure-managed surrogate keys without changing Domain behavior.
4. Batch CreatedAt, StartedAt, and CompletedAt are supplied by immutable corresponding audit events. FinalDisposition is null until an approved use case supplies it.
5. Requirement types use MaterialCode/EquipmentCode; repositories resolve those codes to reference-row IDs.

## I Constraints Sprint 3 Must Preserve

- Domain remains free of EF Core, SQL Server, ASP.NET Core, HTTP, UI, and persistence annotations.
- Repository implementations stay in Infrastructure and fulfill Application contracts.
- Application/Domain own manufacturing decisions; persistence only stores and reconstructs state.
- Explicit/eager use-case loading is preferred; lazy loading is not required.
- No one-repository-per-table expansion, generic repository framework, event store, retry history, WorkflowEvent, Product, Operator, MaterialLot, or EBR table.
- Existing public manufacturing behavior and all 159 tests remain valid.

## J Sprint 3 Non-Negotiables

- Persistence remains behind inner-layer contracts.
- No business rule migrates into EF Core configuration, SQL, triggers, or database procedures.
- Implement exactly the approved 15-table DBDS schema.
- Use restricted foreign-key delete behavior unless the DBDS explicitly says otherwise.
- Implement DBDS primary keys, foreign keys, unique constraints, lengths, nullability, and approved indexes.
- Use one scoped SyncBridgeDbContext and one atomic command boundary per controlled operation.
- Preserve append-only AuditEvent semantics.
- Preserve ElectronicSignature immutability.
- Preserve the Batch five-state model exactly.
- Preserve all Sprint 2 Domain behavior and regression evidence.

## K Remaining Implementation Decisions

- Select and document the precise EF conversion/surrogate strategy for string-valued Domain IDs mapped to DBDS GUID columns.
- Decide how Infrastructure tracks surrogate BatchId and DeviationId while Domain retains business identities.
- Confirm concrete DI registration lifetimes; DbContext and repository implementations should be scoped.
- Define integration-test database isolation and rollback fixtures.
- Confirm the fictional seed identifiers and ensure completed history is never seeded directly.

These are bounded Sprint 3 implementation decisions. They do not authorize schema or Domain behavior changes.

## L Recommended Sprint 3 Starting Sequence

1. Reconfirm the DBDS 15-table inventory and choose the reviewed identifier mapping approach.
2. Add EF Core and SQL Server packages to Infrastructure only.
3. Implement `SyncBridgeDbContext` and Fluent API configurations for AppUser, Recipe definitions, Material, and Equipment.
4. Add ProductionOrder and Batch mappings with unique business identifiers and restricted relationships.
5. Add BatchStepExecution and evidence mappings in DBDS dependency order.
6. Implement Application repository contracts and `IApplicationUnitOfWork` with one scoped DbContext.
7. Create the initial migration and inspect generated SQL against DBDS fields, keys, nullability, and indexes.
8. Add fictional reference seed data only.
9. Add integration tests for round trips, unique/FK/restricted-delete constraints, aggregate loading, and failed-command rollback.
10. Run all 159 Sprint 2 tests plus Sprint 3 integration tests before handoff to Sprint 4.

**Handoff status:** Pending AI-Agent / Team review
