# WP-6 Sprint 3 Task 1 Persistence Mapping Design Gate

## 1 Executive Summary

**Gate result: BLOCKED pending Team decisions.** The accepted Sprint 2 code, Application contracts, and approved 15-table DBDS are structurally compatible with Clean Architecture, but two baseline gaps prevent an exact, implementation-ready round trip without an approved decision:

1. `Batch` requires `PlannedQuantity` and `UnitOfMeasure` for construction and exposes both as state, while DBDS §5.9 has no columns from which either value can be reconstructed. `RecipeCode` can be derived through `Batch.OrderId -> ProductionOrder.RecipeId -> Recipe.RecipeCode`, but quantity and unit cannot be derived without inventing data.
2. `DeviationCode` is the Domain identity, but DBDS §5.12 stores only `DeviationId uniqueidentifier` and has no column for the code. Arbitrary values such as `DEV-001`, which are valid in the accepted tests, cannot be reversibly converted to a GUID and reconstructed under the exact schema.

A third decision is required for string-valued technical identities (`RecipeId`, `RecipeStepId`, `BatchStepExecutionId`, `ElectronicSignatureId`, `AuditEventId`, and `RelatedRecordId`) that map to `uniqueidentifier`. The recommended rule is to accept only canonical GUID text at the persistence boundary and map it reversibly with `Guid.Parse`/`Guid.ToString("D")`. Hashing arbitrary strings is rejected because it is not reversible; silently generating a replacement GUID is rejected because it changes identity.

Subject to those decisions, the recommended implementation is a small Infrastructure persistence representation matching the 15 DBDS tables exactly, plus explicit repository assemblers that reconstruct Domain objects. This is justified because direct mapping cannot safely materialize `Batch`: its public constructor creates a new audit event and always initializes `Created`, while its collections and later states are aggregate-owned. Reflection, uninitialized-object construction, and event replay that creates new evidence are not acceptable. A narrow, non-public Domain rehydration seam would be needed and must be approved before mapping implementation.

This report is a proposal for Team review. It does not approve the gate and does not authorize EF Core implementation.

## 2 Source Inspection Register

### 2.1 Verified repository sources

| Area | Actual path | Verified facts used |
|---|---|---|
| Production order | `src/SyncBridge.Core.Domain/Entities/ProductionOrder.cs` | `ProductionOrderId(Guid)`, business number, product fields, `RecipeId(string)`, quantity, lifecycle status, and `CreatedAt` |
| Recipe aggregate | `src/SyncBridge.Core.Domain/Entities/Recipe.cs` | Private step/material/equipment collections; public add methods enforce ownership and duplicate-step rules |
| Recipe definitions | `src/SyncBridge.Core.Domain/Entities/RecipeStep.cs`; `RecipeMaterialRequirement.cs`; `RecipeEquipmentRequirement.cs` | String technical IDs; requirement associations use material/equipment codes rather than reference GUIDs |
| Batch aggregate | `src/SyncBridge.Core.Domain/Entities/Batch.cs` | Business-number identity; constructor creates `BatchCreated`; five states; private execution/deviation/audit collections; audit-producing transitions |
| Step execution | `src/SyncBridge.Core.Domain/Entities/BatchStepExecution.cs` | String execution/step IDs; Pending/InProgress/Completed; optional timestamps and completing actor |
| Verification evidence | `src/SyncBridge.Core.Domain/Entities/MaterialVerification.cs`; `EquipmentVerification.cs` | No dedicated Domain ID; business association plus actor/time/result; each constructor creates audit evidence |
| Deviation | `src/SyncBridge.Core.Domain/Entities/Deviation.cs` | `DeviationCode(string)` identity, permanent Batch association, optional step, lifecycle data, and audit evidence |
| Signature | `src/SyncBridge.Core.Domain/Entities/ElectronicSignature.cs` | Immutable string ID, Batch, generic related-record identity, actor, meaning, time, and generated audit evidence |
| Audit | `src/SyncBridge.Core.Domain/Entities/AuditEvent.cs` | Immutable string ID; optional Batch; required actor/action/entity/time; internal creation uses GUID `D` text |
| Reference entities | `src/SyncBridge.Core.Domain/Entities/Material.cs`; `Equipment.cs` | Direct GUID IDs, unique business codes, names, and optional equipment calibration date |
| Enumerations | `src/SyncBridge.Core.Domain/Enumerations/*.cs` | Actual values listed in §7 |
| Value objects | `src/SyncBridge.Core.Domain/ValueObjects/*.cs` | GUID IDs exist only for production order, material, and equipment; other technical IDs are strings |
| Persistence contracts | `src/SyncBridge.Core.Application/Contracts/Persistence/*.cs` | Three focused repositories and `IApplicationUnitOfWork`; no table repositories or implementations |
| Actor boundary | `src/SyncBridge.Core.Application/Contracts/Identity/IActorContext.cs` | Trusted actor is exposed only as `OperatorId` |
| Regression conventions | `tests/SyncBridge.Core.Tests/*.cs` | 159 tests; accepted fixtures use human-readable non-GUID values for several string IDs |
| Sprint 2 closure | `docs/WP6_Sprint2_Task8_Sprint_Closure_Report.md`; `docs/testing/Sprint2_Final_Evidence_Index.md` | Accepted mapping obligations and historical 159-test baseline |
| Team-reviewed handoff | `docs/WP6_Sprint2_to_Sprint3_Engineering_Handoff_Team_Reviewed.docx`, §§F, H-L | Design gate, 15-table mapping, timestamp sources, surrogate decisions, and Sprint 3 constraints |

