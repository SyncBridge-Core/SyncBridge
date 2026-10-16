# Sprint 6 final evidence index

Date: 2026-10-15, America/New_York. All paths below are repository-relative. Team acceptance remains PENDING; no Git integration is authorized or executed.

## Current engineering reports

| Path under docs/sprint-6/ | Purpose |
|---|---|
| Sprint6_BatchB_Integrated_Verification_Report.md | Executed equivalence scenarios, SQL HTTP path, security, correction, impact and bounds |
| Sprint6_Final_Requirements_and_Evidence_Matrix.md | Exact five-route/Page/service mapping and FR/BR/NFR/ADR evidence |
| Sprint6_Final_Engineering_Verification_Report.md | Self-contained commands/counts, build/advisories, correction, preservation and readiness |
| Sprint6_Final_Closure_Report_Proposed.md | Proposed Sprint closure, pending acceptance and Git status |
| Sprint6_Git_Readiness_Report.md | Exact staging proposal/exclusions and later authorized integration procedure |
| Sprint6_Final_Evidence_Index.md | This index |

Accepted Batch A reports are currently under `docs/sprint-6/New folder-1/`: `Sprint6_BatchA_Endpoint_Matrix.md`, `Sprint6_BatchA_Implementation_Report.md`, `Sprint6_BatchA_Verification_Report.md`, `Sprint6_BatchA_Implementation_Decision_Register.md`, `Sprint6_BatchA_Evidence_Index.md`. Their relocation predates B. The decision register alone receives an appended B section; A text/history are retained. `BatchA_Report_Hashes.json` captures entry hashes. Batch A raw evidence remains in `artifacts/test-evidence/sprint-6/batch-a/`; its reported 291 unique cases/13 REST repeat are historical for this final report.

## Fresh Batch B raw evidence

Directory: `artifacts/test-evidence/sprint-6/batch-b/`.

| Files | Verification purpose |
|---|---|
| Restore.log | Successful restore |
| Build.log; Build_Final.log; Build_ClockCorrected.log | Passing solution builds, zero warnings/errors |
| Package_Advisories.log | Transitive vulnerability check against configured package sources |
| Integrated_Final.log/.trx; Test_Catalog_Final.json | Final 302/302 unique cases, 284 non-SQL + 18 SQL, zero failures/skips |
| SQL_Final_Repeat.log/.trx | Independent 18/18 SQL repeat after clock correction; not extra unique cases |
| Equivalence_Results_Final.txt; SQL_HTTP_Reconciliation.txt | Real UI/API normalized outcomes, actual SQL rows and exact cleanup evidence; extracted from final TRX |
| Integrated.log/.trx; Test_Catalog.json; Equivalence_Results.txt | Initial passing 302-case run before aligning fixture/HTTP manufacturing clocks; retained, superseded by final run |
| SQL_Repeat.log/.trx | Initial passing 18-case SQL repeat before fixture clock correction |
| Equivalence_Focused.log/.trx | Initial nine-case passing focused run before final coverage additions |
| Development_Before.json; Development_After.json; Preservation_Comparison.json | Identical 15-table ordered-row hashes/counts, original migration and retained A3 summaries; no raw row payloads |
| SQL_Disposal.json | Exact isolated catalog absent after final repeat |
| Architecture.json | No controller persistence references; unchanged inner layers/projects/migration inventory |
| Source_Documents.json | Five authoritative source hashes equal Batch A |
| BatchA_Production_Diff.txt; BatchA_Report_Hashes.json; BatchA_Report_Comparison.json; Decision_Register_Preservation.json | Entry production diff, relocated report hashes and explicit decision-register addendum reconciliation; original A front matter matches its entry SHA with original final newline |
| Preflight_Inventory.txt; Final_Git_Inventory.txt; Remote_Refs.txt | Start/final Git status including ignored items, remote main/feature SHAs |
| Source_Manifest.json | Exact production/test candidate hashes |
| Git_Include_Paths.txt; Git_Inclusion_Manifest.json | Proposed exact per-file inclusion, categories/status; no staging executed |
| Git_Exclusions.json; Inherited_Preservation.json | Ten protected inherited files excluded and hash-preserved, plus ignored/private outputs |
| Sensitive_Content_Check.json; Diff_Check.txt | Candidate secret-pattern and tracked whitespace checks |
| Readiness_Inventory.ps1 | Repeatable read-only inventory/hash/scan utility; no Git mutations or DB connections |

Batch A's originally referenced empty `Inner_Layer_Changes.txt` was absent at B entry; use fresh B `Architecture.json` for explicit unchanged-layer evidence. Protected Sprint 4/5 raw transcripts/completion reports and handoff remain local excluded references; none were copied or silently added to Sprint 6. Their hashes are sufficient for preservation checks. No failed test evidence is concealed: B runs all passed, and initial A failures remain in A's sanitized historical TRX/logs.

Final limitations are in the verification/closure reports. No formal penetration/load qualification, new enterprise integration or Sprint 7 EBR/SSRS acceptance is implied by this evidence.
