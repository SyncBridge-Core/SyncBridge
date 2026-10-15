# Sprint 5 final evidence index

Date: 2026-10-14, America/New_York. Batch A is Team accepted; Batch B results below are freshly executed final engineering evidence. Final Sprint 5 acceptance/Git authorization remain separate.

## Current verification and readiness

All paths in this table are relative to `artifacts/test-evidence/sprint-5/batch-b/`.

| Artifact | Meaning |
|---|---|
| `Restore.log`, `Build.log` | Fresh restore and complete solution build; 0 build warnings/errors |
| `Development_Tool_Build.log` | Accepted Development identity helper, outside solution: build passes with 0 warnings/errors |
| `Advisories.log`, `Pkcs_Dependency_Chain.log` | No reported vulnerable packages; Pkcs 9.0.21 resolution and introducing dependency chain |
| `Final_Integrated.trx`, `.log` | 278 passed / 0 failed / 0 skipped; includes 261 non-SQL and 17 SQL |
| `Final_SQL_Repeat.trx`, `.log` | Independent disposable SQL invocation: 17 passed / 0 failed / 0 skipped |
| `Test_Summary.json`, `Test_Catalog.json` | Actual TRX-derived counts, durations, case identities and outcomes |
| `Development_Before.json`, `Development_After.json`, `SQL_Preservation.json` | Identical ordered-row hashes/counts for all 15 development manufacturing tables and migration history; retained A3 evidence; disposable test catalog removed |
| `Architecture_Checks.json`, `Source_Documents.json`, `Pending_Source_Hashes.json` | Accepted production/A4/golden-document hashes unchanged, project references, no PageModel persistence references, no Domain/migration changes, exact pending source hashes |
| `Review_Findings.json` | Security/manufacturing/architecture review conclusions, environment and explicit limits |
| `Security_Scan.json` | Proposed candidate text/DOCX secret comparison and pattern scan; intentional binaries; accepted screenshot hash preservation; protected contents excluded |
| `Git_Inclusion_Manifest.json`, `Git_Include_Paths.txt`, `Git_Exclusions.json` | Exact proposed per-file inclusion/state/category and protected/ignored exclusions; no staging performed |
| `Remote_Heads.txt`, `Final_Git_State.json`, `Final_Working_Tree.txt`, `Starting_Working_Tree.txt` | Read-only remote confirmation, local relationships, staged state and full pending-path inventory |
| `Diff_Check.log` | Successful tracked and proposed-untracked whitespace review; no remaining finding |
| `Evidence_Sanitization.json`, `sanitized/BatchA_*` | Hash-provenance map and whitespace-trimmed counterparts for five historical files with console padding or terminal blank lines, whose originals remain preserved locally and excluded from Git |
| `Reconcile_Readiness.ps1` | Reproducible scoped inventory/security/source/test-summary reconciliation; never stages files or reads protected transcript contents |

Reports under `docs/sprint-5/`: `Sprint5_Final_Engineering_Verification_Report.md`, `Sprint5_Final_Requirements_and_Evidence_Matrix.md`, this index, `Sprint5_Final_Closure_Report.md` and `Sprint5_Git_Readiness_Report.md`.

## Accepted and historical supporting evidence

Detailed implementation decisions and handler mapping remain in the accepted Batch A completion report, decision register and requirements matrix. Setup commands remain in `Development_Identity_Setup.md` and `Isolated_SQL_Test_Setup.md`.

The accepted `Sprint5_BatchA3_Execution_Report.md` describes actual HTTPS UC-001–UC-004 interactive execution with the two retained identities, explicit limitations and retained fictional records. Its 18 JPGs are indexed/hashed in Batch A `A3_Screenshot_Index.json`; their current hashes match. Certificate trust and identity provisioning/repeat-verification logs remain accepted A3 evidence. Batch B did not repeat credential provisioning, change trust or claim a new interactive walkthrough.

Preserved failures/corrections under `artifacts/test-evidence/sprint-5/batch-a/`:

| Historical artifact | Correct interpretation |
|---|---|
| `Focused_Http.trx`, `Final_NonSql.trx` | Superseded test-input/stored-identifier classification failures; corrected before passing integrated verification |
| `Browser_Host_Workflow.trx`, A2 diagnostics | Original untrusted-certificate blocker; approved A3 exact-certificate trust resolved it; never counted as a passing browser run |
| `Final_Build.log` | Earlier sandbox process failure without compiler diagnostics; elevated verified builds pass |
| `A3_Patched_Integrated.trx` | 266 pass / 12 fail from fixture accounts appending real developer configuration; corrected fixture isolation, then 278 pass |
| `Security_Patch_Integrated.trx` | 277 pass / 1 fail from shared-development empty-history assumption after retained browser data; A4 isolated SQL fixture corrected infrastructure without weakening assertions |
| `A4_Guard_Missing.trx`, `A4_Guard_Development.trx` | Deliberate single-test negative invocations, each expected to fail before SQL connection; evidence of rejection, not ordinary passing test cases |
| `A4_OptOut.trx` | Deliberate no-approval invocation: 17 expected skips, distinct from zero-skip passing regressions |
| `A4_Integrated.trx`, `A4_SQL_Repeat.trx` | Accepted historical 278/278 and 17/17 results, independently repeated successfully in B |

Earlier SQL postflights showing empty history precede A3 browser data; current B before/after evidence preserves it. Legacy summary files retain their checkpoint context. B's `Test_Summary.json` is the current final result summary. No protected raw Sprint 4 SQL transcript content is copied or referenced as publishable evidence. Golden documents are verified by hash and remain unchanged.

Git portability note: Batch A `Final_SQL_Postflight.txt`, `Security_Patch_Dependency_Chain.log`, `Sql_Postflight.txt` and `Sql_Preflight_Elevated.txt` contain console padding. Historical documents retain their original local names. The additional preserved original is `A3_Initial_Checkpoint_Historical.md`. For the Git proposal, use corresponding `batch-b/sanitized/BatchA_<filename>` derivatives; only trailing padding/terminal blank lines differ, and exact source/derivative hashes are recorded. The originals remain untouched locally. The new B Pkcs chain log is similarly whitespace-sanitized without changing dependency content.
