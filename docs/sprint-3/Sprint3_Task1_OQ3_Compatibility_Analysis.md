# WP-6 Sprint 3 Task 1 — OQ-3 Technical Identifier Compatibility Analysis

## 1. Executive Summary

**Investigation result: READY FOR TEAM REVIEW. OQ-3 is not approved, and persistence implementation remains blocked by OQ-4.**

The provisional Option A is compatible with the accepted Domain model, Application contracts, approved OQ-1 decision, and amended 15-table DBDS when applied only to persistence-bound technical identifiers. The smallest sound approach is:

- retain the existing C# identifier types;
- generate canonical lowercase GUID `D` text for new technical string identities;
- validate and convert those strings at the Infrastructure persistence boundary;
- map native `Guid` value objects directly;
- preserve business identifiers as human-readable strings; and
- interpret `ElectronicSignature.RelatedRecordId` according to `RelatedRecordType`, rather than treating every related-record value as a directly parseable GUID.

The accepted signature gate currently supports only `RelatedRecordType.BatchStepExecution`. That case is reversible because `RelatedRecordId` equals the execution's technical `BatchStepExecutionId`, which maps directly to `StepExecutionId uniqueidentifier`. Batch, Deviation, RecipeStep, and verification signature targets are examples in DBDS §5.13, not implemented signature behaviors. They must not be enabled implicitly during persistence work.

Two matters remain outside an OQ-3 approval:

1. OQ-4 must define safe aggregate rehydration without replaying behavior or creating new audit/signature evidence.
2. Current material/equipment verification audit display identifiers can exceed the DBDS `AuditEvent.AffectedEntityId nvarchar(80)` limit. That pre-existing length mismatch requires a separate Team disposition; values must not be truncated or replaced silently.

## 2. Provisional Team Decision and Scope

### 2.1 Provisional OQ-3 decision

Option A proposes canonical GUID text for persistence-bound technical string identifiers while retaining human-readable business identifiers. Invalid technical identifiers may not be hashed, replaced, or silently assigned unrelated GUIDs.

This report validates that approach. It does not change identifier types, validation, constructors, tests, mappings, packages, or databases.

### 2.2 Approved decisions incorporated

- **OQ-1:** `ProductionOrder.Quantity` is authoritative; `Batch.PlannedQuantity` is derived; Batch unit-of-measure state will be removed in coordinated implementation. Identifier strategy does not affect that decision.
- **OQ-2:** `DeviationCode` remains human-readable. The Deviation table retains `DeviationId uniqueidentifier` and adds `DeviationCode nvarchar(40) NOT NULL` with `UQ_Deviation_DeviationCode` under `DBDS-AMD-001`. No mapping table is authorized.
- **OQ-4:** Domain aggregate rehydration remains unresolved and is not implemented or resolved here.

### 2.3 Terminology used in this report

- A **technical identifier** identifies a persisted row or Domain object independently of a manufacturing-facing code.
- A **business identifier** is meaningful in manufacturing workflows and remains human-readable.
- A **contextual reference** such as `RelatedRecordId` can contain either kind of identifier depending on its discriminator. It is not inherently a GUID.
- An **Infrastructure surrogate** is a database technical key with no corresponding Domain identifier.

## 3. Source Inspection Register

### 3.1 Repository evidence

| Area | Files inspected | Verified facts |
|---|---|---|
| Identifier value objects | All files under `src/SyncBridge.Core.Domain/ValueObjects` relevant to identity | Six technical identifiers are string-backed; three are `Guid`-backed; business codes/numbers are string-backed; most positional records perform no value validation |
| Aggregate/entities | `Recipe.cs`, `RecipeStep.cs`, `ProductionOrder.cs`, `Batch.cs`, `BatchStepExecution.cs`, `Deviation.cs`, `ElectronicSignature.cs`, `AuditEvent.cs`, material/equipment and verification entities | Actual assignment, generation, equality, audit, and signature-gate behavior |
| Application contracts | `IRecipeRepository.cs`, `IProductionOrderRepository.cs`, `IBatchRepository.cs`, `IApplicationUnitOfWork.cs`, `IActorContext.cs` | Contracts use Domain identifiers and do not expose database surrogates or conversion concerns |
| Tests | All identifier usages under `tests/SyncBridge.Core.Tests` | Pure Domain fixtures commonly use readable non-GUID technical strings; native GUID identities already use `Guid` values |
| Existing Task 1 evidence | Persistence mapping design gate, completion report, and OQ-1 reports | Original OQ-3 proposal, approved OQ-1 input, and unresolved rehydration dependency |