Actual namespaces are `SyncBridge.Core.Domain.Entities`, `SyncBridge.Core.Domain.ValueObjects`, `SyncBridge.Core.Domain.Enumerations`, `SyncBridge.Core.Application.Contracts.Persistence`, and `SyncBridge.Core.Application.Contracts.Identity`.

### 2.2 Engineering references inspected

| Source | Sections inspected | Role in this decision |
|---|---|---|
| `C:\Engineering\Golden_Documents\Indy-1-SyncBridge-DBDS_v2.docx` | §§3, 5, 6, 7, Appendix A | Exact 15 tables, columns, types, keys, optionality, indexes, restricted deletes, EF boundary, and transaction model |
| `C:\Engineering\Golden_Documents\SyncBridge_Core_SDD_Corrected.docx` | §§3.2, 4.1-4.5, 5.1-5.4 | Dependency direction, Domain ownership, Application contracts, Infrastructure responsibility |
| `C:\Engineering\Golden_Documents\SyncBridge_Core_SRS_Corrected.docx` | §§3-4 | Identity attribution, immutable evidence, lifecycle data, and transactional integrity |
| `C:\Engineering\Golden_Documents\WP-6_Implementation_Execution_Plan_Realigned.docx` | §4.3 and scope controls | Exact Sprint 3 scope, implementation order, one scoped context, and acceptance tests |
| `Z:\InstallationPackages\Transfer_Folder\Sprint_3\WP-6_Sprint3_SQLServer_and_EF_Core_Persistence.docx` | Full document | Task sequence and confirmation that this design gate precedes persistence work |

All references named by the task were located. The Team-reviewed handoff was found in the repository rather than the Golden Documents or transfer folder.

## 3 Domain to DBDS Mapping Matrix

`Verified` describes the accepted source/schema. `Proposed` describes the Task 1 recommendation and remains subject to Team approval.

