# Sprint 5 Batch A evidence index

Current after A4: permanent SQL isolation uses only explicit `SYNCBRIDGE_TEST_SQL_CONNECTION` for owned disposable `localhost` / `SyncBridgeCore_IntegrationTests`. Full suite 278/278 passes and independent SQL repeat 17/17 passes, both with zero failures/skips. All 15 development manufacturing tables and retained A3 evidence match before/after. The test catalog is absent after disposal. Build/advisory/architecture checks pass; no Git integration. The earlier stop below is resolved by the authorized A4 correction.

| A4 artifact | Actual meaning |
|---|---|
| `A4_Build.log` | Solution build: 0 warnings/errors |
| `A4_Integrated.trx`, `.log` | 278 passed / 0 failed / 0 skipped; 72.189 seconds TRX wall time |
| `A4_SQL_Repeat.trx`, `.log` | Independent reprovisioned SQL run: 17 passed / 0 failed / 0 skipped; 28.502 seconds |
| `A4_Guard_Missing.trx`, `.log` | Deliberate negative check: expected 1 failure before connection; inherited development config supplies no fallback |
| `A4_Guard_Development.trx`, `.log` | Deliberate negative check: expected 1 failure before connection for the development catalog |
| `A4_OptOut.trx`, `.log` | No approval flag: all 17 SQL tests skip without provisioning |
| `A4_Verification_Summary.json` | Separate actual regression, negative-check and opt-out counts/durations |
| `A4_ReadOnly_Preservation.ps1` | Reproducible SELECT-only development check |
| `A4_Development_Before.json`, `A4_Development_After.json`, `A4_Preservation_Checks.json` | Identical all-table row hashes/counts and migration history; Completed A3 records/evidence retained; test catalog absent |
| `A4_Advisories.log`, `A4_Architecture_Git_Checks.json` | No reported advisories; Pkcs 9.0.21, manufacturing boundaries/hashes, protected historical hashes and Git state preserved |

## Historical pre-A4 results

Historical bounded security verification: `Security_Patch_Dependency_Chain.log` confirms the EF SQL Server → SqlClient → Pkcs chain. `Security_Patch_Restore.log`, `Security_Patch_Advisories.log` and `Security_Patch_Build.log` pass with Pkcs 9.0.21 and no reported advisories/build warnings. `Security_Patch_Integrated.trx` / `.log` records 277 passed, 1 failed, 0 skipped: the live SQL empty-history seed assertion finds the retained A3 batch. `Security_Patch_Architecture_Checks.json` records preserved source boundaries/hashes and Git state. Execution stopped for Team review; no further browser run. The earlier A3 278/278 result below preceded browser data creation.

Historical A3: exact-certificate trust and normal HTTPS/browser access are verified; permanent Development-only User Secrets provisioning/repeated verification succeeded for the two Team-retained identities. The bounded Pkcs 9.0.21 correction and test-host identity isolation are verified by `A3_Patched_Build.log`, `A3_Identity_Tool_Final_Build.log` (0 warnings/errors) and `A3_Final_Integrated.trx`/`.log` (278 passed, 0 failed/skipped). Interactive core UC-001–UC-004 and stale completed-record POST rejection executed against live SQL; 18 sanitized screenshots are indexed in `A3_Screenshot_Index.json`. See the final A3 report for the explicit role/browser-driver coverage limits. A/A2 results below are historical rather than unresolved current blockers.

All results are from this execution, not inherited Sprint 4 counts. Final source build: `Final_Build_Verified.log`, 0 warnings/errors. Final suite: `Final_Integrated.trx` / `Final_Integrated_Test.log`, 278 passed, 0 failed/skipped (261 non-SQL, 17 live SQL).

| Artifact | Meaning |
|---|---|
| `Baseline_NonSql.trx` | Starting baseline: 235 passed |
| `Focused_Http.trx` | Superseded: 25 passed, 2 test-input failures |
| `Focused_Http_Retest.trx` | Corrected inputs: 27 passed |
| `Full_NonSql.trx` | Intermediate suite: 259 passed |
| `Final_NonSql.trx` | Superseded: 260 passed, 1 identifier classification failure, corrected before final integrated run |
| `LiveSql_Regression.trx` | Intermediate live SQL: 17 passed |
| `Final_Integrated.trx` | Final source: all 278 passed, including 26 new Razor workflow cases |
| `Browser_Host_Workflow.trx` | Separate unsuccessful HTTPS browser-host experiment: local certificate UntrustedRoot; not a passing browser walkthrough |
| `Sql_Preflight.txt` | Initial sandbox connectivity failure |
| `Sql_Preflight_Elevated.txt`, `Sql_Postflight.txt`, `Final_SQL_Postflight.txt` | Successful approved local SQL checks; schema and seed baseline unchanged; final file follows the integrated suite |
| `Final_Build.log` | Sandbox build process failure with no compiler diagnostics |
| `Final_Build_Verified.log` | Successful elevated build with build servers disabled |
| `Verification_Summary.json` | Actual counters extracted from every TRX, including failed attempts |
| `Source_Register.json` | Authoritative document paths and SHA256 |
| `Production_Inventory.json`, `Production_Size.json` | 15 new / 12 modified production files; approximately 495 added, 27 removed, net +468 physical lines |
| `Architecture_Git_Checks.json`, `Source_Security_Check.json` | Page persistence-reference, migration, staging, branch and credential-pattern checks |
| `Protected_File_Checks.json` | Original protected Sprint 4 files retain initial hashes |
| `Dotnet_Environment.txt` | Actual OS, SDK and installed runtimes; initial full `dotnet --info` attempt hit sandbox service-manager access limits; version/runtime queries succeeded |
| `Final_Working_Tree.txt` | Unstaged final change inventory |