### 3.2 Engineering documents

| Document | Relevant evidence |
|---|---|
| Team-reviewed Sprint 2-to-3 handoff | Domain behavior must remain stable; string-valued IDs mapped to database GUIDs require a reviewed conversion/surrogate strategy; persistence reconstructs state and remains in Infrastructure |
| Revised `Indy-1-SyncBridge-DBDS_v2.docx` | Exact 15-table schema; `uniqueidentifier` technical columns; string business columns; §5.13 signature representation; §5.14 audit display identity; Appendix B / `DBDS-AMD-001` |
| Corrected SDD | Domain remains technology-independent; Application coordinates; Infrastructure implements persistence and conversions behind inner-layer contracts |
| Corrected SRS | FR-007 requires permanent association with the controlled record; FR-008 preserves affected entity identity for immutable audit history |
| Realigned WP-6 implementation plan | Round-trip identity references and signature links; no new tables; exact DBDS types and relationships; Domain signature/value-object representation must map faithfully |

The revised DBDS was available at `C:\Engineering\Golden_Documents\Indy-1-SyncBridge-DBDS_v2.docx`. Appendix B explicitly contains `DBDS-AMD-001`; this report does not rely on an unavailable revision.

## 4. Complete Identifier Inventory

### 4.1 Domain identifiers and codes

