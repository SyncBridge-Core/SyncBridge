# Task 3 Material and Equipment Verification Gates — Test Evidence

## Task Identification

- **Project:** SyncBridge Core
- **Work package:** WP-6
- **Sprint:** Sprint 2
- **Task:** Task 3 — Material and Equipment Verification Gates
- **Evidence date:** 2026-10-05
- **Acceptance status:** Pending AI-Agent / Team review

## Scope

This evidence covers BR-001 and BR-002 enforcement at the Batch `Ready -> InProgress` boundary. It verifies that every Recipe Material and Equipment requirement has successful evidence matching the active Batch and required item. It also covers empty requirement sets and basic MaterialVerification/EquipmentVerification structure.

This task does not cover RecipeStep execution, BatchStepExecution, electronic-signature gates, deviations, audit generation, persistence, application services, or UI behavior.

## Files Changed

### Domain

- `src/SyncBridge.Core.Domain/Entities/Batch.cs`
- `src/SyncBridge.Core.Domain/Entities/MaterialVerification.cs`
- `src/SyncBridge.Core.Domain/Entities/EquipmentVerification.cs`
- `src/SyncBridge.Core.Domain/Exceptions/BatchPrerequisiteVerificationException.cs`

### Tests

- `tests/SyncBridge.Core.Tests/BatchLifecycleTests.cs` — necessary signature-only adaptation using valid empty-requirement context.
- `tests/SyncBridge.Core.Tests/BatchPrerequisiteVerificationTests.cs`
- `tests/SyncBridge.Core.Tests/VerificationEvidenceTests.cs`

### Evidence and reporting

- `docs/testing/Test_Evidence_Register.md`
- `docs/WP6_Sprint2_Task3_Prerequisite_Verification_Report.md`
- `artifacts/test-evidence/sprint-2/task-3-prerequisite-verification/build.log`
- `artifacts/test-evidence/sprint-2/task-3-prerequisite-verification/test-results.trx`
- `artifacts/test-evidence/sprint-2/task-3-prerequisite-verification/evidence.md`

## Verification Model Changes

`MaterialVerification` and `EquipmentVerification` retain the accepted evidence fields:

- associated `BatchNumber`;
- required item identity (`MaterialCode` or `EquipmentCode`);
- responsible `OperatorId`;
- `VerifiedAt` timestamp;
- `VerificationResult` (`Passed` or `Failed`).

Both constructors now reject null required identities. No LotNumber, comments, calibration policy, persistence annotations, or database behavior was added.

## StartProcessing and Prerequisite-Gate Design

`Batch.StartProcessing` now requires:

- the applicable `Recipe`;
- available `MaterialVerification` evidence;
- available `EquipmentVerification` evidence.

It first confirms the Batch is `Ready`, then confirms the RecipeCode matches the Batch. Every Recipe Material requirement must have at least one `Passed` verification matching both the BatchNumber and MaterialCode. Every Recipe Equipment requirement must have at least one `Passed` verification matching both the BatchNumber and EquipmentCode.

Empty requirement sets are satisfied without dummy evidence. If validation fails, `BatchPrerequisiteVerificationException` is thrown before any status mutation, leaving the Batch `Ready`. No parameterless `StartProcessing` overload exists.

## Tests Added

Twenty-three Task 3 tests were added:

1. Complete multiple Material and Equipment prerequisites permit execution.
2. Missing Material verification is rejected.
3. Failed Material verification is rejected.
4. Wrong-Batch Material verification is rejected.
5. Unrelated Material verification is rejected.
6. Missing Equipment verification is rejected.
7. Failed Equipment verification is rejected.
8. Wrong-Batch Equipment verification is rejected.
9. Unrelated Equipment verification is rejected.
10. A subset of multiple requirements is rejected.
11. Empty Material requirements with verified Equipment permit execution.
12. Empty Equipment requirements with verified Material permit execution.
13. No requirements permit execution.
14. A Recipe with the wrong RecipeCode is rejected.
15. A failed attempt remains `Ready` and can later succeed with complete evidence.
16–19. MaterialVerification field preservation and required-identity guards.
20–23. EquipmentVerification field preservation and required-identity guards.

## Traceability Summary

Evidence IDs `S2-T3-001` through `S2-T3-023` are recorded in `docs/testing/Test_Evidence_Register.md`. Tests trace as applicable to FR-003, FR-005, FR-006, BR-001, BR-002, ADR-006, ADR-007, SRS §§3.3, 3.5, 3.6, 3.13, 6.2, 6.6, SDD §§5.1–5.3, and DBDS §§5.4, 5.5, 5.11.1, 5.11.2.

No BR-003 through BR-008 coverage is claimed.

## Build Evidence

- **Command:** `dotnet build src/SyncBridge.Core.slnx --no-restore`
- **Result:** Succeeded
- **Warnings:** 0
- **Errors:** 0
- **Raw output:** `build.log`

## Test Evidence

- **Command:** `dotnet test src/SyncBridge.Core.slnx --no-build --logger "trx;LogFileName=test-results.trx" --results-directory artifacts/test-evidence/sprint-2/task-3-prerequisite-verification`
- **Framework:** xUnit 2.9.3 with Microsoft.NET.Test.Sdk 17.14.1
- **Passed:** 60
- **Failed:** 0
- **Skipped:** 0
- **Total:** 60
- **Machine-readable results:** `test-results.trx`

All 15 Task 1 tests passed. Their lifecycle meaning and names remain intact; only calls to `StartProcessing` were adapted to supply valid empty-requirement prerequisite context. All 22 Task 2 tests passed unchanged. All 23 Task 3 tests passed.

## Architecture Confirmation

No Entity Framework Core, SQL Server, persistence, repository, application-service, ASP.NET Core, UI, signature, deviation, or audit-generation dependency or behavior was introduced.
