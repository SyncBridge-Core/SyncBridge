# Sprint 7 Batch A Engineering Closing Report

Date: November 2, 2026 (America/New_York).

Repository: `C:\Engineering\GitHub\SyncBridge.Core`.

Authority: `Z:\InstallationPackages\Transfer_Folder\Sprint_7\CodexPrompt_Sprint_7_Batch_A_Closure.txt`.

## 1. Engineering disposition

**PASS for the implemented and demonstrated Batch A scope; recommended for Team acceptance, subject to Sam's final engineering review and the limitations below.** Native SSRS provides the completed-batch Electronic Batch Record (EBR), browser viewing, PDF export, batch summary and deviation views. The browser integration now requests native HTML5. The Team confirms that this corrects the observed HTML4.0 overlap, that desktop rendering passes, and that no overlap was observed in the demonstrated sections at approximately 565 px. Horizontal scrolling remains at narrow widths.

This report consolidates historical executed evidence and the visual observations supplied in the closure prompt. It does not claim new browser coverage, new test executions or a new production/configuration audit. Sam's final review and formal Team acceptance are not recorded as completed.

## 2. Chronology and authoritative evidence

All report paths below are relative to the repository above. Historical reports remain unchanged, including their accurate blocked/pending status at the time.

| Stage | Engineering outcome | Existing report |
|---|---|---|
| Batch A, October 29 | Mandatory preflight stopped implementation: installed SSRS lacked catalog/URL configuration. SQL and existing completed records were available. | `docs/sprint-7/New folder-3/Sprint7_BatchA_Completion_Report.md` |
| A2, October 29 | Team-configured portal and native SOAP services worked. Restricted Windows reporting identity was missing; service-context connection attempt returned `rsInvalidIntegratedSecuritySettings`. No developer-sysadmin reporting workaround was used. | `docs/sprint-7/New folder-4/Sprint7_BatchA_Completion_Report.md` |
| A3, October 29 | Authorized account creation failed with `AccessDenied,Microsoft.PowerShell.Commands.NewLocalUserCommand`; per-command sandbox escalation did not supply an Administrator token. Team provisioning handoff followed. | `docs/sprint-7/New folder-5/Sprint7_BatchA3_Completion_Report.md` |
| Rights investigation | Stored Windows credentials require a suitable Windows logon right; with interactive logon denied, batch logon is the intended mechanism. Deny-batch conflicts. Effective policy export was inaccessible; later provisioning is Team-attested. | `docs/sprint-7/New folder-6/Sprint7_SSRS_Batch_Logon_Rights_Verification.md` |
| A4, October 29–30 | Following Team prerequisite provisioning, implemented and deployed one RDL, seven datasets, application security and native HTML/PDF integration. Executed reconciliation, PDF review and regression. | `docs/sprint-7/New folder-7/Sprint7_BatchA4_Completion_Report.md` |
| A5, November 2 | Team found actual Edge text overlap. Saved-file browser inspection was rejected by browser security policy; no bypass or speculative production correction was performed in that attempt. | `docs/sprint-7/New folder-8/Sprint7_BatchA5_Completion_Report.md` |
| Corrective continuation, A6 prompt | Switched HTML4.0 to HTML5 and separated fresh evidence output from historical A4 artifacts. Functional/PDF regression passed; physical visual acceptance was pending at report time. | `docs/sprint-7/New folder-9/Sprint7_BatchA5_Correction_Report.md` |
| Closure | Team supplies desktop PASS, demonstrated narrow-window no-overlap observations and readable six-page PDF confirmation. These resolve the reported visual acceptance gate within the stated coverage. | Closure prompt identified above |

The A6-prompt continuation is included as the correction to Batch A5, not a separate claim of Batch B work. Raw evidence remains in `artifacts/test-evidence/sprint-7/batch-a/`, `batch-a2/`, `batch-a3/`, `batch-a4/` and `batch-a5/`.