| DBDS table | Verified Domain/Application representation | Identifier approach | Proposed mapping and conversion |
|---|---|---|---|
| `AppUser` | No Domain entity; `OperatorId(string)` and `IActorContext.ActorId` | `UserId nvarchar(64)` | Infrastructure row only. Store `OperatorId.Value` unchanged; seed fictional user identity data in Sprint 3. Roles/authentication remain outside this table and outside this task. |
| `Recipe` | `Recipe` with `RecipeId(string)`, `RecipeCode`, name, version, approval | `RecipeId uniqueidentifier`; unique code | Require canonical GUID text for persistence, parse to GUID, and restore with `D` format. Store code/name/version/approval at DBDS lengths. |
| `RecipeStep` | `RecipeStep` with `RecipeStepId(string)` and owning `RecipeId(string)` | `RecipeStepId uniqueidentifier`; Recipe FK; unique `(RecipeId, StepNumber)` | Apply the same canonical GUID rule to both IDs. Keep step ordering enforcement in Domain; database enforces only key/unique structure. |
| `RecipeMaterialRequirement` | `RecipeMaterialRequirement` uses `RecipeId` plus `MaterialCode`; optional quantity | Composite `(RecipeId, MaterialId)` | Resolve unique `MaterialCode` to `MaterialId`; persist quantity as `decimal(18,3)` or null. A missing/duplicate code is a persistence input integrity failure. |
| `RecipeEquipmentRequirement` | `RecipeEquipmentRequirement` uses `RecipeId` plus `EquipmentCode` | Composite `(RecipeId, EquipmentId)` | Resolve unique `EquipmentCode` to `EquipmentId`; reject unresolved/ambiguous references before commit. |
| `Material` | `Material` with `MaterialId(Guid)`, code, name | `MaterialId uniqueidentifier`; unique code | Direct scalar conversion is practical. Use an Infrastructure row for consistency with the uniform DBDS model; map GUID unchanged. |
| `Equipment` | `Equipment` with `EquipmentId(Guid)`, code, name, nullable `DateOnly` | `EquipmentId uniqueidentifier`; unique code | Map GUID unchanged and `DateOnly?` to nullable SQL `date`. |
| `ProductionOrder` | `ProductionOrder` with `ProductionOrderId(Guid)`, number, product, `RecipeId(string)`, quantity, status, created time | `OrderId uniqueidentifier`; Recipe FK; unique order number | Map `ProductionOrderId.Value` directly to `OrderId`; parse canonical Recipe GUID; map quantity `decimal(18,3)`, timestamp `datetimeoffset(7)`, and explicit status strings. |
| `Batch` | `Batch` has no GUID ID; `BatchNumber`, production-order number, recipe code, planned quantity/UOM, status, aggregate collections/audit | Infrastructure `BatchId uniqueidentifier`; unique batch number | Generate `BatchId` once on insert and retain it in the row. Resolve order number to `OrderId`; derive recipe code through Order/Recipe when loading. Status uses explicit conversion. Timestamps come from audit events. Exact quantity/UOM reconstruction is blocked (§10). |
| `BatchStepExecution` | String execution and recipe-step IDs; Batch number; step/status/times/completing actor; `RequiresSignature` | `StepExecutionId uniqueidentifier`; Batch/RecipeStep/user FKs | Canonical GUID conversion for both technical IDs; resolve Batch number to `BatchId`; store optional actor as `nvarchar(64)`. `RequiresSignature` is derived from RecipeStep; `RecordedData` remains null until approved behavior supplies it. |
| `MaterialVerification` | No Domain verification ID; Batch number, material code, actor, time, Passed/Failed | Infrastructure `VerificationId uniqueidentifier`; unique `(BatchId, MaterialId)` | Generate ID only for a new unique Batch/material row; retain it for later reads/updates. Map Passed=true, Failed=false. Lot/comments remain null when absent from Domain. |
| `EquipmentVerification` | No Domain verification ID; Batch number, equipment code, actor, time, Passed/Failed | Infrastructure `VerificationId uniqueidentifier`; unique `(BatchId, EquipmentId)` | Same stable key rule. Map Passed=true, Failed=false. `CalibrationValid`/comments remain null when Domain supplies no evidence. |
| `Deviation` | `DeviationCode(string)` is Domain identity; Batch, optional step, description, actor, opened/status/review/disposition/resolution | `DeviationId uniqueidentifier`; no code column | Preferred reversible option is canonical GUID text in `DeviationCode`, mapped 1:1 to `DeviationId`. This conflicts with accepted human-readable examples and requires Team approval; otherwise exact DBDS cannot preserve the code (§10). |
| `ElectronicSignature` | Immutable string ID; Batch; related type/ID; actor; meaning/time | `SignatureId` and `RelatedRecordId` are GUID; Batch/User associations | Require canonical GUID text for signature and related record IDs. Resolve Batch/user; store type and meaning as explicit strings. Insert-only repository behavior preserves immutability. |
| `AuditEvent` | Immutable string ID; optional Batch; actor; enum action; affected entity/ID; time/state/details | `AuditEventId uniqueidentifier`; optional Batch FK; required User FK | Parse canonical GUID ID (internal creation already emits `D` format), resolve optional Batch and required user, map action exactly to enum name, and insert only. `AffectedEntityId` remains its display string per DBDS. |

