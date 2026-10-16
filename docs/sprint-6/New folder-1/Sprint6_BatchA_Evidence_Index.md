# Sprint 6 Batch A sanitized evidence index

Date: 2026-10-15. All paths below are repository-relative. Sprint 5's historical 278/278 and independent SQL repeat are acceptance history, not fresh Sprint 6 executions.

Review artifacts:

- `docs/sprint-6/Sprint6_BatchA_Endpoint_Matrix.md`: pre-code routes, exact service signatures, contracts, roles, FR/BR/ADR, equivalent handlers, planned positive/negative/security checks and mutation evidence. Actual checks are in RestApiTests and final TRX; see verification report for test-name grouping.
- `docs/sprint-6/Sprint6_BatchA_Implementation_Report.md`: delivered scope, code inventory, +85 production lines, dependencies, architecture and security.
- `docs/sprint-6/Sprint6_BatchA_Verification_Report.md`: exact execution commands, all final and intermediate outcomes, preservation, limitations and Git disposition.
- `docs/sprint-6/Sprint6_BatchA_Implementation_Decision_Register.md`: actual bounded choices and inherited authoritative decisions; no new ADRs.

Fresh raw evidence directory: `artifacts/test-evidence/sprint-6/batch-a/`.

| Files | Contents |
|---|---|
| Restore_Elevated.log; Build_Final.log | Successful final restore and build, zero build warnings/errors |
| NonSql_Final.log/.trx; NonSql_Catalog.json | 274 passing non-SQL cases including 13 REST cases |
| SQL_Final.log/.trx; SQL_Catalog.json | 17 passing isolated live SQL cases |
| Rest_Verified.log/.trx | Separate final 13/13 REST repeat |
| Development_Before.json; Development_After.json; Preservation_Comparison.txt | Matching row hashes/counts, original migration, retained demo evidence; no raw manufacturing row payload |
| Source_Documents.json; Golden_Hashes.json | Read source hashes; golden files unchanged, Sprint 5 handoff read in place |
| Working_Inventory.txt; Inherited_Artifacts.json | Final tracked/untracked/ignored paths and ten inherited file hashes; no inherited content copied |
| Inner_Layer_Changes.txt | Empty diff inventory for unchanged Domain/Application/Infrastructure |
| Rest_Focused.trx | Initial 1 pass/10 fail; diagnostic Output nodes stripped to remove transient authentication values |
| Rest_Final.log/.trx | Intermediate 10 pass/1 fail before correcting test state setup |
| Build.log | Passing earlier elevated build |
| Restore.log; Rest_Retest.log | Incomplete restricted execution evidence; no successful test count claimed |

Inherited protected originals were neither modified nor staged. All new files remain uncommitted. Passing test logs are not substitutes for Batch B integrated equivalence or Team acceptance. No credentials, password hashes, raw sensitive transcripts or demonstration row data are intended for Git integration; later staging requires explicit authorization and review.