## 3. Implementation inventory and architecture

| Repository path | Responsibility |
|---|---|
| `src/SyncBridge.Core.Application/Contracts/Reporting/IBatchReporting.cs` | Reader/renderer contracts, request enums and report DTOs |
| `src/SyncBridge.Core.Application/Services/BatchReportService.cs` | Trusted reviewer authorization, option validation, persisted batch resolution and Completed gate |
| `src/SyncBridge.Core.Infrastructure/Reporting/ReportBatchReader.cs` | Minimal EF Core AsNoTracking lookup |
| `src/SyncBridge.Core.Infrastructure/Reporting/SsrsReportRenderer.cs` | Native execution SOAP integration; final formats HTML5 and PDF |
| `src/SyncBridge.Core.Infrastructure/Reporting/Reports/ElectronicBatchRecord.rdl` | Single definition, seven datasets, All/Summary/Deviations modes |
| `src/SyncBridge.Core.Infrastructure/ApplicationComposition.cs` | Application contracts/services, reporting options and HTTP client registration |
| `src/SyncBridge.Core/Pages/Batches/Report.cshtml` | Report route |
| `src/SyncBridge.Core/Pages/Batches/Report.cshtml.cs` | Application delegation, controlled errors, HTML/PDF responses and security headers |
| `src/SyncBridge.Core/Pages/Batches/Details.cshtml` | Completed-record reviewer links for EBR, PDF, summary and deviations |
| `tools/Reporting/Deploy-Ebr.ps1` | Native named-report deployment against the existing approved source; no password provisioning |
| `tests/SyncBridge.Core.Tests/BatchReportingTests.cs` | Service, Razor security and SOAP protocol cases |
| `tests/SyncBridge.Core.Tests/LiveBatchReportingTests.cs` | Opted-in existing-data-only integration/reconciliation; correction adds optional `SYNCBRIDGE_REPORT_EVIDENCE` directory override |

The correction changes the browser renderer literal and test evidence destination only. It does not change the RDL, SQL datasets, PDF renderer or CSP. No Domain, controller, package, manufacturing persistence mapping, migration or new storage changes were introduced. Application owns authorization/orchestration; Infrastructure owns EF/SQL/SSRS. Production PDF generation uses SSRS, not a custom PDF engine.

Production Supervisor and Quality Engineer roles can request existing Completed batches. Application resolves the business batch number to its persisted BatchId; callers cannot choose SQL, report paths, endpoints or execution context. Dataset guards independently require Completed and an exact-length GUID. Existing reviewer scope is retained; no new per-batch ownership model is claimed.

## 4. SSRS configuration, identities and permissions

Executed A4 evidence establishes native SSRS `16.0.1118.33`, service `NT SERVICE\SQLServerReportingServices`, portal `http://127.0.0.1/Reports`, native service `http://127.0.0.1/ReportServer`, and functional catalog configuration. Catalog names `ReportServer` and `ReportServerTempDB` were Team-supplied; no direct catalog database inspection is claimed.

| Setting | Verified value |
|---|---|
| Deployed report | `/SyncBridge/ElectronicBatchRecord` |
| Shared source | `/SyncBridge/SyncBridgeCoreReadOnly` |
| Extension / connection | SQL / `Data Source=SB-DEV-01;Initial Catalog=SyncBridgeCore` |
| Credential retrieval | Store; WindowsCredentials=true; enabled=true; ImpersonateUser=false |
| Dataset identity | `SB-DEV-01\SyncBridgeReport` |
| Connection check | Native TestConnectForItemDataSource=true |
| Deployment | Zero warnings; downloaded definition matches source |

RDL SHA-256, unchanged through correction:

`E0F66D5D0B751B29D193990C92C4A8BDD4CD1D4D845F1301F7A3E09C4276CC5C`.