The proposed persistence representation contains exactly these 15 row types and no additional tables. It is not a second business model; it is the Infrastructure shape required to honor the DBDS without placing EF concerns in Domain.

## 4 Identifier and Surrogate Key Strategy

### 4.1 Verified identifier classes

- Native GUID Domain values: `ProductionOrderId`, `MaterialId`, `EquipmentId`.
- String technical identities: `RecipeId`, `RecipeStepId`, `BatchStepExecutionId`, `ElectronicSignatureId`, `AuditEventId`, `RelatedRecordId`.
- String business identities/codes: `ProductionOrderNumber`, `BatchNumber`, `RecipeCode`, `MaterialCode`, `EquipmentCode`, `ProductCode`, `DeviationCode`, `OperatorId`.
- No Domain identifier exists for Batch or either verification record.

### 4.2 Proposed reversible conversion

For string technical IDs stored as `uniqueidentifier`, Infrastructure should use one strict converter:

1. On write, require `Guid.TryParseExact(value, "D", out id)` (accepting another GUID syntax only if the Team explicitly approves normalization).
2. Store the parsed GUID.
3. On read, reconstruct the value object with `id.ToString("D")`.
4. Fail before `SaveChanges` with a persistence mapping exception when a value is not a valid GUID; do not hash it and do not silently replace it.

This is deterministic, reversible, and schema-preserving. Existing unit tests that use `RECIPE-ID-001`, `STEP-001`, `EXEC-001`, or `SIG-001` remain valid because they do not exercise persistence. Sprint 3 integration fixtures and all later persistence-bound creation paths must use canonical GUID values.

### 4.3 Infrastructure-managed keys

- **BatchId:** Generate once when adding a new `BatchRow`. The unique BatchNumber lookup recovers the same row and key on every later operation. Child records reference this stable key. Never regenerate it during updates or reconstruction.
- **VerificationId:** Before insert, resolve the unique `(BatchId, MaterialId)` or `(BatchId, EquipmentId)` row. Generate a GUID only when no row exists. Retain the existing key for a controlled update. The DB unique constraint is the final race-condition guard.
- **DeviationId:** The exact schema has no storage for `DeviationCode`. The recommended schema-preserving mapping is to use a canonical GUID-valued `DeviationCode` as the persisted `DeviationId`; an independent opaque surrogate would lose the Domain code. Team approval is required.

### 4.4 Alternatives considered

| Alternative | Decision | Reason |
|---|---|---|
| Direct value converter for arbitrary string -> random GUID | Rejected | Not stable unless an unauthorized mapping store is added; cannot reconstruct the original identity |
| Deterministic UUID/hash | Rejected | Stable but not reversible; changes accepted Domain identity after round trip |
| Add code/identity columns or mapping table | Rejected for Task 1 | Changes the approved DBDS or creates a 16th table |
| EF shadow keys on Domain types | Rejected for aggregates | Shadow values are persistence-only, but `Batch` still cannot be safely materialized and Domain-only values remain absent |
| Canonical GUID text for technical string IDs | Recommended | Reversible, simple, and compatible with the exact schema; needs Team approval for creation conventions |

## 5 Reference Code Resolution Strategy

Resolution belongs in Infrastructure repository mapping, not in SQL workflow logic:

1. Load the unique reference row by code using the DBDS unique index.
2. If no row exists, return a typed persistence/integrity failure to the Application caller before mutation is committed.
3. If more than one row exists, treat it as corrupted data even though the unique constraint should prevent it.
4. Use the resolved GUID only to form foreign keys. Domain prerequisite rules continue to operate on codes and `VerificationResult`.

Specific associations:

- `MaterialCode -> Material.MaterialId` for recipe requirements and material verification.
- `EquipmentCode -> Equipment.EquipmentId` for recipe requirements and equipment verification.
- `ProductionOrderNumber -> ProductionOrder.OrderId` for `Batch.OrderId`.
- `BatchNumber -> Batch.BatchId` for every execution/evidence child.
- `RecipeCode` for a loaded Batch is obtained through `Batch.OrderId -> ProductionOrder.RecipeId -> Recipe.RecipeCode`; no direct Batch-to-Recipe FK is added.

The repository must perform resolution in the same scoped DbContext/transaction as the write. A missing reference is not a manufacturing transition rule and must not be represented as a new Domain state.

## 6 Timestamp Reconstruction Strategy

### 6.1 Verified event representations

`AuditEventType` is an enum in `src/SyncBridge.Core.Domain/Enumerations/AuditEventType.cs`. The applicable events are:

- `BatchCreated` -> `Batch.CreatedAt`
- `BatchProcessingStarted` -> `Batch.StartedAt`
- `BatchCompleted` -> `Batch.CompletedAt`

`BatchProcessingResumed` does not replace or advance `StartedAt`. It records return from Exception, not initial execution start.

### 6.2 Proposed write behavior

The immutable audit event is authoritative. In the same atomic commit that inserts the event, Infrastructure copies its `OccurredAt` value into the corresponding Batch timestamp column. This satisfies DBDS §5.9 while keeping the event history required by FR-008/BR-005. Repository integration tests must assert row/event equality.

### 6.3 Proposed read and missing-event behavior

- Exactly one `BatchCreated` event is required for every Batch. Missing or multiple creation events are data corruption; aggregate loading fails. No current time or row fallback is invented.
- `BatchProcessingStarted` is absent only for `Created` and `Ready`. It is required exactly once for `InProgress`, `Exception`, or `Completed`.
- `BatchCompleted` is required exactly once only for `Completed`; it must be absent for earlier states.
- If a populated Batch row timestamp disagrees with its authoritative event, aggregate loading fails and reports a persistence-integrity error. Infrastructure must not silently choose one.

These checks validate stored structure/history; they do not decide whether a transition is allowed.

## 7 Enum and Value Conversion Strategy

All converters must be explicit closed mappings that throw on unknown database values. Database strings do not drive Domain transitions.

| Domain type | Domain values | SQL representation |
|---|---|---|
| `BatchStatus` | Created, Ready, InProgress, Exception, Completed | `nvarchar(20)`: `Created`, `Ready`, `In Progress`, `Exception`, `Completed` |
| `StepExecutionStatus` | Pending, InProgress, Completed | `nvarchar(20)`: `Pending`, `In Progress`, `Completed` |
| `DeviationStatus` | Open, UnderReview, Approved, Rejected, Closed | `nvarchar(30)`: `Open`, `Under Review`, `Approved`, `Rejected`, `Closed` |
| `ProductionOrderStatus` | Planned, Released, InProcess, Completed, Cancelled | `nvarchar(30)`: stable explicit values `Planned`, `Released`, `InProcess`, `Completed`, `Cancelled` because DBDS defines no alternate labels |
| `VerificationResult` | Passed, Failed | `bit`: true, false respectively (`IsAccepted`/`IsReady`) |
| `ElectronicSignatureMeaning` | Approval, Verification, Review, Release, Completion | `nvarchar(200)`: exact enum names |
| `AuditEventType` | 16 accepted values | `AuditEvent.Action nvarchar(100)`: exact enum names listed below |
| `EquipmentStatus` | Available, Running, Maintenance, OutOfService | Not persisted: no approved DBDS column references this currently unused enum |