| Identifier | Domain representation and classification | Construction/assignment and validation | DBDS representation | Option A compatibility | Current files/tests affected by a validation-policy change |
|---|---|---|---|---|---|
| `RecipeId` | `sealed record RecipeId(string Value)`; technical | Caller supplies value to Recipe, RecipeStep, requirements, and ProductionOrder; only wrapper null checks, no blank/GUID validation | `Recipe.RecipeId uniqueidentifier`; FKs in RecipeStep, requirements, ProductionOrder | Compatible through strict Infrastructure conversion | VO; Recipe/RecipeStep/requirements/ProductionOrder; `RecipeTests`, `RecipeStepTests`, `RecipeRequirementTests`, `SprintClosureModelTests`, and Batch fixture files |
| `RecipeStepId` | String record; technical | Caller supplies to RecipeStep; copied to BatchStepExecution and compared by value; no value-format validation | `RecipeStep.RecipeStepId uniqueidentifier`; execution FK | Compatible through strict Infrastructure conversion | VO; RecipeStep, Batch, BatchStepExecution; recipe/step, deviation, signature, audit, and ordered-workflow tests |
| `BatchStepExecutionId` | String record; technical | Caller supplies through `Batch.CreateStepExecution`; no value-format validation | `BatchStepExecution.StepExecutionId uniqueidentifier`; Deviation optional FK; supported signature related ID | Compatible; use the same canonical string as the row GUID | VO; Batch, BatchStepExecution, Deviation; Batch execution/deviation/signature/audit tests |
| `ElectronicSignatureId` | String record; technical | Caller supplies to public ElectronicSignature constructor; wrapper null checked, value format not checked | `ElectronicSignature.SignatureId uniqueidentifier` | Compatible through strict Infrastructure conversion | VO and ElectronicSignature; `ElectronicSignatureTests`, `ElectronicSignatureGateTests`, `BatchAuditEvidenceTests` |
| `AuditEventId` | String record; technical | Public constructor accepts any nonblank string; internal `AuditEvent.Create` generates `Guid.NewGuid().ToString("D")` | `AuditEvent.AuditEventId uniqueidentifier` | Internally generated events are already compatible; externally reconstructed/constructed values need boundary validation | VO and AuditEvent; `AuditEventTests` uses `AUD-001` in pure Domain tests |
| `RelatedRecordId` | String record; contextual reference | Caller supplies; no value validation; interpretation depends on `RelatedRecordType` | `ElectronicSignature.RelatedRecordId uniqueidentifier` | Compatible only with a type-specific mapping; current BatchStepExecution use is directly GUID-compatible | VO, ElectronicSignature, Batch; three signature-related test files |
| `ProductionOrderId` | `sealed record ProductionOrderId(Guid Value)`; technical | Caller supplies; ProductionOrder rejects `Guid.Empty` | `ProductionOrder.OrderId uniqueidentifier` | Direct mapping; no string conversion | VO, ProductionOrder, `SprintClosureModelTests` |
| `MaterialId` | Guid record; technical | Caller supplies; Material rejects `Guid.Empty` | `Material.MaterialId uniqueidentifier` | Direct mapping | VO, Material, `SprintClosureModelTests` |
| `EquipmentId` | Guid record; technical | Caller supplies; Equipment rejects `Guid.Empty` | `Equipment.EquipmentId uniqueidentifier` | Direct mapping | VO, Equipment, `SprintClosureModelTests` |
| `BatchNumber` | String record; business | Caller supplies; consuming entities null-check wrapper but do not validate Value | `Batch.BatchNumber nvarchar(40)`; database `BatchId` is an Infrastructure surrogate | Preserve unchanged; resolve number to BatchId for FKs and reverse through unique number | Widely used across Batch/evidence/exceptions/contracts and Batch tests; no GUID conversion |
| `ProductionOrderNumber` | String record; business | Caller supplies; entity/Batch null-check wrapper | `ProductionOrder.OrderNumber nvarchar(40)`; technical `OrderId` comes from `ProductionOrderId` | Preserve unchanged; repository lookup remains by number | VO, ProductionOrder, Batch, exception, repository contract, order/Batch tests |
| `DeviationCode` | String record; business | Caller supplies; Deviation null-checks wrapper and uses it for equality/audit | Amended `Deviation.DeviationCode nvarchar(40)` unique; `DeviationId uniqueidentifier` Infrastructure surrogate | Compatible through explicit code-to-surrogate resolution under OQ-2 | VO, Deviation, Batch, deviation exception and tests; no GUID conversion |
| `RecipeCode` | String record; business | Caller supplies; Recipe/Batch null-check wrapper and compare by value | `Recipe.RecipeCode nvarchar(40)` unique | Preserve unchanged; resolve to RecipeId only where a DB FK requires it | VO, Recipe, Batch, repository contract and recipe/Batch tests |
| `MaterialCode` | String record; business | Caller supplies; material, requirements, verification null-check wrapper | `Material.MaterialCode nvarchar(40)` unique | Preserve; resolve to MaterialId for requirement/verification FKs | VO, Material, requirement/verification entities and associated tests |
| `EquipmentCode` | String record; business | Caller supplies; equipment, requirements, verification null-check wrapper | `Equipment.EquipmentCode nvarchar(40)` unique | Preserve; resolve to EquipmentId for requirement/verification FKs | VO, Equipment, requirement/verification entities and associated tests |
| `OperatorId` | String record; accountable user/business reference | Caller or `IActorContext` supplies; consuming entities null-check wrapper, not Value | `AppUser.UserId nvarchar(64)` and evidence FKs | Preserve unchanged; never parse as GUID | VO, actor contract, all evidence entities and many tests |
| `ProductCode` | String record; business | Caller supplies to ProductionOrder; wrapper null checked | `ProductionOrder.ProductCode nvarchar(40)` | Preserve unchanged | VO, ProductionOrder, `SprintClosureModelTests` |
| `RelatedRecordType` | Validated string discriminator, not an identifier | Public constructor rejects blank; only built-in constant is `BatchStepExecution` | `ElectronicSignature.RelatedRecordType nvarchar(40)` | Preserve exact stable discriminator; do not infer aliases | VO, ElectronicSignature, Batch, signature tests |

### 4.2 Infrastructure-only technical keys

