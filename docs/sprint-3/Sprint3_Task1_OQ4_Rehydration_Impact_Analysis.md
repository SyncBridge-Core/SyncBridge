# WP-6 Sprint 3 Task 1 — OQ-4 Domain Aggregate Rehydration Impact Analysis

## 1. Executive Summary

**Investigation result: READY FOR TEAM REVIEW. OQ-4 is not approved, and persistence implementation remains blocked pending Team disposition.**

The provisional Option A is the smallest technically sound design: Infrastructure-owned EF Core row models, explicit repository assemblers, and entity-owned internal rehydration factories backed by private full-state constructors. The Domain project would grant friend access only to the named Infrastructure assembly. Public creation and lifecycle APIs would remain unchanged except for the separately approved coordinated OQ-1 Batch constructor correction.

Ordinary constructors cannot safely reconstruct all historical state:

- Batch, MaterialVerification, EquipmentVerification, Deviation, and ElectronicSignature constructors create new AuditEvents.
- ProductionOrder and BatchStepExecution constructors force initial states.
- Public lifecycle methods transition state and, for Batch and Deviation, create new historical evidence.
- Batch and Deviation own private collections that cannot be restored through current public APIs without replaying behavior.

Recipe, RecipeStep, recipe requirements, Material, Equipment, and AuditEvent can use their existing public constructors during assembly because those constructors are side-effect-free and accept their complete persisted scalar state. Recipe collections can be rebuilt with existing guarded add methods. Dedicated restricted rehydration is required for ProductionOrder, Batch, BatchStepExecution, both verification types, Deviation, and ElectronicSignature.

OQ-3A cannot be confirmed unconditionally from the current Domain because `BatchNumber`, material/equipment codes, `OperatorId`, and the public AuditEvent affected-entity input have no Domain maximum length. For values already valid under the amended DBDS component limits, the longest generated identifier is 180 UTF-16 storage units, so `nvarchar(256)` leaves 76 units. The amendment remains conditional until the Team confirms that DBDS length validation at the persistence boundary defines the supported persisted value set; no Domain audit format change is recommended.

## 2. Provisional Decision and Scope

### 2.1 Provisional OQ-4 design

Option A separates persistence representation from Domain behavior:

1. EF Core maps exactly 15 Infrastructure row types.
2. Repositories explicitly/eagerly load a use-case graph.
3. Assemblers validate relational and historical consistency.
4. Internal Domain factories restore stored state without calling creation or lifecycle operations.
5. Database-only surrogate keys and optional fields remain Infrastructure concerns.

This report validates the design and identifies required changes. It does not add factories, friend assemblies, row models, EF Core packages, repositories, or migrations.

### 2.2 Approved decisions incorporated

- **OQ-1:** Batch quantity is copied from the associated ProductionOrder quantity; UnitOfMeasure is removed; Batch retains only ProductionOrderNumber.
- **OQ-2:** DeviationCode remains the Domain/business identity and resolves to a stable DeviationId through `DBDS-AMD-001`.
- **OQ-3:** Existing identifier types remain; persistence-bound technical strings use exact lowercase GUID `D` text; only BatchStepExecution is a supported signature target; AffectedEntityId retains entity-specific representation.
- **OQ-3A:** `DBDS-AMD-002` conditionally extends AffectedEntityId to `nvarchar(256)` subject to the verification in §13.

## 3. Source and Document Inspection Register

### 3.1 Repository sources

| Area | Files inspected | Verified facts |
|---|---|---|
| Domain entities | All 14 types named by the task | Constructor accessibility, inputs, guards, state defaults, private setters, collections, lifecycle methods, and audit side effects |
| Value objects/enums | Identifier value objects and all lifecycle/evidence enums | Equality/representation, five Batch states, three execution states, five deviation states, signature meanings, and 16 audit actions |
| Application contracts | Three repositories, unit of work, actor context | Current load/add/commit capabilities and dependency direction |
| Tests | All 159 accepted test cases and identifier/construction usages | Creation behavior, lifecycle rules, evidence immutability, identity equality, and completed protection |
| Sprint 3 reports | Original design gate, OQ-1, and OQ-3 analyses | Approved inputs supplied by the current task authority and prior open rehydration concerns |

