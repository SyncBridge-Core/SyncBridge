# Task 6 Deviation Management — Test Evidence

## Task Identification

- **Project:** SyncBridge Core
- **Work package:** WP-6
- **Sprint:** Sprint 2
- **Task:** Task 6 — Deviation Management and Exception/Resume Behavior
- **Evidence date:** 2026-10-05
- **Acceptance status:** Pending AI-Agent / Team review

## Scope

This evidence covers deviation creation, permanent Batch origin, optional same-Batch step context, review and disposition lifecycle, blocking status, Batch exception entry, and resolution-gated resumption. It also covers multiple deviations and ordered-workflow/signature regressions.

It does not claim audit generation, authorization, persistence, repository, Application, API, UI, reporting, EBR, or completed-record correction behavior.

## Files Changed

### Domain

- `src/SyncBridge.Core.Domain/Entities/Batch.cs`
- `src/SyncBridge.Core.Domain/Entities/Deviation.cs`
- `src/SyncBridge.Core.Domain/Exceptions/BatchDeviationException.cs`
- `src/SyncBridge.Core.Domain/Exceptions/InvalidDeviationStateTransitionException.cs`

### Tests

- `tests/SyncBridge.Core.Tests/BatchLifecycleTests.cs`
- `tests/SyncBridge.Core.Tests/OrderedWorkflowExecutionTests.cs`
- `tests/SyncBridge.Core.Tests/ElectronicSignatureGateTests.cs`
- `tests/SyncBridge.Core.Tests/DeviationTests.cs`
- `tests/SyncBridge.Core.Tests/BatchDeviationLifecycleTests.cs`

### Evidence and reporting

- `docs/testing/Test_Evidence_Register.md`
- `docs/WP6_Sprint2_Task6_Deviation_Management_Report.md`
- `artifacts/test-evidence/sprint-2/task-6-deviation-management/build.log`
- `artifacts/test-evidence/sprint-2/task-6-deviation-management/test-results.trx`
- `artifacts/test-evidence/sprint-2/task-6-deviation-management/evidence.md`

## Final Deviation Structure

Deviation contains immutable `DeviationCode`, originating `BatchNumber`, optional originating `BatchStepExecutionId`, required `Description`, `ReportedBy`, and `OpenedAt`. Lifecycle state comprises `DeviationStatus`, `ReviewComments`, `FinalDisposition`, and `ResolvedAt`. `IsBlocking` answers whether the deviation prevents resumption.

## Deviation Status Interpretation

- `Open`, `UnderReview`, `Approved`, and `Rejected` are unresolved and blocking.
- `Closed` is resolved and non-blocking.
- The retained enum vocabulary was not changed.

## Review, Disposition, and Resolution

`BeginReview` moves Open to UnderReview and requires comments. `Approve` or `Reject` preserves the review result. Only Approved may resolve. `Resolve` requires a nonblank final disposition, sets ResolvedAt, and closes the deviation. Repeated resolution is rejected without overwriting evidence.

## Batch Exception Entry and Resumption

`EnterException` requires one or more unresolved same-Batch deviations and accepts only the `InProgress -> Exception` transition. Batch retains the supplied deviation references. `ResumeProcessing` remains parameterless but evaluates the retained association, rejecting resumption while any deviation is blocking. Only after all are Closed does `Exception -> InProgress` succeed.

No automatic Batch or step completion occurs. Completed remains terminal.

## Tests Added

Thirty executed Task 6 cases cover the required creation, validation, origin, review, approval/rejection, disposition, resolution, exception-entry, wrong-Batch, invalid-state, multiple-deviation, resumption, ordering, signature, and completion-boundary scenarios.

## Traceability Summary

Evidence IDs `S2-T6-001` through `S2-T6-030` primarily trace FR-003, FR-009, FR-010, BR-006, ADR-006, ADR-007, the listed SRS/SDD sections, and DBDS §§5.9, 5.10, and 5.12. No BR-005, BR-007, BR-008, or NFR-001 coverage is claimed.

## Build Evidence

- **Command:** `dotnet build src/SyncBridge.Core.slnx --no-restore`
- **Result:** Succeeded
- **Warnings:** 0
- **Errors:** 0
- **Raw output:** `build.log`

## Test Evidence

- **Command:** `dotnet test src/SyncBridge.Core.slnx --no-build --logger "trx;LogFileName=test-results.trx" --results-directory artifacts/test-evidence/sprint-2/task-6-deviation-management`
- **Passed:** 135
- **Failed:** 0
- **Skipped:** 0
- **Total:** 135
- **Task 6:** 30/30 passed
- **Task 1:** 15/15 passed
- **Task 2:** 22/22 passed
- **Task 3:** 23/23 passed
- **Task 4:** 24/24 passed
- **Task 5:** 21/21 passed
- **Machine-readable results:** `test-results.trx`

Two sandboxed launches exited before executing tests or producing a TRX after the Windows drive-mount change. The approved full-suite command was rerun outside that sandbox and produced the successful retained TRX. There was no failed test-execution artifact to preserve.

## Architecture Confirmation

- No AuditEvent generation was added.
- No persistence, repository, EF Core, SQL Server, Application, ASP.NET Core, API, UI, or reporting dependency was introduced.
- The five-state Batch vocabulary is unchanged.
- Task 3 prerequisite, Task 4 ordering, and Task 5 signature gates remain intact.