| Database key | Domain counterpart | Required strategy |
|---|---|---|
| `Batch.BatchId` | None; Domain uses `BatchNumber` | Generate once on insert; resolve/recover by unique BatchNumber; retain key for all children |
| `MaterialVerification.VerificationId` | None; Domain identity is the Batch/material association plus evidence fields | Generate once; resolve current row through unique `(BatchId, MaterialId)` |
| `EquipmentVerification.VerificationId` | None; Domain identity is the Batch/equipment association plus evidence fields | Generate once; resolve current row through unique `(BatchId, EquipmentId)` |
| `Deviation.DeviationId` | None; Domain identity is `DeviationCode` | Generate once; resolve bidirectionally through amended unique DeviationCode |

These surrogates must not leak into Domain value objects merely because SQL Server uses GUID primary keys.

## 5. GUID Generation and Validation Findings

### 5.1 Current generation paths

- `AuditEvent.Create` is the only accepted Domain path that generates a technical string ID. It already uses `Guid.NewGuid().ToString("D")`.
- Recipe, RecipeStep, BatchStepExecution, and ElectronicSignature technical IDs are supplied by callers.
- ProductionOrder, Material, and Equipment technical IDs are supplied as native `Guid` values and reject `Guid.Empty` at their entity constructors.
- Existing test fixtures intentionally use values such as `RECIPE-ID-001`, `STEP-001`, `EXEC-001`, `SIG-001`, and `AUD-001`.

### 5.2 Recommended canonical rule

Infrastructure should accept a persistence-bound technical string only when both conditions are true:

1. `Guid.TryParseExact(value, "D", out var parsed)` succeeds.
2. `string.Equals(value, parsed.ToString("D"), StringComparison.Ordinal)` succeeds.

The second check establishes one canonical lowercase representation. `TryParseExact("D")` by itself accepts uppercase hexadecimal; reading the SQL GUID back with `ToString("D")` would then change the Domain string's casing and could break the current case-sensitive record equality. The combined rule guarantees stable string equality across a round trip.

On read, Infrastructure reconstructs the technical string with `databaseGuid.ToString("D")`. It must throw a typed mapping/input-integrity failure for invalid persistence-bound values. It must never hash, replace, or regenerate a supplied identity.

### 5.3 Validation placement

- **Domain value objects:** Do not add GUID-format validation under Option A. The Domain is technology-independent, accepted pure Domain tests use readable technical fixtures, and several objects can exist without persistence.
- **Application orchestration:** Generate new persistence-bound technical IDs in canonical lowercase `D` form and do not accept them as user-controlled manufacturing input. A small ID-generation abstraction is optional only if required for deterministic application tests; no current contract mandates one.
- **Infrastructure mapping:** Enforce the canonical rule before converting to `uniqueidentifier`. This is the mandatory final boundary because the compatibility constraint arises from the approved SQL schema.

A permissive `Guid.TryParse` followed by normalization is not recommended. It would accept `N`, `B`, `P`, whitespace-tolerant, or uppercase representations and silently change the Domain value during round trip.

### 5.4 Existing test compatibility

Non-GUID technical fixtures can remain valid in pure Domain tests because no Domain validation changes are recommended. Persistence integration tests must use canonical GUID-compatible fixtures and must include rejection cases for noncanonical inputs.

## 6. Technical Identifier Conversion Matrix

| Domain value | SQL target | Write conversion | Read conversion | Round-trip result |
|---|---|---|---|---|
| `RecipeId.Value` | `RecipeId uniqueidentifier` | Strict canonical parse | `ToString("D")` | Reversible |
| `RecipeStepId.Value` | `RecipeStepId uniqueidentifier` | Strict canonical parse | `ToString("D")` | Reversible |
| `BatchStepExecutionId.Value` | `StepExecutionId uniqueidentifier` | Strict canonical parse | `ToString("D")` | Reversible |
| `ElectronicSignatureId.Value` | `SignatureId uniqueidentifier` | Strict canonical parse | `ToString("D")` | Reversible |
| `AuditEventId.Value` | `AuditEventId uniqueidentifier` | Strict canonical parse; internally generated values already conform | `ToString("D")` | Reversible |
| `ProductionOrderId.Value` | `OrderId uniqueidentifier` | Direct Guid | Direct Guid | Reversible |
| `MaterialId.Value` | `MaterialId uniqueidentifier` | Direct Guid | Direct Guid | Reversible |
| `EquipmentId.Value` | `EquipmentId uniqueidentifier` | Direct Guid | Direct Guid | Reversible |
| `RelatedRecordId.Value` for BatchStepExecution | `RelatedRecordId uniqueidentifier` | Strict canonical parse after validating discriminator and same-Batch target | `ToString("D")` | Reversible for current supported behavior |
| Business number/code | Corresponding `nvarchar` column | Store unchanged after length/required validation | Reconstruct unchanged | Reversible; no GUID conversion |
| Business number/code used for FK | Related `uniqueidentifier` FK | Resolve through unique business column | Join technical row and restore business value | Reversible when the unique relationship exists |

