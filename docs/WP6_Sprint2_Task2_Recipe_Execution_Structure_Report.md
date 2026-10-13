# WP-6 Sprint 2 Task 2 — Recipe Execution Structure Report

## Outcome

Task 2 is implemented and validated. SyncBridge Core now has a technology-independent Recipe execution-definition model with explicit Recipe and RecipeStep identities, ordered protected steps, material requirements, equipment requirements, and approval/signature-definition terminology aligned to the approved baseline.

## Files Changed

### Modified

- `src/SyncBridge.Core.Domain/Entities/Recipe.cs`
- `docs/testing/Test_Evidence_Register.md`

### Added

- `src/SyncBridge.Core.Domain/Entities/RecipeStep.cs`
- `src/SyncBridge.Core.Domain/Entities/RecipeMaterialRequirement.cs`
- `src/SyncBridge.Core.Domain/Entities/RecipeEquipmentRequirement.cs`
- `src/SyncBridge.Core.Domain/ValueObjects/RecipeId.cs`
- `src/SyncBridge.Core.Domain/ValueObjects/RecipeStepId.cs`
- `tests/SyncBridge.Core.Tests/RecipeTests.cs`
- `tests/SyncBridge.Core.Tests/RecipeStepTests.cs`
- `tests/SyncBridge.Core.Tests/RecipeRequirementTests.cs`
- `artifacts/test-evidence/sprint-2/task-2-recipe-execution-structure/evidence.md`
- `artifacts/test-evidence/sprint-2/task-2-recipe-execution-structure/build.log`
- `artifacts/test-evidence/sprint-2/task-2-recipe-execution-structure/test-results.trx`
- `docs/WP6_Sprint2_Task2_Recipe_Execution_Structure_Report.md`

No Batch, ProductionOrder, Application, Infrastructure, Razor Pages, project, or persistence file was modified.

## Types Added

- `RecipeStep`
- `RecipeMaterialRequirement`
- `RecipeEquipmentRequirement`
- `RecipeId`
- `RecipeStepId`

## Existing Types Modified

`Recipe` was realigned from a passive product-associated record to the approved Recipe definition model. It now owns its definition collections and their definition-level invariants.

## Final Recipe Structure

`Recipe` contains:

- `RecipeId` — independent Recipe identity;
- `RecipeCode` — business code;
- `Name` — required nonblank name;
- `Version` — required nonblank version;
- `IsApproved` — approval status;
- protected read-only views of ordered `Steps`, `MaterialRequirements`, and `EquipmentRequirements`;
- `AddStep`, `AddMaterialRequirement`, and `AddEquipmentRequirement` methods.

Collection mutation remains controlled by Recipe. Returned collection views are arrays exposed through `IReadOnlyList<T>`, so callers cannot mutate Recipe's backing collections.

## Final RecipeStep Structure

`RecipeStep` contains:

- `RecipeStepId` — step identity;
- `RecipeId` — owning Recipe identity;
- `StepNumber` — positive prescribed execution order;
- `Name` — required nonblank name;
- `Instructions` — required nonblank instructions;
- `RequiresSignature` — definition-time signature requirement.

Recipe returns steps in ascending `StepNumber` order and rejects a duplicate `StepNumber` within the same Recipe. No runtime status, execution, skipped-step prevention, or signature enforcement was added.

## Final Material Requirement Structure

`RecipeMaterialRequirement` contains:

- `RecipeId` — owning Recipe identity;
- `MaterialCode` — required Material identity;
- nullable `decimal RequiredQuantity` — optional quantity.

A supplied quantity must be greater than zero. A null quantity is valid. No material-verification behavior or BR-001 enforcement was added.

## Final Equipment Requirement Structure

`RecipeEquipmentRequirement` contains:

- `RecipeId` — owning Recipe identity;
- `EquipmentCode` — required Equipment identity.

No equipment-verification behavior or BR-002 enforcement was added.

## Validation and Invariants

Implemented definition-level validation only:

- Recipe and RecipeStep identities and referenced code identities cannot be null.
- Recipe name and version cannot be blank.
- RecipeStep `StepNumber` must be positive.
- RecipeStep name and instructions cannot be blank.
- A Recipe accepts only definitions carrying its own `RecipeId`.
- Duplicate RecipeStep numbers within one Recipe are rejected with `DomainException`.
- Optional material `RequiredQuantity`, when present, must be positive.
- Owned collections cannot be mutated directly by external callers.
- Steps are observable in prescribed numerical order.

No formatting, length, regex, regulatory, execution-time, or persistence rules were invented.

## Existing Recipe Fields Realigned

- Removed `ProductCode` from Recipe because the approved DBDS places product identification on ProductionOrder.
- Replaced `Description` with the approved Recipe `Name` terminology.
- Replaced `IsActive` with the approved `IsApproved` terminology.
- Added `RecipeId` separately from `RecipeCode` to represent the independent identity shown by the DBDS.

Repository-wide inspection found no source or test call site using the prior Recipe constructor or removed properties, so no dependent compile correction was necessary. ProductionOrder was intentionally not redesigned.

## Tests Added

Twenty-two focused xUnit tests were added across:

- `RecipeTests` — construction, approval representation, required values, ownership, ordered steps, duplicate step rejection, and owned requirement collections.
- `RecipeStepTests` — valid definitions, both signature-definition values, positive step number, and required text.
- `RecipeRequirementTests` — optional/positive material quantities, invalid supplied quantities, and equipment requirement identities.

No test advances Batch, records verification, executes runtime steps, enforces a signature, creates a deviation, or generates an AuditEvent.

## Test Evidence IDs

Task 2 evidence is registered as `S2-T2-001` through `S2-T2-022` in `docs/testing/Test_Evidence_Register.md`. Task 1 evidence rows were preserved.

## Build Result

```text
dotnet build src/SyncBridge.Core.slnx --no-restore

Build succeeded.
0 Warning(s)
0 Error(s)
```

Raw output: `artifacts/test-evidence/sprint-2/task-2-recipe-execution-structure/build.log`.

## Full Test Result

```text
Passed: 37
Failed: 0
Skipped: 0
Total: 37
```

The full-suite TRX is stored at `artifacts/test-evidence/sprint-2/task-2-recipe-execution-structure/test-results.trx`.

All 15 accepted Task 1 Batch lifecycle tests passed unchanged. All 22 new Task 2 tests passed.

## Architecture Confirmation

The Domain project remains free of Entity Framework Core, SQL Server, ASP.NET Core, UI, repository, mapping, migration, and persistence dependencies. No Application or Infrastructure implementation was added.

## Baseline Conflicts and Dependencies

The only existing-model conflict was the former Recipe shape (`ProductCode`, `Description`, and `IsActive`), which contradicted the approved `RecipeId`, `RecipeCode`, `Name`, `Version`, and `IsApproved` structure. It was corrected directly because no code depended on that old shape.

The approved intent states that ProductionOrder references Recipe, while the current ProductionOrder has no Recipe reference. ProductionOrder was not modified because Task 2 explicitly prohibits redesigning it; this remains a separate scoped concern rather than being broadened into this task.

## Scope Stop

Task 2 is complete. Task 3 was not started.
