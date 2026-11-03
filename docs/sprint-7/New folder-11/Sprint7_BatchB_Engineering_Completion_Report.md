# Sprint 7 Batch B Engineering Completion Report

Date: November 2, 2026 (America/New_York).

Repository: `C:\Engineering\GitHub\SyncBridge.Core`.

Authority: `Z:\InstallationPackages\Transfer_Folder\Sprint_7\CodexPrompt_Sprint_7_Batch_B.txt`.

Evidence root: `artifacts/test-evidence/sprint-7/batch-b/`, relative to the repository above. Evidence filenames below use this root unless otherwise stated.

## 1. Executive summary

**Engineering verification PASS; integration preparation complete; Sam's final review and formal Team Batch B acceptance pending.** Batch A is formally Team accepted under this prompt. Batch B independently exercised the accepted HTML5/PDF implementation, reconciled actual reports with SQL for both completed demonstrations, ran the complete applicable suite including the previously skipped 18 guarded SQL cases, and verified preservation.

Final suite: **319 passed, zero failed, zero skipped**, exit 0. Build: zero warnings/errors, exit 0. All four HTML5/PDF requests met the approximately ten-second demonstration target. All 15 manufacturing tables plus migration history remained unchanged. All 310 protected engineering files in the baseline remained unchanged. No production implementation correction was necessary.

No staging, commit, merge, push, fetch, branch switch or Sprint 8 implementation occurred. The exact proposed 20-file staging manifest is prepared for acceptance; it is not staged. Passing engineering checks do not constitute formal Team acceptance.

## 2. Authority and repository preflight

The approved corrected SRS, corrected SDD, amended DBDS, realigned WP-6 plan and Sprint 6-to-7 handoff establish the existing reporting boundary, seven datasets, approximately ten-second demonstration target, preservation and disposable-test safeguards. The accepted Batch A consolidation is now located at `docs/sprint-7/New folder-10/Sprint7_BatchA_Engineering_Closing_Report.md`; it was read and protected, not recreated or relocated. Historical A/A2/A3/A4/A5 reports and raw evidence remain preserved.

| Check | Actual result |
|---|---|
| Active branch | `codex/wp6-sprint7-ssrs-ebr` |
| HEAD / Sprint 6 baseline | `60dbecd1dfb94df8cc70fe77ee22cb5d5411738f` |
| Local main | `c432dd9b604b1d6022c8a0b01e5e2a0e0de0e3f0` |
| Sprint 6.1 feature ancestor of main | `53f54ca49ce6c89d86fd527daf8a197812f5e9fd`; ancestry check exit 0 |
| Sprint 6 baseline ancestor of main | Ancestry check exit 0 |
| Origin | `https://github.com/SyncBridge-Core/SyncBridge.git` |
| Live GitHub main | `c432dd9b604b1d6022c8a0b01e5e2a0e0de0e3f0` |
| Live GitHub Sprint 7 | `60dbecd1dfb94df8cc70fe77ee22cb5d5411738f` |
| Local remote-tracking refs | Match both live remote SHAs above |
| Staged paths | Zero |
| Tracked differences | ApplicationComposition.cs, Details.cshtml and inherited launchSettings.json only |

Live `git ls-remote` succeeded using narrowly scoped execution outside the normal sandbox. This is actual current GitHub access, distinct from local refs and historical Team handoff evidence. No Administrator token or Git configuration change was requested.

The full preflight inventory recorded three tracked modifications, 289 untracked files and 1,926 ignored files; this snapshot already includes the newly written Batch B baseline artifact. Final inventory records **three tracked modifications, zero staged paths, 353 untracked files and 1,926 ignored files**. Exact paths are in `Git_Before.txt` and `Git_Final.txt`; counts are in `Git_Final_Counts.json`. `Refs_Before.txt` retains all local/remote-tracking refs. The accepted 12-path implementation inventory is listed explicitly in section 10 and remains unchanged during B.

## 3. SSRS and security preflight/postflight

Native management API inspection confirms the deployed report `/SyncBridge/ElectronicBatchRecord` matches the local RDL exactly:

`E0F66D5D0B751B29D193990C92C4A8BDD4CD1D4D845F1301F7A3E09C4276CC5C` (SHA-256).

It references only `/SyncBridge/SyncBridgeCoreReadOnly`, SQL extension, `Data Source=SB-DEV-01;Initial Catalog=SyncBridgeCore`, enabled=true, stored Windows credentials=true, identity `SB-DEV-01\SyncBridgeReport`, ImpersonateUser=false. Native TestConnectForItemDataSource returned true before and after. Source settings, report references, hash and explicit report policy match across both inspections.