## 7. Electronic Signature Related-Record Mapping Matrix

### 7.1 Accepted behavior

`ElectronicSignature` can be constructed with any non-null `RelatedRecordType`/`RelatedRecordId`, but the only Domain operation that consumes and validates a signature is `Batch.CompleteStep`. It requires:

- the signature's `BatchNumber` to equal the aggregate Batch number;
- `RelatedRecordType` to equal the exact `BatchStepExecution` constant; and
- `RelatedRecordId.Value` to equal `BatchStepExecutionId.Value`.

Therefore, only BatchStepExecution is a supported related-record target in the accepted implementation. Public constructability of other values does not establish supported behavior.

### 7.2 Current and potential target analysis

| Related record | Accepted Domain support | Domain identity | Database identity | Resolution and reverse mapping | Same-Batch implication | Status |
|---|---|---|---|---|---|---|
| BatchStepExecution | Yes; required-signature step completion | `BatchStepExecutionId` technical string | `StepExecutionId uniqueidentifier` | Direct strict parse/write and `D` read | Query/validate the execution's `BatchId` equals signature `BatchId`; Domain also validates BatchNumber | Fully compatible with Option A |
| Batch | No current signature behavior/type constant | `BatchNumber` business ID | `BatchId uniqueidentifier` surrogate | Resolve unique BatchNumber to BatchId; reverse by BatchId to BatchNumber | Trivially same Batch when both IDs resolve to same row | Technically reversible but not authorized behavior |
| Deviation | No current signature behavior/type constant | `DeviationCode` business ID | `DeviationId uniqueidentifier` surrogate plus amended unique `DeviationCode` | Resolve code to DeviationId; reverse through `DBDS-AMD-001` column | Validate Deviation.BatchId equals signature BatchId | Reversible after OQ-2, but not authorized behavior |
| RecipeStep | No current signature behavior/type constant | `RecipeStepId` technical string | `RecipeStepId uniqueidentifier` | Direct strict conversion | A recipe definition is not Batch-owned; same-Batch relevance would require the Batch's order/recipe and execution context | Mapping possible, behavior unresolved/not authorized |
| MaterialVerification | No current signature behavior/type constant | Composite Domain association/evidence; no technical Domain ID | `VerificationId uniqueidentifier` surrogate | No accepted serialization of the composite into `RelatedRecordId`; a surrogate cannot reconstruct the original string faithfully | Must validate verification BatchId | Unresolved; do not implement |
| EquipmentVerification | No current signature behavior/type constant | Composite Domain association/evidence; no technical Domain ID | `VerificationId uniqueidentifier` surrogate | Same unresolved issue as material verification | Must validate verification BatchId | Unresolved; do not implement |

DBDS §5.13's phrase “such as Batch, Step, Verification, or Deviation” describes schema capacity, not the accepted Domain target set. The initial persistence implementation should store only the exact `BatchStepExecution` discriminator until a later approved use case adds another target and its mapping rules.

### 7.3 Reconstruction and OQ-4

For current signatures, Infrastructure can reconstruct `RelatedRecordId` from `ElectronicSignature.RelatedRecordId.ToString("D")` and verify that the referenced StepExecution row belongs to the signature's Batch row. Reconstructing the immutable ElectronicSignature and attaching it to the Batch without creating duplicate audit evidence depends on the non-public rehydration mechanism still awaiting OQ-4 resolution.

