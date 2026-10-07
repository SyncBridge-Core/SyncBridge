# WP-6 Sprint 2 Task 6 — Deviation Management Report

## Outcome

Task 6 is implemented and validated. Deviations now carry immutable manufacturing origin evidence, support focused review/approval/resolution behavior, and gate the Batch `InProgress -> Exception -> InProgress` path.

## Files Modified

- `src/SyncBridge.Core.Domain/Entities/Batch.cs`
- `src/SyncBridge.Core.Domain/Entities/Deviation.cs`
- `tests/SyncBridge.Core.Tests/BatchLifecycleTests.cs`
- `tests/SyncBridge.Core.Tests/OrderedWorkflowExecutionTests.cs`
- `tests/SyncBridge.Core.Tests/ElectronicSignatureGateTests.cs`
- `docs/testing/Test_Evidence_Register.md`

## Files Added

- `src/SyncBridge.Core.Domain/Exceptions/BatchDeviationException.cs`
- `src/SyncBridge.Core.Domain/Exceptions/InvalidDeviationStateTransitionException.cs`
- `tests/SyncBridge.Core.Tests/DeviationTests.cs`
- `tests/SyncBridge.Core.Tests/BatchDeviationLifecycleTests.cs`
- `artifacts/test-evidence/sprint-2/task-6-deviation-management/build.log`
- `artifacts/test-evidence/sprint-2/task-6-deviation-management/test-results.trx`
- `artifacts/test-evidence/sprint-2/task-6-deviation-management/evidence.md`
- `docs/WP6_Sprint2_Task6_Deviation_Management_Report.md`

## Types Added and Modified

Added `BatchDeviationException` and `InvalidDeviationStateTransitionException`, both derived from DomainException. Modified `Deviation` from a passive arbitrary-status holder into a lifecycle-owning entity. Modified `Batch` to retain exception-path deviations and enforce exception entry and resumption.

## Final Deviation Structure

- `DeviationCode` — immutable deviation identity.
- `BatchNumber` — immutable originating Batch identity.
- `BatchStepExecutionId` — optional immutable originating execution identity.
- `Description` — required immutable abnormal-condition description.
- `ReportedBy` — immutable reporting actor.
- `OpenedAt` — immutable opening timestamp.
- `DeviationStatus` — controlled lifecycle status.
- `ReviewComments` — review evidence.
- `FinalDisposition` — required resolution evidence.
- `ResolvedAt` — resolution timestamp.
- `IsBlocking` — true until Closed.

The step-aware constructor accepts a BatchStepExecution only to validate same-Batch origin, then retains its identity rather than a navigation property.

## Status Interpretation and Behavior

The existing vocabulary remains unchanged: `Open`, `UnderReview`, `Approved`, `Rejected`, and `Closed`. Open, UnderReview, Approved, and Rejected are unresolved/blocking. Closed is resolved/non-blocking.

New deviations always begin Open. `BeginReview` requires comments and moves Open to UnderReview. `Approve` and `Reject` preserve the review result. Only an Approved deviation can resolve. `Resolve` requires a descriptive final disposition, populates ResolvedAt, and moves to Closed. Repeated resolution and invalid transitions throw `InvalidDeviationStateTransitionException` without overwriting evidence.

## Permanent Origin Enforcement

BatchNumber and optional BatchStepExecutionId are get-only. A supplied step execution must belong to the same Batch. No reassignment method exists. Batch rejects foreign deviations during Exception entry.

## Batch Exception and Resumption Behavior

`EnterException(params Deviation[])` requires Batch InProgress, at least one unresolved deviation, same-Batch origin, and unique deviation identities. Successful entry retains all supplied deviations and sets Batch Exception. An already resolved deviation cannot initiate a new Exception episode.

`ResumeProcessing()` checks the retained association. If any deviation is not Closed, it throws `BatchDeviationException` and leaves Batch Exception. After all associated deviations close, it restores Batch InProgress. Foreign resolved deviations cannot enter the association or satisfy the gate.

Resolving and resuming does not complete Batch or an active step. Completed remains terminal. BatchStatus still contains exactly Created, Ready, InProgress, Exception, and Completed.

## Regression Behavior

- Step operations remain blocked during Exception.
- Prescribed ordering remains enforced after resumption.
- Required step signatures remain enforced after resumption.
- Prerequisite gates are unchanged.
- No AuditEvent is generated.

## Tests and Evidence

Thirty Task 6 cases were added. Evidence IDs are `S2-T6-001` through `S2-T6-030`.

```text
dotnet build src/SyncBridge.Core.slnx --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

```text
dotnet test src/SyncBridge.Core.slnx --no-build
Passed: 135
Failed: 0
Skipped: 0
Total: 135
```

All accepted regression groups pass: Task 1 15/15, Task 2 22/22, Task 3 23/23, Task 4 24/24, and Task 5 21/21. `git diff --check` also passes.

## Architecture Confirmation

No audit generation, EF Core, SQL Server, DbContext, repository, mapping, migration, persistence attribute, Application service, authorization, ASP.NET Core, Razor Pages, REST API, UI, or reporting behavior was added.

## Baseline Conflict and Assumptions

The named golden-document files were not present in the repository or mounted transfer folder during implementation. The prompt's explicit golden-behavior extraction, accepted Task 1–5 implementation, accepted reports, and test evidence therefore formed the operational baseline. No conflict was found among those available authorities.

`Approved` remains blocking until explicit final disposition and closure because the approved persistence model separately requires FinalDisposition and ResolvedAt. `Rejected` remains blocking; no unapproved re-review/escalation workflow was invented.

## Scope Stop

Task 6 is complete. Task 7 was not started.