The existing item policy grants trusted local application/deployment identity `SB-DEV-01\Carlos` Content Manager access. That API caller is distinct from the dataset identity. Actual native report output identifies `SB-DEV-01\SyncBridgeReport`; direct reconciliation impersonates that SQL login. Permission inspection returns sysadmin=0 and db_owner=0. SELECT=1 and INSERT/UPDATE/DELETE/ALTER/CONTROL=0 on all 13 reporting tables: AppUser, AuditEvent, Batch, BatchStepExecution, Deviation, ElectronicSignature, Equipment, EquipmentVerification, Material, MaterialVerification, ProductionOrder, Recipe and RecipeStep.

No account, password, SQL grant, Windows logon policy, shared-source setting, SSRS report/policy deployment, firewall or service configuration was changed. The SSRS before inspection completed while regression was running; the manufacturing/file baselines preceded tests. This is not represented as a policy snapshot before the first test. The tests and inspection scripts do not alter SSRS configuration. Windows logon-right assignments remain Team-provisioned/attested; no new effective LSA export was attempted.

Evidence: `SSRS_Before.json`, `SSRS_After.json`, `Permissions_Before.txt`, `Permissions_After.txt`, `Inspect-Reporting.ps1`.

## 4. Seven-dataset reconciliation

Actual persisted Completed demonstrations:

- `A3-B-20261014`: `f3f3808b-a072-44c4-93ed-5039152fdda8`.
- `B-DEMO-20261023-001`: `d63797ae-da45-4f04-a73d-cf2814f8c2e2`.

| Dataset group | A3 rows | Second batch rows | Field/order comparison |
|---|---:|---:|---|
| Identification: Batch + ProductionOrder + Recipe | 1 | 1 | PASS |
| Workflow: BatchStepExecution + RecipeStep + actor | 2 | 2 | PASS |
| Materials: MaterialVerification + Material + actor | 1 | 1 | PASS |
| Equipment: EquipmentVerification + Equipment + actor | 1 | 1 | PASS |
| Deviations: Deviation + reporting actor | 1 | 1 | PASS |
| Signatures: ElectronicSignature + AppUser | 1 | 2 | PASS |
| Audit: AuditEvent + AppUser | 17 | 18 | PASS |

All **14 dataset instances, 50 records and 522 displayed fields** reconciled. Live native XML records match SQL query results under the dedicated reporting login, including exact identifiers, actors, timestamps, statuses, signature relationships, deviation disposition and audit affected IDs. Sequence equality verifies deterministic step/time ordering, including identifier tie-breakers. Source query checks confirm exact BatchId filtering, Completed predicate and ORDER BY. Each dataset rejects the suffix-manipulated GUID. The other demonstration's BatchId is absent from each native output.

The main reconciliation executes the RDL's SQL text; separately captured operational base-table counts independently match all 14 native dataset instances (`Independent_Row_Counts.json`). This distinguishes full-field/query reconciliation from independent cardinality checking. PDF extraction independently found all 522 fields. Both identification records correctly render persisted null final disposition as Not recorded; no approval/disposition was invented. Null lots/comments and other nullable values retain the same explicit representation.

Evidence: `Dataset_Reconciliation.json`, `Independent_Row_Counts.json`, native batch `.xml`/`.html`/`.pdf`, `SQL_Before.txt`, `PDF_Verification.json`.

## 5. Negative and security tests

| Check | Executed result / boundary |
|---|---|
| Anonymous / non-review role | Automated Razor redirect/forbidden checks pass; actual live reporting test rejects Production Operator with 403 |
| Incomplete and missing batches | Application fixture cases pass; incomplete request never reaches renderer; live missing business number returns 404 |
| Invalid format/binding | Invalid enum/binding cases reject; live invalid format returns 422 |
| Manipulated business number | Not found; no report rendered |
| Caller-supplied BatchId | Cannot replace server-resolved persisted batch; live request includes unrelated BatchId and still returns correct batch |
| Cross-batch output | Native outputs exclude other demonstration's BatchId; exact per-group field/row reconciliation passes |
| Native missing/malformed/suffix-tampered IDs | Additional live SSRS XML probes return HTTP 200 with zero record elements/attributes in all three cases |
| Persisted incomplete native probe | Not executed: operational data contains seven Completed batches and no incomplete batch; no manufacturing record created for this test |
| Completed-state enforcement | Automated incomplete/other-batch cases reject before rendering; every dataset independently contains Completed predicate |
| SSRS failure | Controlled malformed/unavailable responses and renderer failure produce sanitized errors/503; no live service stopped |
| Response protections | Authentication/reviewer authorization, no-store and sandboxed CSP tests pass; source retains nosniff/no-referrer |
| Manufacturing mutation | All 16 operational fingerprint rows unchanged; fixture preservation assertions pass |