## 8. Batch, Deviation, Step, and Verification References

- **Batch:** Domain consistently uses human-readable `BatchNumber`; Infrastructure owns `BatchId`. All child FKs resolve through the unique Batch number. Audit display remains the Batch number.
- **Deviation:** Domain and audit evidence use human-readable `DeviationCode`. Under `DBDS-AMD-001`, Infrastructure retains a separate DeviationId and resolves/reconstructs through the unique code. OQ-3 must not reinterpret the code as a GUID.
- **RecipeStep:** `RecipeStepId` is technical and can use canonical GUID text. `RecipeCode` remains the human-readable recipe identity used by Batch; joins may resolve it to RecipeId.
- **BatchStepExecution:** Its technical string ID maps directly to StepExecutionId. This direct equivalence is what makes the current electronic-signature relationship reversible.
- **Material and Equipment:** Domain references use business codes; persistence resolves them to native MaterialId/EquipmentId rows through unique code columns.
- **Verifications:** Domain has no dedicated technical ID; Infrastructure owns VerificationId. The current one-row-per-Batch/item constraints permit persistence reconstruction, but no canonical single-string Domain identity exists for using those rows as electronic-signature targets.

## 9. Audit Identifier Compatibility

`AuditEventId` and `AffectedEntityId` serve different purposes:

- `AuditEventId` is the technical event identity and maps to `uniqueidentifier`.
- `AffectedEntity` is a type/display discriminator.
- `AffectedEntityId` is the record identity serialized for audit display and maps to `nvarchar(80)`.

Current evidence uses:

| Affected entity | Current `AffectedEntityId` value | Required treatment |
|---|---|---|
| Batch | Human-readable `BatchNumber` | Preserve unchanged |
| BatchStepExecution | Technical `BatchStepExecutionId` | Canonical GUID text for persisted executions |
| Deviation | Human-readable `DeviationCode` | Preserve unchanged under OQ-2 |
| MaterialVerification | `BatchNumber:MaterialCode:OperatorId:timestamp` composite | Preserve semantics; do not convert to GUID |
| EquipmentVerification | `BatchNumber:EquipmentCode:OperatorId:timestamp` composite | Preserve semantics; do not convert to GUID |
| ElectronicSignature | Technical `ElectronicSignatureId` | Canonical GUID text for persisted signatures |

No rule should convert every `AffectedEntityId` to a GUID. Doing so would discard the accepted human-readable Batch, Deviation, and verification evidence.

**Unresolved length risk:** the verification composite can exceed 80 characters using approved DBDS maximum lengths (up to 40 + 40 + 64 characters, separators, and an ISO timestamp). Infrastructure must not truncate it. The Team must approve a bounded audit-display identity representation or a controlled schema amendment before such maximum-length evidence is persisted. This report does not select or implement one.

## 10. Existing Test Impact Matrix

| Test area | Current fixture behavior | Option A impact |
|---|---|---|
| Recipe and RecipeStep tests | Use readable `RECIPE-ID-*` and `STEP-*` values | Remain unchanged as pure Domain tests |
| Batch lifecycle, prerequisite, ordered execution, deviation, and protection tests | Use readable Recipe/Step/Execution identifiers | Remain unchanged as pure Domain tests |
| `ElectronicSignatureTests` | Uses `SIG-001` and `EXEC-001`; tests constructor preservation and guards | Remains unchanged; no Domain GUID validation recommended |
| `ElectronicSignatureGateTests` | Verifies same Batch, exact target type, exact execution ID, immutable evidence | Remains unchanged; add later persistence round-trip coverage with GUID fixtures |
| `BatchAuditEvidenceTests` | Verifies execution/signature IDs are preserved in audit display | Remains unchanged; add later canonical-GUID and business-ID audit assertions |
| `AuditEventTests` | Public constructor accepts `AUD-001`; generated-event tests observe behavior | Remains unchanged; Infrastructure tests reject noncanonical persisted technical IDs |
| `SprintClosureModelTests` | Native ProductionOrder/Material/Equipment GUID identities | Already compatible |
| Future persistence integration tests | Not present | Must use lowercase `D` fixtures and test valid round trips, noncanonical rejection, no ID replacement, signature target ownership, OQ-2 deviation resolution, and surrogate stability |