### 3.2 Authoritative documents

| Document | Actual path and relevant content |
|---|---|
| Team-reviewed handoff | `docs/WP6_Sprint2_to_Sprint3_Engineering_Handoff_Team_Reviewed.docx`; Infrastructure reconstructs state, explicit/eager loading, immutable evidence, timestamp sources |
| Revised DBDS | `C:\Engineering\Golden_Documents\Indy-1-SyncBridge-DBDS_v2.docx`; exact 15 tables, Appendix B `DBDS-AMD-001`, Appendix C `DBDS-AMD-002` |
| Corrected SDD | `C:\Engineering\Golden_Documents\SyncBridge_Core_SDD_Corrected.docx`; Domain ownership, Infrastructure persistence, inward dependencies |
| Corrected SRS | `C:\Engineering\Golden_Documents\SyncBridge_Core_SRS_Corrected.docx`; immutable audit, permanent signatures, transaction consistency, complete EBR history |
| Realigned execution plan | `C:\Engineering\Golden_Documents\WP-6_Implementation_Execution_Plan_Realigned.docx`; exact schema, explicit/eager loading, round trips, atomic commits |

All required documents were available. The DBDS contains both required amendments and identifies Appendix C as approved in principle pending this technical verification.

## 4. Construction and Side-Effect Matrix

| Domain type | Existing construction and mutable state | Side effects/guards | Historical reconstruction assessment |
|---|---|---|---|
| ProductionOrder | Public constructor takes full identity/product/recipe/quantity/created time; always sets Planned; status private | Validates nonempty Guid, product name, positive quantity; Release/Start/Complete/Cancel mutate status | **Dedicated rehydration required** for any non-Planned row; command replay is unnecessary and cannot represent arbitrary valid persisted status safely |
| Recipe | Public constructor takes full scalar state; private step/requirement collections; public Add methods | Required name/version; Add methods validate ownership and duplicate step number; no audit | **Existing constructor and Add methods are safe**; they also provide useful structural checks |
| RecipeStep | Public constructor contains all stored Domain fields | Positive step number; required name/instructions; no audit | **Existing constructor is safe** |
| RecipeMaterialRequirement | Public constructor contains full Domain state | Required references; optional quantity must be positive; no audit | **Existing constructor is safe** |
| RecipeEquipmentRequirement | Public constructor contains full Domain state | Required references; no audit | **Existing constructor is safe** |
| Material | Public constructor contains full Domain state | Nonempty Guid, required name; no audit | **Existing constructor is safe** |
| Equipment | Public constructor contains full Domain state | Nonempty Guid, required name; no audit | **Existing constructor is safe** |
| Batch | Public constructor sets Created and owns private executions/deviations/direct-audit lists | Validates positive quantity; creates a new BatchCreated AuditEvent | **Dedicated rehydration required** for every persisted Batch, including Created, because ordinary construction changes evidence identity |
| BatchStepExecution | Public constructor sets Pending; status/timestamps/completing actor have private setters; Start/Complete are internal | Positive step number; Start/Complete enforce transitions; Batch records corresponding audit | **Dedicated rehydration required** for InProgress/Completed and recommended uniformly for all persisted executions |
| MaterialVerification | Public constructor accepts evidence fields; private setters | Creates a new MaterialVerified AuditEvent with new ID | **Dedicated rehydration required** to attach the original audit object |
| EquipmentVerification | Same shape as material verification | Creates a new EquipmentVerified AuditEvent with new ID | **Dedicated rehydration required** |
| Deviation | Public constructors always create Open; owns private audit list; lifecycle fields private | Constructor creates DeviationOpened; review/approve/reject/resolve methods mutate state and create events | **Dedicated rehydration required** for every persisted deviation |
| ElectronicSignature | Public constructor contains signature values; properties immutable | Creates a new ElectronicSignatureApplied AuditEvent with new ID | **Dedicated rehydration required** to preserve the original audit event |
| AuditEvent | Public constructor accepts all persisted Domain fields; immutable properties | Validates nonblank event/affected IDs, known enum, required actor/entity; no secondary event | **Existing public constructor is safe** and should be the canonical evidence reconstruction path |