These negative cases are included in 319 passes, not additional unique test counts. Reviewer roles retain the accepted general Completed-batch review scope; this increment does not invent per-batch ownership authorization. Native XML is engineering evidence, not a new manufacturing endpoint. HTTP loopback/default credentials remain the accepted local SSRS configuration; remote plaintext targets and redirects are restricted.

Evidence: `Test_Summary.json`, `Regression.trx`, `Regression.log`, `Native_Negatives.json`, `Verify-Negatives.ps1` and individual negative XML exports.

## 6. HTML5, PDF and performance

Source confirms browser Format=HTML5 and PDF Format=PDF; both load the same fixed deployed RDL and pass server-resolved BatchId. No RDL, datasets or renderer code was changed in B. Native HTML5 includes native script elements, but CSP remains restrictive and blocks execution; no script permission was added. Summary and deviation HTML modes also executed successfully.

| Batch | Actual HTML5 request | Actual PDF request | Ten-second demonstration target |
|---|---:|---:|---|
| A3-B-20261014 | 5,895.6486 ms | 3,834.0811 ms | PASS |
| B-DEMO-20261023-001 | 1,244.1362 ms | 1,461.3382 ms | PASS |

These are full response-byte timings through authenticated in-process HTTP, Application lookup and real local SSRS, during the full regression run. They are not physical-browser timings or load/cold-start/network guarantees.

Fresh native PDFs contain six and seven Letter pages (612 × 792 points), all 522 fields, no missing fields, body content and numbering on every page, and zero form widgets. Codex inspected all **13 freshly rendered page images**: no clipping or overlap observed; headings, long identifiers, comments and footers remain readable. The bundled Python runtime performed text/page checks and PNG rendering; no PDF product engine or dependency was installed.

Team's accepted prior desktop rendering PASS, demonstrated approximately 565 px no-overlap observations and readable six-page PDF confirmation are retained as **Team visual evidence**, not a new automated browser run. Horizontal scrolling remains at narrow widths; demonstrated sections do not establish exhaustive responsive or browser/device coverage. Batch B performed no physical-browser automation.

Evidence: `Timings.json`, `PDF_Verification.json`, `PDF_Check.log`, `Visual_Review.json`, `Verify_Pdf.py`, native exports, `.pdf.txt` and all `.page-*.png` files. Prior Team observations are recorded in the accepted A closing report.

## 7. Full regression and disposable lifecycle safeguards

Build command:

```powershell
dotnet build src/SyncBridge.Core.slnx --no-restore --disable-build-servers -m:1 -v:minimal
```

Result: exit 0, zero warnings/errors; existing restored dependencies were used.

Before enabling SQL tests, code review independently confirmed:

- Mandatory explicit SQL opt-in and connection environment variable; no fallback to application/User Secrets connection.
- Exact target `localhost/SyncBridgeCore_IntegrationTests`, Windows authentication, no attached file/failover/user instance; canonical nonpooled connection.
- Session-owned exclusive `SyncBridge.Core.Tests:SyncBridgeCore_IntegrationTests` application lock with zero timeout.
- Existing database reset requires exact DB identity and extended property `SyncBridgeTestOwner=SyncBridge.Core.Tests.Disposable.v1`.
- New disposable database is marked and reverified before migration/seeding; expected 15 tables, 11 seed rows and accepted migration required.
- Drop repeats identity/marker checks; no forced disconnect, SINGLE_USER, computed target or server-wide cleanup. Failure cleanup is ownership guarded.
- Serialized SQL collection and all SQL fixtures use the guarded connection. Operational reporting is a separate existing-data-only test path.

Read-only live preflight found caller `SB-DEV-01\Carlos`, sysadmin=1, disposable database absent and lock acquisition result 0. Thus no pre-existing database needed deletion or marker inference. The preflight lock was released with its connection; the actual fixture independently reacquired and held the same lock during its lifecycle. The fixture created/marked/migrated/seeded only the authorized disposable database. It logged cleanup with zero manufacturing-history rows and two reference orders, then dropped its owned database. Postflight DB_ID is NULL.