If GUID validation were moved into the Domain value objects (Option B-like behavior), numerous accepted fixtures across at least Recipe, RecipeStep, Batch execution, deviation, signature, and audit tests would require mechanical changes and the accepted arbitrary-string Domain behavior would change. Option A avoids that regression.

## 11. Options Comparison

| Criterion | Option A — canonical GUID technical strings | Option B — strongly typed GUID Domain IDs | Option C — SQL string technical IDs |
|---|---|---|---|
| Requirements compliance | Satisfies identity preservation with type-specific references | Can satisfy requirements but changes accepted Domain API | Can satisfy persistence but amends approved schema broadly |
| Persistence reversibility | Yes with strict canonical rule | Yes directly | Yes directly |
| Domain behavior preservation | Highest; no existing Domain validation change required | Low; constructors/types/equality and fixtures change | Domain preserved, DBDS not preserved |
| Existing tests | Pure Domain fixtures remain valid | Many fixtures and signatures change | Mostly unchanged |
| Electronic signatures | Current execution target maps directly; business/surrogate targets require explicit resolver | Still needs contextual resolver for business/surrogate targets | Still needs contextual target semantics despite string storage |
| Aggregate reconstruction | Compatible but still dependent on OQ-4 | Still dependent on OQ-4 | Still dependent on OQ-4 |
| Database impact | None beyond already approved OQ-2 amendment | None necessarily | Changes multiple `uniqueidentifier` columns/FKs/indexes |
| Complexity | Small, localized converters/resolvers | Broad Domain and test refactor | Broad DBDS/migration/referential change |
| Maintainability | Clear when conversion is centralized and discriminator mappings are closed | Strong compile-time types but disproportionate current cost | Weakens database GUID consistency and diverges from baseline |

**Recommendation: Option A.** No exception is required for currently supported signature behavior. The narrow rule for generic references is that `RelatedRecordId` must be mapped by its exact `RelatedRecordType`; direct GUID parsing is permitted only for a target whose Domain identity is itself a canonical technical GUID string.

## 12. Recommended OQ-3 Resolution

The Team should approve the following bounded decision:

1. Keep existing Domain identifier types.
2. Define persistence-bound technical string values as exact lowercase GUID `D` strings.
3. Generate new technical IDs in Application orchestration (or the existing Domain factory for AuditEvent) and validate again in Infrastructure.
4. Parse/write and format/read through one centralized Infrastructure converter.
5. Reject invalid/noncanonical values before commit with no replacement, hashing, or normalization.
6. Preserve all business numbers/codes and `OperatorId` unchanged in their approved `nvarchar` columns.
7. Treat `RelatedRecordId` as a discriminated contextual reference. Initially support only `BatchStepExecution`, matching accepted Domain behavior.
8. Preserve audit display IDs according to the affected entity; do not apply blanket GUID conversion.
9. Do not enable Batch, Deviation, RecipeStep, or verification signature targets without a separately approved Domain use case and mapping rule.
10. Keep persistence implementation blocked until OQ-4 defines aggregate rehydration.

## 13. Required Eventual Implementation Changes by Layer

### Domain

- No identifier type or GUID-format validation change is required for Option A.
- Preserve current signature gate and exact `BatchStepExecution` discriminator.
- Any change to audit verification display identity depends on a separate approved resolution of the 80-character mismatch.
- OQ-1 removal of Batch unit-of-measure remains a coordinated, separate approved change.

### Application

- Future creation services should generate Recipe, RecipeStep, BatchStepExecution, and ElectronicSignature technical IDs as lowercase GUID `D` strings rather than accepting them from users.
- Existing repository and actor contracts do not require identifier-signature changes.
- If deterministic ID generation is needed for service tests, introduce only the smallest inner-layer abstraction approved with those services.

### Infrastructure