### 4.1 Operations that must never be replayed for loading

- Batch constructor, MarkReady, StartProcessing, EnterException, ResumeProcessing, Complete, StartStep, and CompleteStep.
- MaterialVerification and EquipmentVerification constructors.
- Deviation constructors, BeginReview, Approve, Reject, and Resolve.
- ElectronicSignature constructor.
- ProductionOrder lifecycle methods and BatchStepExecution Start/Complete, even where they do not themselves emit audit, because reconstruction must restore facts rather than re-decide or sequence historical commands.

`AuditEvent.Create` is also prohibited during rehydration because it generates a new technical identity. Assemblers must use the public AuditEvent constructor with the stored ID and values.

## 5. Batch Aggregate Reconstruction Matrix

| Required state | Source and reconstruction | Structural validation |
|---|---|---|
| BatchNumber | Batch.BatchNumber | Required, unique, unchanged |
| ProductionOrderNumber | Batch.OrderId join to ProductionOrder.OrderNumber | Required order exists and Batch.OrderId resolves uniquely |
| PlannedQuantity | Associated ProductionOrder.Quantity under OQ-1 | Positive and exactly equal to authoritative order quantity; never read from a Batch column |
| RecipeCode | Batch.OrderId -> ProductionOrder.RecipeId -> Recipe.RecipeCode | Order recipe exists; code remains human-readable; no Batch navigation property |
| BatchStatus | Batch.Status explicit closed conversion | Only Created, Ready, InProgress, Exception, Completed |
| Created/start/completion times | Batch row timestamps checked against BatchCreated, BatchProcessingStarted, BatchCompleted audit events | Required/absent by state; exact timestamp agreement; one authoritative event of each applicable type |
| Step executions | BatchStepExecution rows joined to RecipeStep and ordered by StepNumber | Unique ID/step, same Batch, recipe ownership, stored StepNumber matches definition, recognized state, coherent timestamps/actor |
| Material verifications | MaterialVerification joined to Material and AppUser; reconstruct with original audit event | Unique Batch/material, same Batch, known result, exact actor/time/action/affected identity |
| Equipment verifications | EquipmentVerification joined to Equipment and AppUser | Same checks using equipment association |
| Deviations | Deviation rows plus optional execution and their audit events | Unique DeviationCode; same Batch; optional execution belongs to Batch; state-specific review/disposition/resolution consistency |
| Electronic signatures | ElectronicSignature rows plus AppUser and related StepExecution | Canonical IDs; exact BatchStepExecution discriminator; target exists in same Batch; original signature audit attached |
| Audit history | AuditEvent rows ordered by OccurredAt and stable ID | Unique IDs, known action, required actor, correct Batch association, unchanged display identity, no missing or conflicting duplicates |
| Completed protection | Restored status and complete historical graph | Completed requires completion evidence, all required recipe steps completed, and no blocking deviation; subsequent public methods retain existing guards |

### 5.1 Five-state structural expectations

- **Created:** one BatchCreated event; no processing-start/completion event; StartedAt/CompletedAt null.
- **Ready:** creation and ready evidence; no processing-start/completion event; StartedAt/CompletedAt null.
- **InProgress:** exactly one original processing-start event and matching StartedAt; no completion event; CompletedAt null. Resume events may exist after an earlier Exception.
- **Exception:** processing-start evidence exists; at least one associated blocking deviation and exception-entry evidence; no completion event.
- **Completed:** creation/start/completion evidence and matching timestamps; all required executions complete; no blocking deviation; completion is terminal under existing methods.

These checks confirm that stored state is internally representable. They do not rerun prerequisite verification or decide whether historical operators should have been allowed to act.

### 5.2 Collection ownership and evidence gap

Batch currently owns only `_stepExecutions`, `_deviations`, and `_auditEvents`. It does not retain typed MaterialVerification, EquipmentVerification, or ElectronicSignature collections. During normal behavior, verification/signature AuditEvidence is added to `_auditEvents`, while Deviation audit events remain in each Deviation and are concatenated by `Batch.AuditEvents`.

