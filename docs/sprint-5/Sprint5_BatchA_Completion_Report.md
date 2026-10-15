# Sprint 5 Batch A completion report

Date: 2026-10-14 (America/New_York). Current status after authorized Batch A4: permanent isolated SQL verification is implemented; integrated regression passes 278/278 and independent SQL repeat passes 17/17, with zero failures/skips in either run. All development manufacturing data and retained A3 evidence are unchanged. Build, advisory audit and architecture checks pass. Ready for Sam engineering review and subsequent Team acceptance review. Acceptance and Git integration have not occurred. Earlier outcomes below remain historical evidence.

## A4 permanent SQL isolation and regression closure

All 17 SQL tests share the new `SqlServerTestDatabase` collection fixture, restricted to `localhost` / `SyncBridgeCore_IntegrationTests`. Only `SYNCBRIDGE_TEST_SQL_CONNECTION` selects the connection; no fallback to application configuration, environment connection strings or User Secrets exists. The approval flag remains required. The ten recovery tests now use the same opt-in attribute as the seven service/Batch B tests. All 261 non-SQL tests and manufacturing assertions, including empty-history assertions, remain intact.

The fixture validates exact server/catalog and Windows authentication before connecting, constructs a canonical test connection, and holds a session-owned SQL application lock against overlapping runs. It creates only the constant disposable catalog, marks ownership with database extended property `SyncBridgeTestOwner=SyncBridge.Core.Tests.Disposable.v1`, applies unchanged migration `20261013010705_InitialCreate` through EF Core and calls the existing Development seeder. It verifies 15 manufacturing tables and 11 seed rows. No manufacturing table/entity or migration was added.

Before each drop/reset, actual `DB_NAME()` and ownership must match; an unmarked existing database is refused. Existing transaction rollback and exact test-owned order cleanup remain unchanged. Both runs finished with zero manufacturing-history rows and two seeded orders before dropping the owned catalog. A read-only master check confirms it no longer exists. There is no forced disconnect or broad cleanup. Interrupted-run recovery requires the same identity/ownership checks and exclusive lock. Reproduction commands, permission requirements and safeguards are in `Isolated_SQL_Test_Setup.md`.

| A4 check | Actual result / artifact |
|---|---|
| Solution build | 0 warnings/errors; `A4_Build.log` |
| Integrated regression | 278 passed, 0 failed, 0 skipped (261 non-SQL + 17 SQL); TRX wall time 72.189 seconds; `A4_Integrated.trx` / `.log` |
| Independent SQL repeat | Fresh database/migration/seed; 17 passed, 0 failed, 0 skipped; TRX wall time 28.502 seconds; `A4_SQL_Repeat.trx` / `.log` |
| Missing explicit configuration | Expected single-test rejection before SQL access despite inherited development connection; deliberate 1-failure negative check; `A4_Guard_Missing.trx` / `.log` |
| Explicit development catalog | Expected single-test rejection before SQL access; deliberate 1-failure negative check; `A4_Guard_Development.trx` / `.log` |
| No approval flag | All 17 SQL tests skipped without provisioning; `A4_OptOut.trx` / `.log` |
| Preservation | All 15 development table row hashes/counts and migration history match exactly; `A4_Development_Before.json`, `A4_Development_After.json`, `A4_Preservation_Checks.json` |
| Security/architecture/Git | No vulnerable packages reported; Pkcs 9.0.21 remains resolved; prior manufacturing-source hashes unchanged; no PageModel persistence references or migration changes; no staged files, branch/HEAD unchanged; `A4_Advisories.log`, `A4_Architecture_Git_Checks.json`; `git diff --check` passes |

Environment: Windows, .NET SDK 10.0.401, .NET/ASP.NET Core 10.0.12, existing SQL Server product version 17.0.1145.4, Windows authentication and the accepted encrypted local test connection. The inherited development connection remained present during both successful runs. Actual separate counts/durations are in `A4_Verification_Summary.json`; deliberate negative checks are not regression failures.

Read-only checks confirm Completed `A3-B-20261014` and `A3-PO-20261014`, two steps, one material verification, one equipment verification, one deviation, one signature, 17 batch audits and four order audits. Matching primary-key-ordered full-table row hashes preserve exact values/identities and fictional reference data, beyond these counts. No protected legacy database was accessed. A4 changes only test infrastructure: the new fixture, collection registration/opt-in on recovery tests, and the two SQL connection-selection paths. No A4 production, authentication, HTTPS, Data Protection, schema or package-reference change occurred. Earlier failed logs remain preserved.