- Centralize strict technical-string-to-Guid conversion and canonical formatting.
- Map native Guid identifiers directly.
- Resolve business identifiers through approved unique columns.
- Generate and retain Batch/verification/deviation surrogates according to the design gate and OQ-2.
- Implement a closed signature related-record resolver keyed by exact type; initially register only BatchStepExecution.
- Validate the signature target belongs to the same Batch before commit and during integrity-checked reconstruction.
- Preserve `AuditEvent.AffectedEntityId` as display text.
- Await OQ-4 before implementing aggregate materialization.

### Tests

- Preserve all existing pure Domain tests and readable fixtures.
- Add converter unit tests for exact lowercase D acceptance, uppercase/N/B/P/blank/non-GUID rejection, and stable round trip.
- Add repository integration fixtures using canonical GUID strings.
- Add signature persistence tests proving SignatureId and StepExecution related ID preservation, same-Batch enforcement, and rejection of unsupported type mappings.
- Add business-ID tests for BatchNumber, DeviationCode, RecipeCode, MaterialCode, EquipmentCode, ProductionOrderNumber, ProductCode, and OperatorId.
- Add surrogate stability tests and OQ-2 DeviationCode/DeviationId resolution tests.

## 14. Risks, Unresolved Questions, and OQ-4 Dependencies

| Item | Effect | Disposition |
|---|---|---|
| OQ-4 rehydration unresolved | Cannot safely materialize Batch/signature/audit collections without replay or duplicate evidence | Blocking dependency for persistence implementation; not resolved here |
| Verification `AffectedEntityId` may exceed 80 | Valid Domain evidence may not fit approved audit column | Team decision required; no truncation permitted |
| Public Domain constructors accept non-GUID technical strings | A persist attempt may fail late if Application generation is bypassed | Application generates; Infrastructure validates authoritatively |
| Uppercase D strings are parseable but not canonically stable under current string equality | Normalization could change identity | Require exact lowercase `ToString("D")` representation |
| DBDS lists broader signature examples than Domain supports | Generic mapping could accidentally expand behavior | Closed resolver; support BatchStepExecution only |
| Verification signature target lacks a reversible Domain single ID | Surrogate GUID cannot recreate an arbitrary RelatedRecordId string | Mark unsupported until separately designed/approved |
| RelatedRecordId has no database FK | Database alone cannot enforce target type/existence/same-Batch ownership | Explicit Infrastructure validation plus Domain gate; integration tests |

## 15. Proposed Acceptance Criteria for Eventual Implementation

1. All persistence-bound RecipeId, RecipeStepId, BatchStepExecutionId, ElectronicSignatureId, AuditEventId, and supported technical RelatedRecordId values are exact lowercase GUID `D` strings.
2. Noncanonical or invalid technical strings fail before commit and are never replaced, hashed, or normalized silently.
3. Native ProductionOrderId, MaterialId, and EquipmentId GUIDs round-trip unchanged and reject empty values under existing Domain behavior.
4. All listed business identifiers round-trip character-for-character through their approved string columns.
5. DeviationCode resolves to a stable DeviationId and reconstructs unchanged using `DBDS-AMD-001`.
6. BatchId and verification surrogate IDs remain stable across updates/reloads and do not enter the Domain API.
7. ElectronicSignature supports only BatchStepExecution until another type is approved.
8. SignatureId, RelatedRecordType, RelatedRecordId, Batch association, actor, meaning, and timestamp round-trip unchanged.
9. A signature related execution must exist and belong to the same Batch; mismatches and unsupported types are rejected.
10. AuditEventId maps as a technical GUID, while AffectedEntityId preserves the accepted entity-specific human-readable/technical display value.
11. No audit display value is silently truncated; the verification display-ID length issue is resolved before affected persistence tests pass.
12. Existing 159 Domain tests remain green and unchanged unless a separately approved change requires an update.
13. New converter and repository integration tests cover canonical validation, resolution, round-trip identity, and failure atomicity.
14. The amended schema still contains exactly 15 tables and only the approved DeviationCode column/constraint amendment.
15. Domain and Application remain free of EF Core, SQL Server, and Infrastructure dependencies.

**Recommendation status: READY FOR TEAM REVIEW. OQ-3 is not approved. Persistence implementation must not begin until the Team records the OQ-3 decision and resolves OQ-4.**