The recommended assembler should therefore:

1. reconstruct typed verification/signature rows for consistency validation;
2. attach their canonical existing AuditEvent instances to Batch's direct/evidence audit collection through the restricted Batch factory;
3. attach deviation events only to the corresponding Deviation so `Batch.AuditEvents` does not duplicate them; and
4. return Batch with its currently owned collections unchanged.

Typed verification/signature records are not exposed by the returned Batch. If a later operational use case requires them after loading, the Team must approve either an explicit Application read model or new aggregate ownership. OQ-4 should not silently broaden the Domain aggregate. Reporting/EBR reads can query normalized persistence through their later approved read boundary.

## 6. Historical Evidence and Immutable-State Analysis

Infrastructure should build one canonical `AuditEvent` instance per AuditEventId and index it during assembly. Evidence-producing objects receive references from this index:

- each verification receives its single original matching verification event;
- each ElectronicSignature receives its original signature-applied event;
- each Deviation receives only its own ordered lifecycle events; and
- Batch receives direct Batch/step events plus verification/signature evidence, excluding events owned through Deviation.

Before construction, the assembler must fail if two rows share an AuditEventId, if one row is represented by conflicting data, if an expected evidence event is missing or ambiguous, or if an event is associated with the wrong Batch/entity/identity. It must not merge similar events, generate replacements, deduplicate distinct legitimate events, or use current time.

ElectronicSignature and AuditEvent remain immutable after restoration. Deviation audit collections and Batch audit collections should be defensively copied by the private full-state constructors. Original identifiers, actors, actions, meanings, timestamps, resulting state, details, and entity-specific affected IDs must pass through unchanged.

## 7. Rehydration Access-Mechanism Comparison

| Approach | Encapsulation and architecture | Complexity/testability | Risk | Assessment |
|---|---|---|---|---|
| **A. Internal entity factories + friend Infrastructure assembly** | Domain owns construction rules; Infrastructure references Domain; Domain gains no project/type dependency on Infrastructure, only a reviewed assembly-name grant | Small private constructors/internal factories; repository integration tests exercise them | Friend access is assembly-wide, so code review must keep use limited to assemblers | **Recommended** |
| **B. Domain-owned public factory interfaces/assembly mechanism** | Can preserve direction but requires public state carriers or extra abstractions so Infrastructure can provide full history | More types and indirection with no current second consumer | Risks a generic persistence/factory framework and public historical-state vocabulary | Not justified |
| **C. Public rehydration APIs** | Exposes arbitrary historical state to Application/Presentation callers | Easy to call and unit test | Highest risk of bypassing aggregate creation/lifecycle rules | Reject |
| **D. EF constructor/property materialization** | Couples EF behavior to private Domain shape and still cannot solve constructor-created evidence safely | Mapping appears smaller but hidden coupling is high | May invoke side effects, bypass collections, or require persistence annotations/setters | Reject |
| **E. Reflection/uninitialized objects** | Bypasses Domain ownership entirely | Fragile under runtime/refactoring; difficult to test meaningfully | Can create impossible objects and skip all invariants | Reject |
| **F. Historical command/event replay** | Re-executes business operations rather than restoring facts | Complex ordering and versioning | Generates new audit IDs/evidence and can fail under current rules | Reject |

### 7.1 Recommended concrete access design

- Add the SDK `InternalsVisibleTo` item for `SyncBridge.Core.Infrastructure` to the Domain project file. No Domain project reference to Infrastructure is added.
- Add `internal static Rehydrate(...)` methods on the seven affected entities.
- Back each method with a private full-state constructor. Do not use Boolean “skip validation/skip audit” flags in public constructors.
- Keep the factories entity-specific; do not introduce a generic rehydration framework, generic state bag, reflection, or EF attributes.
- Do not grant friend access to Presentation or Application. Prefer repository/assembler integration tests over adding the test assembly as another friend.

The factory validates local structural shape and defensively copies collections. Cross-row, foreign-key, timestamp, and evidence correlation belongs in the Infrastructure assembler and should fail with an Infrastructure persistence-integrity exception, not a manufacturing Domain exception.