The full run used:

```powershell
$env:SYNCBRIDGE_APPROVED_LIVE_SQL='1'
$env:SYNCBRIDGE_TEST_SQL_CONNECTION='Server=localhost;Database=SyncBridgeCore_IntegrationTests;Integrated Security=True;TrustServerCertificate=True'
$env:SYNCBRIDGE_APPROVED_REPORTING='1'
$env:SYNCBRIDGE_REPORT_EVIDENCE='C:\Engineering\GitHub\SyncBridge.Core\artifacts\test-evidence\sprint-7\batch-b'
dotnet test tests/SyncBridge.Core.Tests/SyncBridge.Core.Tests.csproj --no-build --no-restore --logger 'trx;LogFileName=Regression.trx' --results-directory artifacts/test-evidence/sprint-7/batch-b --logger 'console;verbosity=normal'
```

**319/319 executed and passed; zero failed, skipped, aborted or timed out**, exit 0, approximately 2.4094 minutes. The 18 formerly skipped SQL cases all passed, including constraints, transactional rollback, concurrent commands, optional-field fidelity, evidence/Completed protection and actual SQL/UI/API equivalence. They add to the previously applicable 301 passes; no subset is counted twice. No destructive test targeted `SyncBridgeCore`; protected legacy `SyncBridge_Core` was not accessed.

Evidence: `Build.log`, `Regression.log`, `Regression.trx`, `Test_Summary.json`, `Disposable_Preflight.txt`, `Disposable_Postflight.txt`, reviewed `tests/SyncBridge.Core.Tests/SqlServerTestDatabase.cs`.

## 8. Preservation and scope compliance

`Protected_Before.json` and `Protected_After.json` compare SHA-256 for inherited engineering files, historical Sprint 4–7 evidence/reports, accepted uncommitted reporting implementation, launchSettings.json, golden documents and User Secrets. **310 protected engineering files unchanged; zero failures.** Four golden DOCX files plus the approved repository handoff are protected. Six existing User Secrets files were hashed without copying secret contents. The inherited `https://0.0.0.0:7099;http://localhost:5219` binding remains unchanged and is excluded from staging.

The baseline also records **1,910 bin/obj cache files**, with zero hash changes detected in this run. Cache files are separately classified; this result does not turn generated outputs into staging candidates or claim every future build preserves caches byte-for-byte. `Authority.json` confirms all five governing document hashes match the accepted Batch A source-document hashes and records the current prompt hash.

Operational SQL before/after snapshots are identical: all **15 manufacturing tables plus migration history**, all seven Completed demonstrations, 127 audit rows and eight signatures remain intact. SSRS definition/source identity/policy and effective SQL permissions match. Accepted Sprint 6.1 main/feature ancestry and HEAD remain intact; local Git state plus protected source hashes show no HMI changes by B.

No manufacturing schema, migrations, report engine, REST endpoint, operational database, User Secrets, credential, Windows policy or production source changes were made. The only database lifecycle mutation was the expressly authorized ephemeral integration-test mechanism. No reporting database or new product storage was introduced.

`Diagnostics.md` records routine diagnostic corrections: initial SOAP XPath/source-name error, null result in the unavailable incomplete-batch probe, inaccessible WindowsApps Python alias and the nonfatal PowerShell InitializeDefaultDrives warning. These were evidence-tool issues, not production defects; successful repeats and final TRX determine the reported results. Induced SQL errors in passing rollback tests are expected. No policy bypass was used.

## 9. Requirements/design traceability and limitations

| Authority | Batch B evidence |
|---|---|
| FR-011, UC-005, SRS 5.2 | Actual authenticated Completed EBR, native HTML5/PDF, seven-group SQL/XML/PDF reconciliation |
| BR-007 | Exact persisted fields, null disposition, deterministic order and independent row counts |
| FR-010, DBDS 8.2 | Existing Review by Exception retained; executed normalized deviation/summary modes |
| NFR-001 | Authorization, Completed gate, isolated parameters, restricted dataset identity, native negatives and CSP tests |
| NFR-002 | Four measured HTML5/PDF requests below approximately ten seconds |
| NFR-003 | Unchanged operational fingerprints; guarded SQL rollback, concurrency, integrity and lifecycle regression |
| SDD 5.4/7.4–7.5, ADR-004/010 | Existing Application/Infrastructure split and native SSRS boundary unchanged |
| DBDS 8.1, AMD-001/002 | Seven normalized datasets, complete deviation/audit identifiers and optional fields |
| WP-6 4.7 / approved handoff | Reconciliation, negatives, timings, full regression, preservation and gated Git readiness |

