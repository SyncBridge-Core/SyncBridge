# Sprint 6 Batch B integrated verification report

Date: 2026-10-15, America/New_York. Baseline and current HEAD: `e2cbbb56e786f2ba2dbc57750b1c06f370110452`, branch `codex/wp6-sprint6-rest-api`, upstream same-named origin branch. Batch B execution is Team authorized; Sprint acceptance is PENDING and Git integration is NOT AUTHORIZED / NOT EXECUTED.

Objective: verify behavioral and persisted-evidence equivalence for four approved capabilities through the five accepted routes, including actual REST HTTP execution through SQL Server. There is no material-verification REST endpoint or new manufacturing capability. Corrected SRS/SDD, amended DBDS, realigned plan and Sprint 5 handoff hashes match Batch A.

## Preflight

Five routes, controller, Program configuration, Application methods, matching Page handlers, trusted identity, antiforgery and native exception handling were inspected. The isolated SQL fixture retains exact localhost/SyncBridgeCore_IntegrationTests, Windows authentication, explicit opt-in, exclusive application lock, ownership marker and controlled creation/disposal. The read-only preservation baseline preceded integration activity. No protected legacy catalog was opened.

All five Batch A reports were present under the user-relocated `docs/sprint-6/New folder-1/` rather than their original paths. They were read in place and preserved; only the existing decision register receives a labeled B addendum. Batch A logs remain unchanged. Preflight inventoried all tracked, untracked and ignored paths; ten inherited protected originals remain excluded and hash-preserved.

## Executable equivalence

`EquivalenceScenario.Run` drives real HTTPS-capable WebApplicationFactory clients with native credential login, cookies and antiforgery. One path posts native Razor forms; the other posts JSON to accepted routes. Each gets a separate fictional order/batch, the same recipe, roles and prerequisites. Setup/quality/resume/final batch completion use approved Application commands; signature creation uses the existing Quality Sign Page. No manufacturing state is fabricated by SQL updates.

- **Order context:** JSON equals the existing Application view and all displayed identifiers/product/recipe/quantity/status match the Page. Record/audit counts stay unchanged. Additional paired boundary tests cover missing/invalid order, anonymous/API 401 vs browser redirect, Administrator 403 and additive read roles.
- **Initiation:** unreleased orders reject without batch/audit additions; released orders produce Created, one trusted Operator creation audit, API 201 vs UI 302. Duplicate identifiers reject with identical snapshots. SQL Operator/Supervisor additive membership works.
- **Steps:** Created/Ready cannot bypass execution gates. Individually missing/failed material/equipment prevent processing and step bypass; valid prerequisites permit InProgress. Step 2 before 1 rejects. Successful start/completion preserve recorded data and trusted completing user. Unsigned controlled completion rejects; existing Quality signature is associated only with BatchStepExecution and completion adds no second signature. Terminal Completed rejects all exposed mutation types.
- **Deviation:** random/missing activity and the other batch's actual execution reject. Valid origin is preserved with trusted reporter, Open deviation and Exception. Quality review/approval/resolution does not auto-resume; explicit Operator resume is required. Separate paired test verifies a batch-level deviation retains null activity association.
- **Rollback:** inject the existing failure after a real INSERT while creating a deviation through each adapter. Both return controlled 503; fresh view and complete audit snapshots equal their before values. Successful retry creates only one intended deviation. Existing SQL constraints/transaction regression validates unique/FK and unit-of-work behavior.
- **Normalized evidence:** compare final status/quantity/recipe, ordered step status/data/signature requirements, deviation description/status/review/disposition, signature actor/meaning and audit action/actor/result sequence. Sort by time and then action for simultaneous events; do not compare generated IDs or absolute times. Separately assert unique audit IDs, exact persisted audit counts, one signature, correct batch/step/signature foreign associations, timestamps, completing/signing/reporting actors and BatchCreated/BatchCompleted timeline endpoints.

The same scenario executes in SQLite Development and Production plus actual SQL Server Production. The SQL case asserts the HTTP host's provider and exact guarded catalog, reconciles rows through fresh SQL scopes and invokes the existing exact test-owned cleanup. Cleanup leaves two seeded orders and zero batch, step, deviation, signature or audit history; collection disposal also validates all history and drops only its owned database. SQL tests remain serialized in the existing collection.

## Security and HTTP

New paired boundary cases verify both adapters deny Supervisor/Quality/Administrator mutations despite forged body identities, query role/actor parameters and identity headers; snapshots remain unchanged. Missing/invalid/cross-user antiforgery tokens fail before mutation. Inactive AppUser invalidates existing cookies, blocks API reads/mutations with 401 without redirects and preserves browser redirects. Additive Operator/Quality reads and batch-level deviations remain permitted.

All Batch A REST tests execute again, including 200/201/400/401/403/404/422/503 mappings, structured validation, generic unhandled 500 ProblemDetails in Development and Production, signature gates, rollback and safe error bodies. Existing Razor/authentication regressions verify browser rendering, cookies, antiforgery and role behavior. No JWT/API-key or additional authentication endpoint exists.

## Execution and correction

The initial focused run passed 9/9. The initial full integrated run passed 302/302 with 18 live SQL cases, and its independent SQL repeat passed 18/18. The long integrated run was inspected read-only; it completed normally before a stop request took effect, returned exit 0 and produced complete TRX. No failed/aborted count is inferred from the interim lack of console progress.

Review of that passing output exposed two clocks in the test setup: fixture Application commands used its advancing clock, while HTTP manufacturing commands used system time. Corrected `SecuredServiceFixture.ConfigureTestHost` to register the existing fixture TimeProvider. Added timestamp containment and first/last audit assertions; normalization orders equal-time events by action because database ordering within a timestamp is unspecified. No production or manufacturing behavior was changed. Final reruns and exact counts are recorded in `Sprint6_Final_Engineering_Verification_Report.md`.

## Impact and bounds

Batch B production LOC: **0**. Tests: two new files, `UiApiEquivalenceTests.cs` (280 physical lines) and `RestBoundaryRegressionTests.cs` (144); shared fixture +2 lines; existing SQL cleanup visibility one line changed. Total test diff +427/-1, net +426. Eleven new discovered cases: ten non-SQL and one live SQL. No package/project/schema/migration/Domain/Application/Infrastructure changes.

Development ordered-row hashes/counts and A3 summary are compared before/after with the accepted read-only utility. Final preservation, source/architecture/security scans and exact Git inventory are in the final evidence set. The 15-table schema, original migration and AMD-001/002 remain authoritative. No destructive development testing or legacy access occurs.

Limitations: HTTPS TestServer exercises the actual middleware, services, repositories and SQL provider; it is not a new deployed external-network/browser performance qualification. SQL identities intentionally reuse the accepted two-account test configuration; broader role permutations run through SQLite and existing security tests. EBR/SSRS, FR-011/BR-007/UC-005 remain Sprint 7. No production-scale performance or formal security certification is claimed. Ready for Sam/Team review after final gates; Sprint acceptance and Git integration remain pending.