## 8. Structural Integrity Checks

### 8.1 Validate during rehydration

- Required row relationships and referenced AppUser/Recipe/Step/Material/Equipment records exist.
- All enum/database strings map through closed converters; unknown values fail.
- All OQ-3 technical identifiers are exact canonical lowercase GUID `D` values after conversion.
- Business IDs/codes reconstruct unchanged and fit approved persistence lengths.
- Aggregate members use the same Batch and applicable Recipe.
- Step IDs/numbers are unique, match RecipeStep, and have coherent status/timestamp/actor combinations.
- Pending executions have no execution times/completing actor; InProgress has StartedAt only; Completed has start, completion, and completing actor with nondecreasing times.
- Verification associations/results/actor/time match their audit evidence.
- Deviation state agrees with review comments, disposition, resolution time, origin execution, and lifecycle evidence.
- Signatures target an existing same-Batch BatchStepExecution and retain exact IDs/actor/meaning/time.
- Audit IDs are unique; required audit actions exist once where defined; row timestamps agree with event timestamps.
- Batch status agrees with start/completion evidence and terminal-state structure.

### 8.2 Do not execute during rehydration

- Do not call lifecycle transitions to prove the transition sequence again.
- Do not re-evaluate historical authorization or authenticated identity.
- Do not generate verification outcomes, signatures, deviations, timestamps, dispositions, or audit events.
- Do not apply current time or fallback identity values.
- Do not “repair” corrupt rows by dropping, merging, normalizing, or synthesizing evidence.

Corrupt/incomplete state should fail the repository load with a typed persistence-integrity exception containing safe diagnostic context. It should not return a partially reconstructed aggregate and should not mutate the database.

## 9. Infrastructure Persistence Representation Assessment

A uniform Infrastructure row model is preferable to mixed direct Domain mapping:

- it matches all 15 DBDS tables exactly, including surrogates and database-only fields;
- it prevents EF from invoking Domain creation constructors;
- it makes code/FK resolution and strict OQ-3 conversion explicit;
- it retains optional DB-only fields even when the Domain has no current counterpart;
- it supports controlled eager loading and deterministic assembly;
- it keeps EF navigation/configuration behavior outside Domain; and
- it supports a single scoped transaction through IApplicationUnitOfWork.

Expected Infrastructure responsibilities are row definitions, Fluent configurations, stable surrogate tracking, converters, explicit queries, assemblers, repository implementations, and a persistence-integrity exception. Database-only `LotNumber`, comments, `CalibrationValid`, `RecordedData`, and Batch FinalDisposition remain in row state and must not be overwritten merely because current Domain types do not expose them.

The DbContext should load rows explicitly/eagerly by use case. Lazy loading and EF proxies are unnecessary. Assemblers—not EF—call the restricted Domain factories after all relevant rows have been validated.

## 10. Existing Contract Compatibility

| Contract | Reconstruction capability | Assessment |
|---|---|---|
| `IProductionOrderRepository` | Load by business number, check uniqueness, add | Sufficient for ProductionOrder reconstruction; internal factory does not change signature |
| `IRecipeRepository` | Load by technical ID or business code | Sufficient for Recipe graph reconstruction |
| `IBatchRepository` | Load one execution aggregate by BatchNumber; add a new Batch | Sufficient to return the currently defined Batch-owned state and audit history |
| `IApplicationUnitOfWork` | Atomic commit of tracked work | Sufficient transaction boundary in principle |
| `IActorContext` | Trusted actor for new commands | Not used to replace historical actors during rehydration; existing contract remains correct |

No contract signature change is required solely to reconstruct and return the current Domain aggregates.

There is a separate future write/read concern: Batch does not own typed verification/signature collections, and explicit Infrastructure rows are not automatically tracked through Domain object mutation. If secured Application use cases must retrieve or persist those typed records independently, their precise use-case contract must be reviewed in the later Application/persistence design. This investigation does not justify a generic repository or a silent expansion of IBatchRepository.

## 11. Existing and Proposed Test Impact