Limitations: runtime negative checks cover missing configuration and explicit development-catalog rejection. Ownership refusal, interrupted-run recovery and concurrent-process lock rejection were reviewed in source but not fault-injected. No production performance, deployment or new browser-acceptance claim is made. Required create/drop permissions were available.

## Bounded security patch verification rerun (historical; resolved by A4)

The one-line Infrastructure override was already applied under the earlier explicit Team approval. Dependency inspection confirms Microsoft.EntityFrameworkCore.SqlServer 10.0.12 introduces Microsoft.Data.SqlClient 6.1.6, whose package dependency requests Pkcs 9.0.11; the direct override resolves System.Security.Cryptography.Pkcs 9.0.21. No unrelated package references were upgraded. Fresh solution restore succeeded without NU1902/NU1903; the transitive vulnerability audit reported no vulnerable packages in all five solution projects against the configured sources. Full solution build succeeded with zero warnings/errors.

The complete regression suite at that checkpoint, with live SQL enabled, finished with 277 passed, 1 failed and 0 skipped (48 seconds). All 261 non-SQL tests and 16 of 17 live SQL tests passed. The sole failure was `DevelopmentSeed_IsRepeatableAndCreatesNoManufacturingHistory`, at line 47: its global `Assert.Empty(Batches)` found retained Completed `A3-B-20261014`. This demonstrated fixture-state interference. Execution stopped for Team review without deleting history or weakening the assertion. A4 subsequently authorized and verified the isolated regression closure recorded above.

Architecture checks confirm the prior 27 production-source hashes are unchanged, no persistence references in PageModels, no migration changes, no staged files, and unchanged branch/HEAD and protected Sprint 4 evidence hashes. The Infrastructure diff remains exactly the approved package-reference line. Evidence: `Security_Patch_Dependency_Chain.log`, `Security_Patch_Restore.log`, `Security_Patch_Advisories.log`, `Security_Patch_Build.log`, `Security_Patch_Integrated.trx` / `.log`, and `Security_Patch_Architecture_Checks.json` under `artifacts/test-evidence/sprint-5/batch-a/`. Earlier results below retain their original execution context.

## Implemented workflow

Native Razor Pages now provide sign-in presentation, a role-aware production dashboard, production-order creation/view/status management, order-to-batch initiation, required material/equipment verification, ordered recipe execution, recorded data and step signature selection, deviation entry and Quality disposition, explicit Operator resume, Supervisor batch completion, Review by Exception and permitted order/batch audits. Completed batches display read-only context, steps, verification evidence, signatures and deviations.

The accepted cookie login/logout POST boundary remains intact. Page handlers call secured Application services; no page accesses EF, SQL or repositories. Controls reflect current trusted roles and record state; services and Domain independently reject forbidden and invalid direct requests. Administrator alone receives an account/configuration explanation without manufacturing operations. Additive roles remain additive; no self-approval restriction was introduced.

The only Application/persistence extension is dashboard listing through the existing repositories/services and a minimal batch summary DTO. Stored identifiers use the existing DBDS validator and technical error classification. Native validation, antiforgery, existing Bootstrap, success redirects, sanitized errors and no-store responses are reused. No production credentials or password hashes were added.

## Scope and traceability

FR-001–FR-010 and UC-001–UC-004 map to the pages and services in `Sprint5_BatchA_Requirements_and_Evidence_Matrix.md`. The explicit handler mapping, roles and test names are recorded there. Bounded choices are recorded in `Sprint5_BatchA_Implementation_Decision_Register.md`.

Inherited D1–D11, OQ-1/2/3/3A/4, five Batch states, order-authoritative quantity, BatchStepExecution-only signatures, immutable signatures, append-only audit, original schema/migration and composition remain intact. Prototype grouping informed the implementation; decorative/prototype-only controls were not treated as requirements. SRS/SDD/DBDS/plan were not edited. The supplied Sprint 4-to-5 handoff was read and preserved.

## Executed evidence

Environment: Windows, PowerShell, .NET SDK 10.0.401, .NET/ASP.NET Core runtime 10.0.12; native ASP.NET Core WebApplicationFactory HTTP testing with real secured identity/services/repositories and disposable SQLite; live SQL Server at localhost, SyncBridgeCore, Windows authentication. SQL encryption/trust settings match the accepted local regression configuration. No deployment account configuration was changed.