Interactive browser QA was attempted but did not successfully reach the workflow: initial in-app navigation returned connection refused, and the optional Kestrel workflow host failed certificate validation before its scenario. Temporary host code was removed; no certificate validation/trust changes were made. Native WebApplicationFactory HTTP/rendering evidence is recorded separately and does not substitute for Team browser acceptance.

No account hashes, production passwords, cookies, antiforgery token captures or raw protected Sprint 4 transcripts were added to this evidence. SQL files contain only the approved schema/seed metadata and sanitized local connectivity diagnostics. Source checks are focused inspections, not a general secret-scanner or regulatory certification claim.

Batch A2 follow-up: `A2_HTTPS_Host.log`, `A2_Certificate_Validity.log`, `A2_Certificate_Trust.log`, and `A2_HTTPS_Diagnostics.json` record successful existing-profile startup, a valid but untrusted real-user development certificate, HTTPS UntrustedRoot failure and browser ERR_CERT_AUTHORITY_INVALID. Trust installation awaits explicit Team authorization; no interactive walkthrough or screenshots have been produced.

Batch A3 checkpoint: Team authorization has now been received. `A3_Certificate_Trust.log` records the standard command reaching the normal Windows confirmation; completion and the subsequent trust check remain pending. `A3_Account_Configuration_Check.json` records missing approved identity configuration without secret values. `A3_Production_Baseline.json` and `A3_Protected_Files.json` preserve hashes. No A3 browser success, walkthrough, screenshot or regression rerun is claimed.

That initial A3 checkpoint is superseded. Current artifacts:

| Artifact | Actual outcome |
|---|---|
| `A3_Certificate_Trust.log`, `A3_Certificate_Verification.log` | Standard command succeeded after normal Windows confirmation; exact approved thumbprint trusted |
| `A3_HTTPS_Connectivity.json`, `A3_HTTPS_SignIn.jpg` | Normal TLS 200, existing HTTP redirect 307; browser sign-in page without certificate warning |
| `A3_Identity_Provision.log`, `A3_Identity_Verification.log` | Two active seed identities provisioned; password/actor/role checks passed without secret output |
| `A3_Identity_Repeat_Setup.log`, `A3_Identity_Repeat_Verification.log`, `A3_Identity_Final_Verification.log` | Same-roster repeat setup and final read-only verification passed |
| `A3_Development_Guard.json`/`.log` | Production invocation rejected with exit 1 before credential/database access |
| `A3_Identity_Tool_Final_Build.log`, `A3_Patched_Build.log` | Final tool and patched solution builds succeeded without warnings/errors |
| `A3_Patched_Integrated.trx`/`.log` | Superseded: 266 passed, 12 failed due to developer User Secrets mixing with fixture accounts |
| `A3_Final_Integrated.trx`/`.log` | After test-host isolation: all 278 passed, 0 failures/skips, including 17 live SQL tests |
| `A3_Screenshot_Index.json` | Actual 18 screenshot filenames, byte sizes and hashes; no nonexistent denial screenshot listed |
| `A3_Browser_SQL_Record_Verification.txt` | Completed order/batch; both steps Complete with original data; Closed deviation; correct Quality step-2 signature; 17 batch/4 order audits; same two users and original schema/migration |
| `A3_Sanitization_Check.json`, `A3_Final_Preservation.json`, `A3_Final_Working_Tree.txt` | Credential-value scan, expected source change/preservation, and unstaged Git inventory |
| `A3_Initial_Checkpoint_Historical.md` | Superseded waiting-state record, preserved without presenting it as current status |

The browser demonstration records are retained. The final A3 SQL regression ran before those records were created; older empty-history postflight outputs are not a claim that the database remains empty now. No destructive cleanup, schema change, Git integration or Batch B work occurred. Local development passwords/hashes are outside the repository in User Secrets; none are included in this evidence.
