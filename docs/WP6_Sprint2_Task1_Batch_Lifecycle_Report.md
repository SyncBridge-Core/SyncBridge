# WP-6 Sprint 2 Task 1 — Corrected Batch Lifecycle Report

## Outcome

Task 1 is complete. The Batch lifecycle now matches the approved five-state vocabulary and transition map. Obsolete Batch states and lifecycle APIs were removed, focused xUnit coverage was added, the complete solution builds without warnings or errors, and all tests pass.

## Files Changed

| File | Change |
|---|---|
| `src/SyncBridge.Core.Domain/Enumerations/BatchStatus.cs` | Replaced the obsolete eight-state vocabulary with exactly five approved states. |
| `src/SyncBridge.Core.Domain/Entities/Batch.cs` | Replaced the obsolete verification-oriented lifecycle methods with the approved lifecycle transition methods and retained centralized transition enforcement. |
| `tests/SyncBridge.Core.Tests/UnitTest1.cs` | Removed the generated empty placeholder test. |
| `tests/SyncBridge.Core.Tests/BatchLifecycleTests.cs` | Added 15 focused xUnit tests covering initial state, legal transitions, required illegal transitions, full lifecycle paths, and terminal behavior. |
| `docs/WP6_Sprint2_Task1_Batch_Lifecycle_Report.md` | Added this implementation evidence report. |

The pre-existing `docs/WP6_Sprint2_Current_State_Review.md` was not modified; it remains a dated snapshot of the repository before Task 1.

## Final BatchStatus Values

`BatchStatus` contains exactly:

1. `Created`
2. `Ready`
3. `InProgress`
4. `Exception`
5. `Completed`

Removed Batch states:

- `Prepared`
- `MaterialVerified`
- `EquipmentVerified`
- `QualityReview`
- `Cancelled`

No aliases or compatibility mappings were retained.

## Final Legal Transition Map

```text
Created    -> Ready
Ready      -> InProgress
InProgress -> Exception
Exception  -> InProgress
InProgress -> Completed
```

The corresponding public domain methods are:

| Source | Method | Target |
|---|---|---|
| `Created` | `MarkReady()` | `Ready` |
| `Ready` | `StartProcessing()` | `InProgress` |
| `InProgress` | `EnterException()` | `Exception` |
| `Exception` | `ResumeProcessing()` | `InProgress` |
| `InProgress` | `Complete()` | `Completed` |

Every operation delegates to the private centralized `TransitionTo` guard. An incorrect source state throws the existing `InvalidBatchStateTransitionException`. `Completed` is terminal, and no cancel or reopen operation exists.

## Preserved Batch Data

The task did not redesign unrelated Batch data. The following existing state remains intact:

- `BatchNumber`
- `ProductionOrderNumber`
- `RecipeCode`
- `PlannedQuantity`
- `UnitOfMeasure`
- constructor null guards
- positive planned-quantity guard
- automatic initial `BatchStatus.Created`

## Obsolete Lifecycle Behavior Removed

The following public Batch methods were removed because they supported the obsolete state model or out-of-scope business gates:

- `Prepare()`
- `ConfirmMaterialVerification(...)`
- `ConfirmEquipmentVerification(...)`
- `SubmitForQualityReview()`
- `Cancel()`

The material/equipment verification entity types remain unchanged because deleting or redesigning them is outside Task 1. Batch no longer uses them as lifecycle prerequisites; those business gates are reserved for later tasks.

## Tests Added

`tests/SyncBridge.Core.Tests/BatchLifecycleTests.cs` contains 15 tests:

1. `Constructor_SetsStatusToCreated` — verifies the initial state.
2. `MarkReady_WhenCreated_SetsStatusToReady` — verifies `Created -> Ready`.
3. `StartProcessing_WhenReady_SetsStatusToInProgress` — verifies `Ready -> InProgress`.
4. `EnterException_WhenInProgress_SetsStatusToException` — verifies `InProgress -> Exception`.
5. `ResumeProcessing_WhenInException_SetsStatusToInProgress` — verifies `Exception -> InProgress`.
6. `Complete_WhenInProgress_SetsStatusToCompleted` — verifies `InProgress -> Completed`.
7. `StartProcessing_WhenCreated_ThrowsInvalidTransition` — rejects `Created -> InProgress`.
8. `EnterException_WhenCreated_ThrowsInvalidTransition` — rejects `Created -> Exception`.
9. `Complete_WhenCreated_ThrowsInvalidTransition` — rejects `Created -> Completed`.
10. `EnterException_WhenReady_ThrowsInvalidTransition` — rejects `Ready -> Exception`.
11. `Complete_WhenReady_ThrowsInvalidTransition` — rejects `Ready -> Completed`.
12. `Complete_WhenInException_ThrowsInvalidTransition` — rejects `Exception -> Completed`.
13. `NormalLifecycle_ReachesCompleted` — verifies `Created -> Ready -> InProgress -> Completed`.
14. `ExceptionLifecycle_ResumesAndReachesCompleted` — verifies `Created -> Ready -> InProgress -> Exception -> InProgress -> Completed`.
15. `LifecycleMethods_WhenCompleted_RejectAllFurtherTransitions` — invokes every available lifecycle method after completion, verifies each throws `InvalidBatchStateTransitionException`, and confirms status remains `Completed`.

Tests exercise observable public behavior only and introduce no mocking framework.

## Build Result

Command:

```text
dotnet build src/SyncBridge.Core.slnx --no-restore
```

Result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Test Result

Command:

```text
dotnet test src/SyncBridge.Core.slnx --no-build --no-restore
```

Result:

```text
Passed: 15
Failed: 0
Skipped: 0
Total: 15
```

## Architecture and Dependency Verification

The Domain project was scanned for framework and persistence references. None were found.

Task 1 introduced no:

- Entity Framework Core dependency;
- SQL Server dependency;
- `DbContext`, repository, mapping, migration, or persistence implementation;
- ASP.NET Core, HTTP, Razor Pages, or other UI dependency;
- application service;
- workflow-step, deviation, signature, or audit-generation behavior.

`src/SyncBridge.Core.Domain/SyncBridge.Core.Domain.csproj` remains a technology-independent .NET class-library project with no package or project references.

## Dependency on the Old Lifecycle

No unexpected source-code or test dependency on the old Batch lifecycle was found outside `Batch.cs` and `BatchStatus.cs`.

Repository-wide matching did find `Cancelled` in `ProductionOrderStatus`, `ProductionOrder.Cancel()`, and `AuditEventType`. These are distinct, unrelated domain concepts and were intentionally left unchanged under the instruction not to redesign ProductionOrder or rename unrelated concepts.

The pre-existing current-state review describes the former lifecycle because it is an as-built snapshot produced before Task 1; it was intentionally not rewritten.

## Scope Stop

Task 1 is complete. No Task 2 functionality was started.