No demonstrated functional defect remains. Limits: two Completed reporting demonstrations, not production volume or every empty/null combination; incomplete live native case unavailable, with automated fixture coverage instead; reduced-mode HTML exercised, separate reduced-mode PDFs not sampled; no new physical-browser coverage; narrow horizontal scrolling remains; no new Windows effective-policy export; no regulatory certification or production deployment claim. Sam review and formal Team B acceptance remain required.

## 10. Exact staging manifest and Git readiness

`Proposed_Staging_Manifest.txt` is an exact **20-file allowlist**, with hashes/status and candidate checks in `Proposed_Contents.json` and `Candidate_Checks.json`. The proposed contents are:

1. `src/SyncBridge.Core.Application/Contracts/Reporting/IBatchReporting.cs`
2. `src/SyncBridge.Core.Application/Services/BatchReportService.cs`
3. `src/SyncBridge.Core.Infrastructure/Reporting/ReportBatchReader.cs`
4. `src/SyncBridge.Core.Infrastructure/Reporting/SsrsReportRenderer.cs`
5. `src/SyncBridge.Core.Infrastructure/Reporting/Reports/ElectronicBatchRecord.rdl`
6. `src/SyncBridge.Core.Infrastructure/ApplicationComposition.cs`
7. `src/SyncBridge.Core/Pages/Batches/Report.cshtml`
8. `src/SyncBridge.Core/Pages/Batches/Report.cshtml.cs`
9. `src/SyncBridge.Core/Pages/Batches/Details.cshtml`
10. `tools/Reporting/Deploy-Ebr.ps1`
11. `tests/SyncBridge.Core.Tests/BatchReportingTests.cs`
12. `tests/SyncBridge.Core.Tests/LiveBatchReportingTests.cs`
13. `docs/sprint-7/New folder-10/Sprint7_BatchA_Engineering_Closing_Report.md`
14. `docs/sprint-7/Sprint7_BatchB_Engineering_Completion_Report.md`
15. `artifacts/test-evidence/sprint-7/batch-b/Test_Summary.json`
16. `artifacts/test-evidence/sprint-7/batch-b/Preservation_Summary.json`
17. `artifacts/test-evidence/sprint-7/batch-b/Independent_Row_Counts.json`
18. `artifacts/test-evidence/sprint-7/batch-b/Timings.json`
19. `artifacts/test-evidence/sprint-7/batch-b/PDF_Verification.json`
20. `artifacts/test-evidence/sprint-7/batch-b/Git_Readiness.json`

This includes the accepted 12 implementation/test/deployment paths, two consolidated engineering reports and six compact evidence files. It excludes inherited launchSettings.json, secrets/credentials/private configuration, unrelated untracked history, ignored build outputs, raw logs/TRX, native report exports and PNGs. Excluded engineering artifacts remain preserved locally; exclusion is not deletion. No indiscriminate add or actual staging was performed.

Tracked whitespace checks, candidate text/credential-pattern review, dependency boundary checks, exact-path inventory, source/deployed RDL equality, branch ancestry and live synchronization checks support readiness. Pattern review is a bounded scan of proposed content, not a guarantee against every possible secret. `Candidate_Checks.json` records exact checks and outcomes. Current main retains accepted Sprint 6.1 history; Sprint 7 remains at the accepted Sprint 6 baseline with uncommitted reporting work, as intended before consolidation. A later non-fast-forward merge must preserve both histories and resolve any genuine conflict under the accepted workflow; no merge is claimed tested by this preparation.

## 11. Final recommendation and stop gate

**Recommend Team acceptance of Batch B and the prepared Sprint 7 manifest after Sam's final engineering review.** Full applicable regression is complete, including all 18 guarded SQL cases. Reporting fidelity, negative/security behavior, native HTML5/PDF, measured performance and protected-state preservation passed within the explicit coverage limits.

This new Markdown report and Batch B evidence are untracked and unstaged. Existing Git authorization remains valid, but the prompt requires recording formal Batch B acceptance before staging/consolidated commit, non-fast-forward merge, post-merge regression, pushes, independent remote ancestry checks and Sprint 8 branch publication. None of those gated actions was executed. Stop here for Sam and the Team; do not begin Sprint 8 implementation.
