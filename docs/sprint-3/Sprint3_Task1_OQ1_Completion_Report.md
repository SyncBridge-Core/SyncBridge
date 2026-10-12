# WP-6 Sprint 3 Task 1 — OQ-1 Completion Report

## Task Record

| Field | Actual value |
|---|---|
| Work package | WP-6 |
| Sprint/task | Sprint 3, Task 1 — OQ-1 investigation |
| Execution date | 2026-10-08 |
| Branch | `feature/wp6-sprint3-core-domain` |
| Baseline commit | `1be2fbee70d94be8ae21b5b3128fe71610ca00b3` |
| Authorization | Analysis and documentation only |
| Result | Recommendation complete; OQ-1 not approved or implemented |

## Outcome

The investigation recommends retaining `Batch.PlannedQuantity` as immutable derived state sourced exclusively from the associated `ProductionOrder.Quantity`, removing `Batch.UnitOfMeasure`, and preserving the approved 15-table DBDS without added columns or tables.

The recommendation reflects the confirmed business decision that every Batch executes the full Production Order quantity, including when multiple Batches reference the same Production Order.

## Evidence Reviewed

- Current Domain entities, value objects, enumerations, lifecycle behavior, and guards.
- Current Application persistence contracts.
- All existing Batch-related test fixtures and Production Order quantity tests.
- `docs/WP6_Sprint2_to_Sprint3_Engineering_Handoff_Team_Reviewed.docx`.
- `C:\Engineering\Golden_Documents\Indy-1-SyncBridge-DBDS_v2.docx`.
- `C:\Engineering\Golden_Documents\SyncBridge_Core_SRS_Corrected.docx`.
- `C:\Engineering\Golden_Documents\SyncBridge_Core_SDD_Corrected.docx`.
- `C:\Engineering\Golden_Documents\WP-6_Implementation_Execution_Plan_Realigned.docx`.
- Existing Task 1 design-gate and completion reports.

## Files Created or Modified

Created:

- `docs/sprint-3/Sprint3_Task1_OQ1_Impact_Analysis.md`
- `docs/sprint-3/Sprint3_Task1_OQ1_Completion_Report.md`

No source, project, package, test, database, SRS, SDD, DBDS, or existing Task 1 report was modified.

The two pre-existing untracked Task 1 reports under `docs/sprint-3/` were preserved unchanged.

## Potential Later Implementation Scope

Production files:

- `src/SyncBridge.Core.Domain/Entities/Batch.cs`
- `src/SyncBridge.Core.Domain/ValueObjects/UnitOfMeasure.cs` (removal)

Existing test fixtures:

- `tests/SyncBridge.Core.Tests/BatchAuditEvidenceTests.cs`
- `tests/SyncBridge.Core.Tests/BatchDeviationLifecycleTests.cs`
- `tests/SyncBridge.Core.Tests/BatchLifecycleTests.cs`
- `tests/SyncBridge.Core.Tests/BatchPrerequisiteVerificationTests.cs`
- `tests/SyncBridge.Core.Tests/BatchStepExecutionTests.cs`
- `tests/SyncBridge.Core.Tests/CompletedRecordProtectionTests.cs`
- `tests/SyncBridge.Core.Tests/ElectronicSignatureGateTests.cs`
- `tests/SyncBridge.Core.Tests/OrderedWorkflowExecutionTests.cs`

No current Application contract requires modification. Future Infrastructure mapping code will need to derive the Batch quantity through the existing Batch-to-ProductionOrder association when repository implementation begins.

## Validation Results

| Validation | Result |
|---|---|
| `dotnet build src\SyncBridge.Core.slnx --no-restore --verbosity minimal` | Passed; 0 errors, 0 warnings |
| `dotnet test src\SyncBridge.Core.slnx --no-restore --verbosity minimal` | Passed; 159 passed, 0 failed, 0 skipped |
| Source/test modifications | None |
| Database/schema modifications | None |
| Existing Task 1 report modifications | None |

An initial test command used the nonexistent root path `SyncBridge.Core.sln`; it failed before build or test execution with MSBuild error MSB1009. The command was corrected to the actual solution at `src\SyncBridge.Core.slnx`, where the full suite passed.

## Assumptions

1. The Team statement that each Batch executes the full Production Order quantity supersedes the earlier unresolved reconstruction question but does not itself authorize code changes.
2. `ProductionOrder.Quantity` remains required and positive under the existing Domain invariant.
3. `Batch.PlannedQuantity` is useful read-only execution context and is not a second source of truth.
4. No default unit such as `kg` may be inferred because the approved requirements and DBDS provide no authoritative unit.
5. Batch continues to retain `ProductionOrderNumber`, not a Production Order navigation property.
6. The exact creation and non-public rehydration API will be approved with the broader persistence/rehydration design before implementation.
7. Accepted historical reports remain unchanged; the eventual implementation report will record the approved OQ-1 disposition.

## Outstanding Issues

- OQ-1 still requires formal Team approval before source changes.
- The exact Batch creation/rehydration API must prevent independently supplied quantity while preserving aggregate boundaries.
- Other Task 1 open questions, including Deviation identity, string technical-ID format, and the general Domain rehydration seam, remain outside this investigation and unresolved unless separately approved.

## Next Action

Team engineering review should approve or reject Option A. If approved, schedule the narrowly scoped Domain/test changes with the later Infrastructure reconstruction work. Do not add Batch quantity/unit columns or begin persistence implementation from this report alone.
