# WP-6 Sprint 2 Task 4 — Ordered Workflow Execution Report

## Outcome

Task 4 is implemented and validated. SyncBridge Core now represents runtime execution of RecipeStep definitions through BatchStepExecution and enforces BR-003 ordering at the Batch domain boundary.

## Files Changed

### Modified

- `src/SyncBridge.Core.Domain/Entities/Batch.cs`
- `docs/testing/Test_Evidence_Register.md`

### Added

- `src/SyncBridge.Core.Domain/Entities/BatchStepExecution.cs`
- `src/SyncBridge.Core.Domain/Enumerations/StepExecutionStatus.cs`
- `src/SyncBridge.Core.Domain/Exceptions/InvalidBatchStepExecutionException.cs`
- `src/SyncBridge.Core.Domain/ValueObjects/BatchStepExecutionId.cs`
- `tests/SyncBridge.Core.Tests/BatchStepExecutionTests.cs`
- `tests/SyncBridge.Core.Tests/OrderedWorkflowExecutionTests.cs`
- `artifacts/test-evidence/sprint-2/task-4-ordered-workflow/evidence.md`
- `artifacts/test-evidence/sprint-2/task-4-ordered-workflow/build.log`
- `artifacts/test-evidence/sprint-2/task-4-ordered-workflow/test-results.trx`
- `docs/WP6_Sprint2_Task4_Ordered_Workflow_Report.md`

No BatchStatus, RecipeStep, prerequisite-verification, Application, Infrastructure, Razor Pages, project, or persistence file was modified.

## Types Added

- `BatchStepExecution`
- `StepExecutionStatus`
- `InvalidBatchStepExecutionException`
- `BatchStepExecutionId`

## Types Modified

`Batch` now owns a protected collection of registered BatchStepExecution instances and exposes domain operations to create, start, and complete them.

## Final BatchStepExecution Structure

- `BatchStepExecutionId` — execution identity.
- `BatchNumber` — associated Batch identity.
- `RecipeStepId` — associated RecipeStep identity.
- `StepNumber` — positive prescribed sequence number.
- `StepExecutionStatus` — runtime state.
- `StartedAt` — null until started.
- `CompletedAt` — null until completed.
- `CompletedBy` — null until completion.

New executions begin Pending. Starting sets StartedAt. Completing sets CompletedAt and CompletedBy. Execution transition methods are internal, ensuring public callers must go through Batch enforcement.

## Final Step-Status Vocabulary

`StepExecutionStatus` contains exactly:

```text
Pending
InProgress
Completed
```

Legal transitions:

```text
Pending -> InProgress -> Completed
```

There are no additional step-execution states.

## Ordered Workflow Behavior

Batch provides:

- `CreateStepExecution(Recipe, RecipeStep, BatchStepExecutionId)`;
- `StartStep(Recipe, BatchStepExecution, DateTimeOffset)`;
- `CompleteStep(BatchStepExecution, DateTimeOffset, OperatorId)`.

Registration requires the Recipe to match the Batch RecipeCode and the RecipeStep identity/number to exist in that Recipe. Duplicate execution identity or duplicate execution for one RecipeStep is rejected.

Starting requires:

- Batch is `InProgress`;
- execution is registered with the Batch;
- execution belongs to the applicable Recipe;
- no other execution is `InProgress`;
- every lower-numbered RecipeStep has a registered Completed execution;
- selected execution is `Pending`.

Completing requires Batch `InProgress`, a registered execution, and execution status `InProgress`.

## Failure Behavior

Invalid runtime operations throw `InvalidBatchStepExecutionException`, derived from DomainException, with the affected BatchNumber and reason. Rejected starts leave the selected execution Pending; rejected transitions do not mutate execution or Batch status.

## Batch-State Interaction

Step start and completion are permitted only while Batch is `InProgress`. Operations are rejected while Batch is `Created`, `Ready`, `Exception`, or `Completed`.

Step execution never changes Batch lifecycle state. Completing the final RecipeStep leaves Batch `InProgress`; later tasks own completion gates.

The five Batch states remain unchanged:

```text
Created
Ready
InProgress
Exception
Completed
```

## Tests Added

Twenty-four focused xUnit tests cover construction, required identities, step status/timestamps, completion actor, invalid transitions, full three-step ordering, skipped steps, concurrent starts, Recipe consistency, Batch-state gating, final-step behavior, and the deliberate absence of signature enforcement.

## Evidence IDs

Task 4 evidence is registered as `S2-T4-001` through `S2-T4-024`. All Task 1–3 evidence rows and raw artifacts were preserved.

## Build Result

```text
dotnet build src/SyncBridge.Core.slnx --no-restore

Build succeeded.
0 Warning(s)
0 Error(s)
```

Raw output: `artifacts/test-evidence/sprint-2/task-4-ordered-workflow/build.log`.

## Full Test Result

```text
Passed: 84
Failed: 0
Skipped: 0
Total: 84
```

- Task 1: 15/15 passed.
- Task 2: 22/22 passed.
- Task 3: 23/23 passed.
- Task 4: 24/24 passed.

Machine-readable results: `artifacts/test-evidence/sprint-2/task-4-ordered-workflow/test-results.trx`.

## Required Confirmations

- Completing the final RecipeStep does not automatically complete Batch.
- RecipeStep `RequiresSignature` is not enforced; Task 5 retains that responsibility.
- No AuditEvent is created; Task 7 retains that responsibility.
- Task 3 prerequisite enforcement remains unchanged.
- BatchStatus values remain unchanged.
- No EF Core, SQL Server, repository, persistence, application, ASP.NET Core, API, or UI dependency was added.

## Dependencies and Baseline Conflicts

No unexpected dependency or baseline conflict was discovered. The existing Recipe ordered-step model and identifier value objects supported direct integration without changes. The runtime collection is Domain-owned and independent of future persistence mappings.

## Scope Stop

Task 4 is complete. Task 5 was not started.
