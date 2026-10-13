# WP-6 Sprint 2 Task 3 — Prerequisite Verification Report

## Outcome

Task 3 is implemented and validated. Batch now enforces BR-001 and BR-002 at the `Ready -> InProgress` boundary using the applicable Recipe's complete Material and Equipment requirement sets and supplied verification evidence.

## Files Changed

### Modified

- `src/SyncBridge.Core.Domain/Entities/Batch.cs`
- `src/SyncBridge.Core.Domain/Entities/MaterialVerification.cs`
- `src/SyncBridge.Core.Domain/Entities/EquipmentVerification.cs`
- `tests/SyncBridge.Core.Tests/BatchLifecycleTests.cs`
- `docs/testing/Test_Evidence_Register.md`

### Added

- `src/SyncBridge.Core.Domain/Exceptions/BatchPrerequisiteVerificationException.cs`
- `tests/SyncBridge.Core.Tests/BatchPrerequisiteVerificationTests.cs`
- `tests/SyncBridge.Core.Tests/VerificationEvidenceTests.cs`
- `artifacts/test-evidence/sprint-2/task-3-prerequisite-verification/evidence.md`
- `artifacts/test-evidence/sprint-2/task-3-prerequisite-verification/build.log`
- `artifacts/test-evidence/sprint-2/task-3-prerequisite-verification/test-results.trx`
- `docs/WP6_Sprint2_Task3_Prerequisite_Verification_Report.md`

## Verification Entities Changed

`MaterialVerification` and `EquipmentVerification` were realigned as guarded domain evidence. Their approved existing fields were retained, and constructor null guards plus complete XML documentation were added. No persistence fields or behavior were introduced.

## Final MaterialVerification Structure

- `BatchNumber` — Batch associated with the verification.
- `MaterialCode` — verified Material identity.
- `OperatorId` — responsible actor.
- `VerifiedAt` — verification timestamp.
- `VerificationResult` — `Passed` or `Failed`.

The three identity values are required. LotNumber and comments were not added because they were not needed for the approved Task 3 gate.

## Final EquipmentVerification Structure

- `BatchNumber` — Batch associated with the verification.
- `EquipmentCode` — verified Equipment identity.
- `OperatorId` — responsible actor.
- `VerifiedAt` — verification timestamp.
- `VerificationResult` — `Passed` or `Failed`.

The three identity values are required. Calibration evidence and a calibration gate were not added because calibration is optional and not an approved Sprint 2 gate.

## Final Prerequisite-Evaluation Behavior

`Batch.StartProcessing` accepts the applicable Recipe and both evidence sets. A verification satisfies a requirement only when:

- it belongs to the Batch attempting execution;
- it references the required Material or Equipment item;
- its result is `Passed`.

Every Recipe Material requirement and every Recipe Equipment requirement must be satisfied. Failed, missing, unrelated-item, and wrong-Batch evidence does not satisfy the gate. Empty requirement sets are satisfied without dummy evidence. Material and Equipment evidence is evaluated independently, with no ordering policy.

The supplied RecipeCode must match the Batch RecipeCode so requirements from another Recipe cannot authorize execution.

## Ready to InProgress Protection

There is one public `StartProcessing` path, and it requires prerequisite context. No parameterless or unrestricted overload remains. Batch verifies its current state and all prerequisites before assigning `BatchStatus.InProgress`.

The approved five-state vocabulary remains unchanged:

```text
Created -> Ready -> InProgress
InProgress -> Exception
Exception -> InProgress
InProgress -> Completed
```

All lifecycle behavior outside the initial `Ready -> InProgress` gate is unchanged.

## Failure Behavior

Prerequisite failures throw `BatchPrerequisiteVerificationException`, derived from `DomainException`, with the affected BatchNumber and a clear reason. State mutation occurs only after the complete evaluation succeeds, so failed attempts leave Batch in `Ready` with no partial transition.

Calling `StartProcessing` from a state other than `Ready` continues to throw `InvalidBatchStateTransitionException` before prerequisite evaluation.

## Tests Added

Twenty-three focused Task 3 tests cover:

- complete multiple Material and Equipment requirement sets;
- missing, failed, wrong-Batch, and unrelated Material evidence;
- missing, failed, wrong-Batch, and unrelated Equipment evidence;
- incomplete subsets of multiple requirements;
- empty Material, empty Equipment, and completely empty requirement sets;
- mismatched Recipe association;
- failure atomicity and later successful retry with complete evidence;
- verification evidence field preservation;
- verification required-identity guards.

No Task 3 test or implementation executes RecipeSteps, enforces signatures, creates deviations, or generates AuditEvents.

## Task 1 Regression Adaptation

The accepted Task 1 test names and lifecycle assertions remain intact. Calls to the changed `StartProcessing` signature now pass a matching Recipe with empty requirement sets and empty evidence. This is approved production behavior, not a test-only bypass.

## Task 3 Evidence IDs

Task 3 evidence is registered as `S2-T3-001` through `S2-T3-023`. Task 1 and Task 2 rows and raw evidence were preserved.

## Build Result

```text
dotnet build src/SyncBridge.Core.slnx --no-restore

Build succeeded.
0 Warning(s)
0 Error(s)
```

Raw output: `artifacts/test-evidence/sprint-2/task-3-prerequisite-verification/build.log`.

## Complete Test Result

```text
Passed: 60
Failed: 0
Skipped: 0
Total: 60
```

- Task 1: 15/15 passed.
- Task 2: 22/22 passed unchanged.
- Task 3: 23/23 passed.

Machine-readable results: `artifacts/test-evidence/sprint-2/task-3-prerequisite-verification/test-results.trx`.

## Required Confirmations

- Failed prerequisites leave Batch `Ready`.
- No direct public `StartProcessing` bypass exists.
- `BatchStatus` was not modified.
- Task 2 Recipe tests and implementation remain unchanged.
- Task 1 and Task 2 raw evidence were not overwritten.
- No EF Core, SQL Server, repository, persistence, application, ASP.NET Core, or UI dependency was added.

## Dependencies and Baseline Conflicts

The only required compatibility change was the Task 1 test adaptation for the new mandatory `StartProcessing` context. No unexpected production dependency on the former parameterless signature existed. Existing `VerificationResult.Passed` and `Failed` terminology already matched the approved model and was retained.

## Scope Stop

Task 3 is complete. Task 4 was not started.