Actual native report data identifies the dedicated reporting account. SQL impersonation checks show sysadmin=0, db_owner=0 and SELECT=1, INSERT/UPDATE/DELETE/ALTER/CONTROL=0 on all 13 reporting tables: AppUser, AuditEvent, Batch, BatchStepExecution, Deviation, ElectronicSignature, Equipment, EquipmentVerification, Material, MaterialVerification, ProductionOrder, Recipe and RecipeStep. Verification inspected effective permissions; it did not attempt production writes.

The Team provisioned the Windows account, stored credentials, SQL login/user/read role and logon rights. Local/RDP interactive denial remains intended; batch logon must be allowed without an applicable deny-batch assignment. Codex could not independently enumerate effective LSA policy; successful stored-credential rendering is functional corroboration, not a policy export. No automatic SSRS right-provisioning guarantee is asserted.

The native API/application identity is distinct from the SQL data-source identity. The report's explicit item policy uses trusted local `SB-DEV-01\Carlos` as Content Manager; it does not grant general end users direct report access. A different application Windows identity requires an explicit SSRS access review. A4 deployed the named report and its item policy; it did not change the existing shared-source password, SQL grants, Windows rights, service identity or firewall. The HTML5 continuation made no SSRS configuration change.

## 5. Seven datasets and data fidelity

Each dataset is SELECT-only, uses the approved shared source, filters by resolved BatchId and Completed status, and orders deterministically with an identifier tie-breaker.

| Group | Principal normalized sources | A3 rows | Second batch rows |
|---|---|---:|---:|
| Identification | Batch, ProductionOrder, Recipe | 1 | 1 |
| Workflow | BatchStepExecution, RecipeStep, completing AppUser | 2 | 2 |
| Materials | MaterialVerification, Material, verifying AppUser | 1 | 1 |
| Equipment | EquipmentVerification, Equipment, verifying AppUser | 1 | 1 |
| Deviations | Deviation, reporting AppUser | 1 | 1 |
| Signatures | ElectronicSignature, AppUser | 1 | 2 |
| Audit | AuditEvent, AppUser | 17 | 18 |

Executed demonstrations: `A3-B-20261014` (`f3f3808b-a072-44c4-93ed-5039152fdda8`) and `B-DEMO-20261023-001` (`d63797ae-da45-4f04-a73d-cf2814f8c2e2`). Native XML/SQL comparison checked all 14 dataset instances, 50 records and 522 fields, including order. A4 independent base-table counts matched 14/14 instances. The correction reran field/order reconciliation. Suffix-tampered GUIDs returned no rows; cross-batch identifiers were excluded.

Audit affected identifiers, deviation codes, actors, signatures, timestamps, comments and disposition remain factual persisted evidence. Nulls display as Not recorded; a null final disposition is not converted into an invented approval. Unsupported site, strength, attachments or signature images were not fabricated.

## 6. HTML5 correction and visual evidence