| Check | Actual outcome / artifact |
|---|---|
| Starting non-SQL baseline | 235 passed, 0 failed, 0 skipped; `Baseline_NonSql.trx` |
| First build after workflow pages | Succeeded, 0 warnings/errors; tool output |
| Focused HTTP/authentication | Initial 25 passed / 2 failed because role tests supplied an invalid deviation action; corrected and retested: 27 passed, 0 failed/skipped; `Focused_Http.trx`, `Focused_Http_Retest.trx` |
| Full non-SQL intermediate | 259 passed, 0 failed/skipped; `Full_NonSql.trx` |
| Added corruption regression | Intermediate 260 passed / 1 failed; stored batch identifier was misclassified as caller input. Corrected using the existing persistence validator; `Final_NonSql.trx` records this superseded failure |
| Live SQL intermediate | 17 passed, 0 failed/skipped; `LiveSql_Regression.trx` |
| SQL pre/postflight | Elevated execution succeeded; original migration, 15 tables, 23 restricted FKs, 11 seed rows and no manufacturing-history residues. Initial sandbox connection attempt failed with access/timeout. `Sql_Preflight.txt`, `Sql_Preflight_Elevated.txt`, `Sql_Postflight.txt` |
| Final build | Verified elevated build with disabled build servers succeeded, 0 warnings/errors; `Final_Build_Verified.log`. Prior sandbox build exited unsuccessfully with 0 compiler diagnostics; `Final_Build.log` |
| Final integrated suite | 278 passed, 0 failed, 0 skipped (261 non-SQL plus 17 live SQL), duration 1 minute 32 seconds; `Final_Integrated.trx`, `Final_Integrated_Test.log`. Includes both corrected identifier-corruption cases and final navigation/cache changes |
| Browser-host experiment | Failed before the browser walkthrough: HTTPS certificate `UntrustedRoot`; 0 passed / 1 environment failure in `Browser_Host_Workflow.trx`. Initial browser navigation reported connection refused. No successful interactive walkthrough or screenshots are claimed; optional harness removed; HTTPS validation unchanged |
| Architecture/security/Git | No persistence references in Pages, no migration changes, zero staged files, credential-pattern scan found no literals in 27 production files. `Architecture_Git_Checks.json`, `Source_Security_Check.json`; `git diff --check` passed |

HTTP coverage exercises UC-001–UC-004 end to end, missing/failed prerequisites, skipped steps, missing signatures and wrong targets, deviation approval/rejection/resolution, closure without automatic resume, additive self-review, forbidden direct POSTs for each relevant role, forged actor/role fields, completed mutations, invalid model binding, missing antiforgery, active-account revocation, safe technical-failure feedback and rollback. Invalid credentials and cookie security/rate limits remain covered by existing authentication integration tests. These are actual HTTP/rendering tests, not interactive browser acceptance or SQL substitutes.

## Production changes and exact artifacts

Original Batch A production change size: 15 added files and 12 modified files; approximately 495 added physical lines, 27 removed, net +468. `Production_Inventory.json` and `Production_Size.json` preserve that original calculation. A3 adds one authorized package-reference line in Infrastructure, making 13 modified production files and net approximately +469 lines in total; manufacturing production source otherwise retains its A3 starting hashes. The new Development-only utility/manifest under `tools/SyncBridge.DevelopmentIdentity/` is separate support tooling. Tests also include the A3 `SecuredServiceFixture.ConfigureTestHost` isolation correction so local developer User Secrets cannot mix with fixture accounts. See A3 build/provision/regression logs and `Development_Identity_Setup.md`.

Reports under `docs/sprint-5/`:

- `Sprint5_BatchA_Completion_Report.md` (this report).
- `Sprint5_BatchA_Requirements_and_Evidence_Matrix.md`.
- `Sprint5_BatchA_Implementation_Decision_Register.md`.
- `Sprint5_BatchA_Starting_State_Report.md` (historical initial checkpoint; missing-handoff issue subsequently resolved).

Supporting evidence under `artifacts/test-evidence/sprint-5/batch-a/`: test TRX files/logs, build logs, SQL pre/postflight, source hashes, production inventory/size, environment information, architecture/security checks and final verification summary. Failed attempts are retained and explicitly distinguished from passing final checks. The original raw Sprint 4 SQL transcript is excluded from new artifacts and was not read, changed or staged.