### 11.1 Existing tests

The 159 accepted cases cover:

- constructor guards and initial states for all primary entities;
- ProductionOrder and Batch lifecycle transitions;
- Recipe ownership and ordered-step behavior;
- material/equipment verification evidence;
- deviation lifecycle and Batch exception handling;
- electronic-signature target, same-Batch, and immutability checks;
- audit creation/immutability and affected identity;
- completed-record protection; and
- identifier/value equality assumptions.

Option A preserves these public behaviors. Existing tests should remain unchanged except for the separately approved OQ-1 Batch constructor fixture update during coordinated implementation.

### 11.2 Future pure Domain tests

Tests within the Domain assembly are not currently a separate project. Rather than granting the test assembly friend access, verify post-rehydration behavior through assembler/repository tests. Continue using pure Domain tests for public creation and lifecycle behavior, especially that reconstructed Completed objects reject normal mutation once returned.

### 11.3 Future Infrastructure/integration tests

Add focused tests proving:

1. zero new AuditEvents and zero new ElectronicSignatures during load;
2. exact identity, actor, timestamp, details, and evidence counts before/after round trip;
3. all five Batch states and all execution/deviation states reconstruct;
4. ordered steps, verifications, deviations, signatures, and audit history retain associations;
5. Completed remains protected and nonterminal reconstructed aggregates continue enforcing public rules;
6. corrupt/missing/duplicate associations fail without partial aggregate return or database mutation;
7. OQ-1 quantity equals ProductionOrder.Quantity;
8. OQ-2 DeviationCode and stable DeviationId round-trip;
9. OQ-3 identifiers retain exact lowercase `D` form with no normalization;
10. OQ-3A maximum DBDS-valid composite value (180 units) round-trips without truncation, and values exceeding 256 are rejected;
11. signature target remains the same-Batch BatchStepExecution; and
12. one atomic command failure leaves no partial evidence rows.

SQL Server integration tests are required for actual column lengths, unique/FK constraints, restricted deletes, transactions, and Unicode storage. Assembler unit tests can use constructed row graphs without SQL Server for structural failure cases.

## 12. Approved OQ Compatibility Assessment

| Decision | Compatibility result |
|---|---|
| OQ-1 | Compatible: Batch factory receives authoritative order quantity from assembler and stores it as immutable derived state; no Batch quantity/UOM column or ProductionOrder navigation |
| OQ-2 | Compatible: row model retains DeviationId surrogate and DeviationCode; Domain factory receives unchanged code and history |
| OQ-3 | Compatible: row converter produces exact lowercase technical strings; factories preserve them; business IDs remain strings; signature resolver supports only BatchStepExecution |
| OQ-3A | Conditionally compatible: `nvarchar(256)` fits the 180-unit DBDS-valid generated composite, but unbounded Domain strings prevent an unconditional global maximum |

## 13. OQ-3A Maximum Audit Identifier Length Verification

### 13.1 Accepted generated formats

| AuditEventType | AffectedEntityId format | Bounded maximum under amended DBDS/OQ-3 |
|---|---|---:|
| BatchCreated | BatchNumber | 40 |
| BatchMarkedReady | BatchNumber | 40 |
| BatchProcessingStarted | BatchNumber | 40 |
| MaterialVerified | `BatchNumber:MaterialCode:OperatorId:{VerifiedAt:O}` | 180 |
| EquipmentVerified | `BatchNumber:EquipmentCode:OperatorId:{VerifiedAt:O}` | 180 |
| StepStarted | BatchStepExecutionId | 36 |
| StepCompleted | BatchStepExecutionId | 36 |
| ElectronicSignatureApplied | ElectronicSignatureId | 36 |
| DeviationOpened | DeviationCode | 40 |
| DeviationReviewStarted | DeviationCode | 40 |
| DeviationApproved | DeviationCode | 40 |
| DeviationRejected | DeviationCode | 40 |
| DeviationResolved | DeviationCode | 40 |
| BatchExceptionEntered | BatchNumber | 40 |
| BatchProcessingResumed | BatchNumber | 40 |
| BatchCompleted | BatchNumber | 40 |