Exact `AuditEventType -> Action` values are `BatchCreated`, `BatchMarkedReady`, `BatchProcessingStarted`, `MaterialVerified`, `EquipmentVerified`, `StepStarted`, `StepCompleted`, `ElectronicSignatureApplied`, `DeviationOpened`, `DeviationReviewStarted`, `DeviationApproved`, `DeviationRejected`, `DeviationResolved`, `BatchExceptionEntered`, `BatchProcessingResumed`, and `BatchCompleted`.

Other scalar conversions are direct: `DateTimeOffset -> datetimeoffset(7)`, `DateOnly? -> date null`, decimal quantities -> `decimal(18,3)`, and bounded value-object strings -> their DBDS `nvarchar` lengths. Length violations must fail validation/mapping before or during the atomic commit; values must never be truncated.

## 8 Identity and Evidence Associations

`AppUser.UserId` is the stable relational identity and uses `OperatorId.Value` unchanged (`nvarchar(64)`). The association applies to:

- material/equipment verification `VerifiedByUserId`;
- step execution `CompletedByUserId` when present;
- deviation `ReportedByUserId`;
- electronic signature `UserId`;
- audit event `UserId`.

Infrastructure verifies that referenced AppUser rows exist and relies on restricted FKs. It does not decide authorization, active-user policy, signature permission, or actor substitution. `IActorContext` remains the only Application-facing actor contract; its authentication-backed implementation belongs to a later sprint.

ElectronicSignature remains insert-only. AuditEvent remains append-only. Updates/deletes for either should not be exposed through repositories, and EF change interception or SaveChanges validation may reject modified/deleted tracked rows as an Infrastructure integrity measure. Same-Batch generic-record validation remains Domain/Application behavior; a generic `RelatedRecordId` cannot be protected by a polymorphic FK without changing the DBDS.

## 9 Repository Reconstruction Strategy

### 9.1 Recommended EF shape

Use Infrastructure-only persistence row classes for the exact 15 DBDS tables and Fluent API configurations for all columns, keys, FKs, unique constraints, indexes, and restricted deletes. Repository assemblers translate rows to Domain objects and Domain state back to rows.

Direct EF mapping is technically practical for `Material`, `Equipment`, `ProductionOrder`, `Recipe`, and `RecipeStep` because their scalar shapes are close to the schema. It is not practical for Batch/evidence types because of missing surrogate properties, constructor-created audit events, default-state constructors, private aggregate collections, code-to-FK resolution, and DB-only optional columns. A mixed direct/row approach would create two materialization conventions and complicate aggregate loading. The uniform row model is therefore the simpler maintainable choice despite the extra mapping code.

### 9.2 Aggregate loading

- `IRecipeRepository`: query Recipe plus ordered steps and both requirement joins; resolve Material/Equipment rows to codes; assemble via a non-rule-bypassing rehydration path.
- `IProductionOrderRepository`: query order by unique number, preserve its GUID ID and Recipe GUID-text identity, and reconstitute its persisted status without replaying lifecycle methods.
- `IBatchRepository.GetExecutionAggregateAsync`: query Batch by number, its ProductionOrder/Recipe context, ordered step executions, verification rows, deviations, signatures needed by the use case, and audit events. Use explicit/eager queries; no lazy loading.
- `IBatchRepository.AddAsync`: create the Batch row and all currently owned evidence, resolve codes/numbers in the same context, generate required surrogates once, and de-duplicate audit rows by `AuditEventId` because evidence may be reachable through more than one Domain path.
- `IApplicationUnitOfWork.CommitAsync`: call one scoped DbContext commit. A controlled command and its audit/signature evidence succeed or roll back together.

### 9.3 Required rehydration seam

The current public constructors represent creation, not persistence materialization. Replaying public methods would rerun rules and generate new audit IDs; reflection or `RuntimeHelpers.GetUninitializedObject` would be fragile and unsafe. The proposed solution is a narrow non-public Domain rehydration factory/constructor that accepts already validated persisted state and collections without producing new evidence. It must:

- be inaccessible to Presentation/Application callers;
- add no EF or SQL dependency;
- preserve existing public constructors and methods unchanged;
- perform structural consistency checks without re-enacting historical transitions;
- be covered by repository round-trip tests.

The Team must approve the exact accessibility mechanism (for example, internal factories with a reviewed friend-assembly boundary) before implementation.

## 10 Architectural Compliance Review

| Constraint | Review result |
|---|---|
| Domain independent of EF Core/SQL/ASP.NET/Infrastructure | Compliant in current source; proposed row model preserves it |
| Application owns repository/actor contracts | Compliant: contracts are in `SyncBridge.Core.Application.Contracts.*` |
| Infrastructure implements contracts | Proposed; no implementation exists yet |
| Domain owns manufacturing invariants/transitions | Preserved; no rule is assigned to EF or SQL |
| Exactly 15 DBDS tables | Preserved by proposal; no mapping table/event store/new table |
| Restricted deletes | Required for all FKs unless DBDS explicitly says otherwise |
| Append-only audit | Preserved through insert-only access and mutation rejection |
| Immutable signatures | Preserved through insert-only access and mutation rejection |
| Five Batch states | Exact explicit conversion; no extra state |
| One scoped atomic command boundary | `IApplicationUnitOfWork` plus one scoped DbContext |
| No generic/per-table repositories | Preserved; only the three accepted use-case repositories are implemented |

## 11 Open Questions and Risks

### OQ-1 Batch reconstruction data gap - blocking

`Batch.PlannedQuantity` and `Batch.UnitOfMeasure` are required Domain state but absent from DBDS §5.9 and cannot be derived reliably. Options are a formally approved DBDS change, a formally approved Domain rehydration/design change, or an authoritative derivation rule supplied by the Team. Task 1 does not select one because doing so would alter the baseline.

### OQ-2 Deviation identity loss - blocking

The DBDS has `DeviationId` but no `DeviationCode`. Approve canonical GUID-valued `DeviationCode` mapped directly to `DeviationId`, or approve a controlled baseline change. A separate random surrogate cannot preserve the accepted Domain identity.

### OQ-3 String technical ID format - approval required

Approve canonical GUID `D` text for every persistence-bound string technical identity. Existing non-GUID unit-test examples remain valid for pure Domain tests but are not persistence-compatible under the exact schema.

### OQ-4 Domain rehydration seam - approval required

Approve a narrow non-public rehydration mechanism. Without it, exact aggregate reconstruction would require audit-producing public behavior replay or unsafe reflection.

### Additional implementation risks

- DB-only optional fields (`LotNumber`, verification comments, `CalibrationValid`, `RecordedData`) have no Domain source and must remain null until approved behavior exists.
- `Batch.FinalDisposition` remains null because no approved Batch-level disposition behavior exists.
- Generic signature related-record associations cannot be enforced with a conventional FK; same-Batch validation must remain in Domain/Application.
- Audit evidence reachable through Batch, deviations, verification, and signatures must be de-duplicated by stable audit ID before insert.
- Persisted string lengths are stricter than most current value objects; Infrastructure must reject oversize data rather than truncate it.

## 12 Recommended Task 2 Prerequisites

Before installing/configuring persistence dependencies or beginning mappings, the Team should record approval or disposition for:

1. OQ-1 Batch quantity/UOM reconstruction.
2. OQ-2 DeviationCode/DeviationId identity.
3. The canonical GUID-text rule for string technical IDs.
4. The Infrastructure-only 15-row persistence representation and explicit assembler approach.
5. The non-public Domain rehydration seam and its accessibility mechanism.
6. Explicit enum strings and missing-event failure behavior specified in §§6-7.

After approval, Task 2 may configure EF Core/SQL Server dependencies in Infrastructure only. Mapping, DbContext, repositories, migrations, and database changes remain reserved for later Sprint 3 tasks.

**Design gate status: BLOCKED - Team review and decisions required. Not approved.**