## Outstanding work, Git and readiness

The A3 browser core walkthrough has now executed successfully on the approved HTTPS host with the retained two accounts. This is engineering evidence for Team review, not a claim that the Team has accepted the sprint. No performance/capacity target, accessibility certification, published-host deployment acceptance or SSRS/EBR success is claimed. FR-011/BR-007 and FR-012 remain approved later-sprint work; enterprise integrations and broad administration remain excluded.

Batch A2 diagnosis (2026-10-14): the existing HTTPS launch profile starts successfully on localhost:7099 (IPv4/IPv6 loopback). The real user's localhost development certificate is valid but untrusted; ordinary HTTPS requests fail with UntrustedRoot and in-app browser navigation fails with ERR_CERT_AUTHORITY_INVALID. The diagnostic host was stopped. The standard proposed resolution is `dotnet dev-certs https --trust` for the existing certificate, pending the Team authorization explicitly required by Batch A2. No trust change or interactive walkthrough has occurred. See `Sprint5_BatchA2_HTTPS_Diagnosis_Report.md` and the A2 evidence files.

Batch A3 checkpoint (2026-10-14): the Team authorized the standard trust command for the exact existing certificate. Its identity was verified and the command started in the normal Windows user context; it is waiting for the normal Windows confirmation. Trust verification has not yet executed. Approved fictional-account configuration is absent from the project's user secrets and environment, and its location was requested without printing or inventing credentials. No A3 host/browser walkthrough or screenshots are claimed. See `Sprint5_BatchA3_Execution_Report.md` for the actual pending status and A3 evidence. No production code changed or regression rerun occurred.

That initial A3 checkpoint is superseded by the following completed execution. The user completed normal Windows confirmation, and exact-certificate trust verification passed. Normal HTTPS returned 200; HTTP redirected with 307. The Team approved permanent Development provisioning, explicitly retained demo.operator (Operator + Supervisor) and demo.quality (Quality), and authorized the bounded Pkcs 9.0.21 Infrastructure override after fresh restore surfaced an inherited advisory. User Secrets provisioning and repeated verification passed with no AppUser writes. A test-only account-binding issue exposed by real User Secrets caused 12 failures in the first post-provisioning run; corrected fixture isolation then passed all 278 tests, including 17 live SQL, with zero skips. Patched application and final tool builds passed without warnings/errors.

Actual Codex In-app Browser forms executed order creation/release, invalid unreleased-order initiation, valid batch initiation, missing-prerequisite rejection, required verifications, ordered steps, step-associated deviation, Quality review/approval/resolution, closed-deviation Exception awaiting explicit Operator resume, missing-signature rejection, Quality step signature, Supervisor completion, audits, completed read-only review and a rejected stale late-mutation POST. SQL verified both Completed steps with unchanged data, one Closed deviation, the correct step-2 signature target, 17 batch audits, 4 order audits, unchanged two AppUsers, 15 tables and the original migration. Eighteen sanitized screenshots are indexed in `A3_Screenshot_Index.json`. Actual walkthrough/results/limits are detailed in `Sprint5_BatchA3_Execution_Report.md`.

Coverage limits: the two-account configuration deliberately has no Administrator-only or standalone Operator/Supervisor browser sessions. A crafted skipped-step POST was covered by passing HTTP tests rather than browser DOM mutation. In-app direct Quality order-creation navigation reported ERR_BLOCKED_BY_CLIENT, so no rendered denial or browser-observed application status is claimed for that route; role navigation and final HTTP forbidden-request tests remain evidence. Browser demonstration records are retained; no destructive reset occurred, and the A3 SQL regression preceded their creation. The development host was stopped after logout.

Branch remains `feature/wp6-sprint5-razor-pages`, HEAD `65d324bd2e0f0a82515dd20eca89efe5984baf96`, upstream `origin/feature/wp6-sprint5-razor-pages`. Tracked changes and new files are unstaged. The two protected preexisting Sprint 4 files retain their initial SHA256 hashes. The Team-supplied untracked handoff is preserved. No commit, merge, push, reset, branch deletion or Sprint 6 branch was created.

Recommendation: ready for Sam implementation/evidence review and Team acceptance review of the completed two-account browser walkthrough, with the stated coverage limits visible. No HTTPS or identity-provisioning blocker remains in that approved scope. Git closure still requires explicit Team acceptance and Git authorization.