### 13.2 Composite calculation

The verification format contains four components and three colon separators:

- BatchNumber: 40
- MaterialCode or EquipmentCode: 40
- OperatorId/AppUser.UserId: 64
- three separators: 3
- DateTimeOffset round-trip `O` text: 33 (`yyyy-MM-ddTHH:mm:ss.fffffffzzz`)

Maximum for a DBDS-valid composite: `40 + 1 + 40 + 1 + 64 + 1 + 33 = 180`.

The executed runtime check produced a 33-character maximum DateTimeOffset representation and a 180-character material/equipment composite. `nvarchar(256)` therefore leaves 76 UTF-16 storage units for values whose components already satisfy their DBDS columns. SQL Server `nvarchar(n)` capacity and .NET string length must be validated consistently for supplementary Unicode characters; integration tests should include both BMP and surrogate-pair input.

### 13.3 Conditional result

The Domain value objects for BatchNumber, MaterialCode, EquipmentCode, and OperatorId do not impose maximum lengths. The public AuditEvent constructor also accepts any nonblank affectedEntityId length. Therefore the accepted Domain API has no finite universal maximum.

**OQ-3A result:** `nvarchar(256)` is sufficient for every generated identifier whose component values are valid for the approved DBDS, with a proven maximum of 180. It cannot be confirmed for every value currently constructible in the Domain. `DBDS-AMD-002` remains conditionally approved until the Team confirms persistence-bound component-length validation as the supported limit. No automatic length increase or Domain audit-format change is recommended.

## 14. Alternative Approaches Comparison

| Criterion | A — restricted factories + rows + assemblers | B — direct EF Domain mapping | C — command/event replay | D — reflection/uninitialized |
|---|---|---|---|---|
| Clean Architecture | Strong; EF remains Infrastructure | Weaker hidden EF/materialization coupling | Business behavior used as persistence mechanism | Violates Domain ownership |
| Historical evidence | Exact values supplied once | Constructor/materializer conflicts likely | Regenerates IDs/events and timestamps | Can inject values but bypasses all protection |
| Invariant handling | Structural checks separated from future commands | EF may bypass or trigger inconsistent guards | Re-executes rules, not stored facts | No reliable checks |
| Public API impact | Internal only; one friend grant | Private-setter/constructor compromises likely | None initially but replay pipeline required | None visible, high runtime fragility |
| Existing tests | Public behavior preserved | Materialization changes may affect API | Behavior/version sensitive | Hard to test safely |
| Complexity | Explicit but bounded | Initially terse, later fragile | Highest sequencing/version complexity | Low code, unacceptable maintenance risk |
| Round-trip correctness | Deterministic and testable | Poor fit for side-effect constructors/collections | Cannot preserve exact evidence | Possible bitwise state, not trustworthy objects |

**Recommendation: Option A.**

## 15. Recommended Design and Exact Affected-File Inventory

### 15.1 Existing Domain files requiring coordinated modification

- `src/SyncBridge.Core.Domain/SyncBridge.Core.Domain.csproj` — add only the Infrastructure friend assembly declaration.
- `src/SyncBridge.Core.Domain/Entities/ProductionOrder.cs` — private full-state constructor and internal Rehydrate factory.
- `src/SyncBridge.Core.Domain/Entities/Batch.cs` — OQ-1 constructor correction plus full-state factory restoring status, executions, deviations, and canonical audit collection.
- `src/SyncBridge.Core.Domain/Entities/BatchStepExecution.cs` — restore status/timestamps/completing actor.
- `src/SyncBridge.Core.Domain/Entities/MaterialVerification.cs` — restore original AuditEvidence without generation.
- `src/SyncBridge.Core.Domain/Entities/EquipmentVerification.cs` — same.
- `src/SyncBridge.Core.Domain/Entities/Deviation.cs` — restore lifecycle fields, optional execution ID, and original audit list.
- `src/SyncBridge.Core.Domain/Entities/ElectronicSignature.cs` — restore immutable signature with original AuditEvidence.
- `src/SyncBridge.Core.Domain/ValueObjects/UnitOfMeasure.cs` — remove under approved OQ-1, not because of rehydration itself.