Team DevTools observations before correction showed approximately 160 px content height in a 701 px-wide element within fixed-height parent cells. Legacy native HTML contained nested fixed-height structures. The precise browser sizing mechanism remains a hypothesis; selecting HTML5 removed the observed old fixed-cell pattern without CSS overrides or RDL edits. Microsoft describes HTML5 as the modern native browser renderer, with HTML4.0 fallback: [Rendering extensions overview](https://learn.microsoft.com/en-us/sql/reporting-services/extensions/rendering-extension/rendering-extensions-overview?view=sql-server-ver17).

The HTML5 output contains native script elements. Existing CSP remains restrictive and blocks script execution; no permission was weakened. Responses retain no-store, nosniff, no-referrer and sandboxed CSP. Historical A4 statements about zero script elements describe HTML4.0 output and must not be applied to final HTML5 bytes.

**Team-supplied closure observations:** desktop EBR rendering PASS; HTML4.0 overlap corrected by native HTML5; no overlap observed in demonstrated sections at approximately 565 px; horizontal scrolling still required at narrow widths. These are not Codex browser measurements or proof of every section/browser/device at that width. No new screenshots or computed after-dimensions were generated during closure.

## 7. PDF and automated verification

Executed A4 verification exported six-page and seven-page native PDFs, US Letter 612 × 792 points. All 13 page images were individually inspected at A4. Correction regression generated new six-/seven-page exports and found all 522 fields (252/270), body content and numbering on every page, with zero form widgets. No new manual page inspection was claimed by the correction report. The Team now confirms the exported six-page EBR PDF is readable and correctly formatted. These are ordinary SSRS exports, not a certified digital-signature subsystem.

| Executed check | Result |
|---|---|
| A4 build and correction build | Exit 0, zero warnings/errors |
| A4 focused reporting/security/protocol/live integration | 17 passed, zero failed/skipped |
| A4 final full suite | 301 passed, zero failed, 18 skipped, 319 total |
| Correction full suite | 301 passed, zero failed, 18 skipped, 319 total |
| Correction native HTML5/XML/SQL reconciliation | PASS for both stated batches and all seven groups |
| Summary/deviation browser modes | Executed successfully |
| Full EBR PDF content/pagination regression | PASS for both stated batches |

The focused cases are included in the full-suite totals; counts must not be added. The full suite comprises 284 existing passing non-SQL cases, 16 new non-live reporting cases and one new live read-only integration case. Reporting opt-in was enabled; 18 guarded SQL mutation/lifecycle tests remained skipped. No disposable database was created or dropped. Historical initial failures and corrected diagnostics remain retained; final logs/TRX govern these results.

Tests cover roles, anonymous/non-review rejection, Completed gate, missing/invalid input, persisted batch resolution, parameter isolation, SOAP errors and controlled unavailable responses, security headers and live native output. Live HTTP exercised an authenticated WebApplicationFactory host against real SQL/SSRS; it was not physical-browser automation. Server failure handling used controlled responses, not stopping live services.

A4 measured HTML/PDF request times were 1,973.7241/967.4577 ms for A3 and 1,603.3763/2,040.5237 ms for the second batch. These meet the approximately ten-second demonstration target for that measured run; they are development in-process integration timings, not new HTML5 performance measurements, load tests or network/cold-start guarantees. No tests were rerun solely to author this closing report.

## 8. Preservation and current repository facts

Historical A4 preservation checked 178/178 engineering files unchanged, User Secrets, five approved documents and all 16 manufacturing/migration fingerprint rows. Expected SDK bin/obj regeneration affected 399 cache entries, including two obsolete compressed entries removed by the SDK; byte-identical ignored build-cache preservation is not claimed. Correction evidence reports **260 protected engineering files unchanged**, no preservation failures, and all 16 fingerprint rows unchanged. RDL hash stayed identical.

These measured preservation results apply to their recorded execution windows. Closure does not claim a newly repeated filesystem/database fingerprint audit. This closure operation only creates this Markdown report, reads existing evidence and performs non-mutating Git inspection. Existing reports/evidence, source, launchSettings.json, User Secrets, database state and configuration are not edited by closure.

Closure-time local checks confirm branch `codex/wp6-sprint7-ssrs-ebr`, HEAD `60dbecd1dfb94df8cc70fe77ee22cb5d5411738f`, and no staged paths. Tracked differences remain ApplicationComposition.cs, Details.cshtml and inherited launchSettings.json. The inherited HTTPS binding `https://0.0.0.0:7099;http://localhost:5219` is outside the reporting implementation inventory. New implementation/evidence remains uncommitted. This closing report is a new unstaged file; no staging or commit is authorized.

Accepted main `c432dd9b604b1d6022c8a0b01e5e2a0e0de0e3f0` and Sprint 6.1 history preservation are historical verified evidence, not a new remote check. No live GitHub synchronization or metadata-write test was performed for closure. PowerShell emitted the nonfatal warning `Attempting to perform the InitializeDefaultDrives operation on the 'FileSystem' provider failed.` Successful local commands still returned exit 0.

## 9. Requirements traceability

| Authority | Delivered behavior / evidence |
|---|---|
| FR-011, UC-005, SRS 5.2 | Completed EBR, seven datasets, native HTML5/PDF and authenticated application integration |
| BR-007 | SQL/XML/PDF field reconciliation; factual persisted evidence and null handling |
| FR-010 / DBDS 8.2 | Existing Review by Exception retained; normalized deviation view |
| NFR-001 | Reviewer roles, Completed gate, restricted data identity, isolated parameters, CSP/no-store and scoped report access |
| NFR-002 | Recorded four-request A4 timing evidence below demonstration target, with stated limits |
| NFR-003 | SELECT-only datasets and unchanged manufacturing/migration fingerprints |
| SDD 5.2/5.4; ADR-004/010 | Application orchestration/contracts and Infrastructure EF/SQL/native SSRS integration |
| DBDS 8.1; AMD-001/002 | Seven normalized evidence groups, complete deviation/audit identifiers |
| SDD 7.4/7.5; WP-6 4.7 | Prototype-informed completed-record review, single report, summary/deviation modes, PDF and runtime evidence |

Traceability consolidates the mappings recorded in A4; it does not expand acceptance to unimplemented Sprint 7 work.

## 10. Remaining limitations and recommendation

- Narrow layouts require horizontal scrolling. Approximately 565 px no-overlap confirmation covers demonstrated sections, not exhaustive responsive/tablet/browser coverage.
- The 18 SQL mutation/lifecycle tests are skipped, not passed. Evidence covers two existing Completed batches, not production-scale loads or every empty/null combination.
- Summary and deviation HTML modes were exercised; separate reduced-mode PDF samples were not produced.
- Windows effective logon policy remains Team-attested; the restricted stored-credential path was functionally verified. A new application Windows identity needs SSRS access review.
- Prior browser tooling restrictions remain respected. Team visual observations close the demonstrated defect; Codex did not independently reproduce final desktop/narrow geometry.
- Final Sam review, formal Team acceptance and any later authorized Git publication remain outstanding. No claim of regulatory certification or production deployment readiness is made.

**Recommendation:** accept Batch A's implemented engineering scope and demonstrated HTML5/PDF correction after Sam reviews this consolidated report and retained evidence, recording the narrow-width scrolling limitation. No unresolved functional blocker is recorded within the demonstrated scope. Do not infer authorization for Batch B or Git integration from this recommendation.

## 11. Evidence locator

Under `artifacts/test-evidence/sprint-7/batch-a4/`: `Deployment.json`, `Report_Access_Postflight.json`, `SSRS_DataSource_Preflight.txt`, `Permissions_Preflight.txt`, `Architecture_Checks.json`, `Dataset_Reconciliation.json`, `Independent_Row_Counts.json`, `PDF_Verification.json`, `Visual_Review.json`, `Timings.json`, `Build.log`, `Focused_Reporting_Final.trx`, `Regression_Final.trx`, `Preservation_Summary.json`, `SQL_Before.txt`, `SQL_After.txt`, `Final_Git_Inventory.txt`, native batch HTML/PDF/XML and page images.

Under `artifacts/test-evidence/sprint-7/batch-a5/`: historical `Static_Inspection.json`; corrective `A6_Layout_Comparison.json`, `A6_Build.log`, `A6_Regression.log`, `A6_Regression.trx`, `Dataset_Reconciliation.json`, `PDF_Verification.json`, `A6_Protected_Before.json`, `A6_Preservation.json`, `A6_SQL_Before.txt`, `A6_SQL_After.txt`, `A6_Permissions.txt`, `A6_Git_Inventory.txt`, native output and page images. Earlier prerequisite evidence remains in the A/A2/A3 directories listed above.

No commit, merge, push, branch switch or Batch B action was performed. Stop for Sam's final engineering review.