`Recipe.cs`, RecipeStep/requirement types, Material, Equipment, AuditEvent, current value-object identifier types, and enumerations do not require rehydration-specific changes.

### 15.2 Proposed Infrastructure files/groups

Exact filenames should follow the established namespaces when Task 2+ implementation is authorized:

- `Persistence/SyncBridgeDbContext.cs`.
- 15 `Persistence/Rows/*Row.cs` files matching the amended DBDS tables.
- 15 `Persistence/Configurations/*Configuration.cs` files.
- `Persistence/Conversion/TechnicalGuidConverter.cs` and explicit enum converters.
- `Persistence/Assembly/RecipeAssembler.cs`.
- `Persistence/Assembly/ProductionOrderAssembler.cs`.
- `Persistence/Assembly/BatchAggregateAssembler.cs`.
- `Persistence/Exceptions/PersistenceIntegrityException.cs`.
- Implementations of the three existing repositories and Application unit of work.

These are proposed future files, not created by this investigation.

### 15.3 Tests

- Preserve current test files, with only OQ-1 fixture changes when coordinated implementation occurs.
- Add Infrastructure assembler unit tests and SQL Server integration tests in the existing test project or the Team-approved persistence test project.
- Do not add a broad `InternalsVisibleTo` grant to the test assembly merely to call restricted factories directly.

## 16. Outstanding Risks and Team Approval Questions

1. Approve or reject entity-owned internal Rehydrate factories with a friend grant to `SyncBridge.Core.Infrastructure`.
2. Confirm that DBDS component-length validation defines supported persistence-bound identifiers, allowing `DBDS-AMD-002` to move from conditional to implementable; otherwise the audit length gate remains blocked.
3. Confirm that typed verifications/signatures need not become new Batch-owned public collections for this persistence increment.
4. Decide later use-case-specific write/read contracts for verification/signature rows; do not introduce a generic repository.
5. Approve the classification of cross-row corruption as an Infrastructure persistence-integrity error rather than a Domain manufacturing exception.
6. Confirm that database-only optional fields are preserved by row tracking but remain absent from Domain until a requirement adds them.

## 17. Proposed Implementation Acceptance Criteria

1. Public creation and lifecycle APIs retain accepted behavior and still generate evidence exactly once for new operations.
2. Loading any persisted aggregate creates zero new AuditEvents, ElectronicSignatures, verifications, deviations, or transitions.
3. Every stored identifier, actor, timestamp, enum, result, status, meaning, detail, and association round-trips unchanged.
4. All five Batch states and every supported execution/deviation state reconstruct without command replay.
5. Batch quantity equals the associated ProductionOrder quantity; UnitOfMeasure is absent; no Batch quantity/unit column exists.
6. DeviationCode and stable DeviationId mapping follow `DBDS-AMD-001`.
7. Technical strings follow exact OQ-3 canonical form and are never normalized or replaced.
8. Only BatchStepExecution signatures load; target existence and same-Batch ownership are validated.
9. Audit events are unique by AuditEventId, append-only, correctly partitioned between Batch and Deviation ownership, and never duplicated in `Batch.AuditEvents`.
10. Batch row timestamps match authoritative audit events; missing/conflicting evidence fails safely.
11. Maximum DBDS-valid 180-unit verification affected IDs round-trip intact through `nvarchar(256)`; over-capacity values fail before commit.
12. Corrupt rows fail with no partial aggregate and no database mutation.
13. Completed reconstructed records remain protected; eligible nonterminal aggregates continue enforcing current Domain rules.
14. Existing repository contracts remain unchanged unless a later Team-approved use case demonstrates a missing capability.
15. Domain/Application remain free of EF Core/SQL dependencies; Infrastructure contains rows, mappings, assemblers, and repositories.
16. The database contains exactly 15 tables with only approved amendments.
17. All 159 accepted tests pass, plus new assembler/integration tests for every criterion above.

**Recommendation status: READY FOR TEAM REVIEW. OQ-4 and DBDS-AMD-002 are not marked fully approved by this report, and no persistence implementation is authorized.**
