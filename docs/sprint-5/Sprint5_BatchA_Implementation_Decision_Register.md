# Sprint 5 Batch A implementation decisions

Date: 2026-10-14. Authority: corrected SRS/SDD, amended DBDS, realigned plan §4.5, Sprint 4-to-5 handoff and Batch A prompt. Inherited D1–D11 and OQ decisions remain unchanged. These choices authorize no Git integration or golden-document changes.

| Choice | Implementation and reason |
|---|---|
| Dashboard selection reads | Add `GetNumbersAsync` to the two existing repository contracts and secured `ListAsync` methods to the two existing services. Order DTOs and a three-field batch summary provide existing records for selection, including completed records. Load through the accepted assemblers; classify corrupted stored identifiers as technical failures using the existing DBDS validator. No new repository, persistence model or query framework. |
| Cohesive batch page | Keep verification, ordered execution, step signatures, deviation entry/review and lifecycle forms together on `/Batches/Details`. Each named POST maps directly to one secured service operation. Display conditions describe available controls; Domain/Application remain authoritative for direct requests. |
| Shared presentation feedback | One small authorized PageModel base handles trusted role display, sanitized outcome feedback, success redirects and no-store responses. It contains no persistence calls or workflow eligibility engine. Current roles come from `IActorContext`, never cookie role claims or form fields. |
| Authentication presentation | Replace the original inline GET form with a native Razor Page at the same `/authentication` address. Retain existing login/logout POST endpoints, cookie identity, credential authenticator, antiforgery, rate limits and HTTPS policies. Invalid login remains HTTP 401 with explanatory text. |
| HTTP evidence | Reuse the existing `WebApplicationFactory`, trusted identity and SQLite repository fixture. Tests exercise rendered Razor forms, HTTP model binding, antiforgery, cookies, real services and actual persistence. Existing SQL regressions remain independent live evidence. |

No new manufacturing rules, signature targets, self-approval restriction, workflow states, migration, table, manufacturing REST controller, administration framework or reporting integration was introduced. Prototype-only pause, assignments, categories, attachment, notification, handwritten-signature, reporting and analytics controls are not approved capabilities for this batch.

An optional local Kestrel browser-host experiment failed on an untrusted HTTPS certificate. Its temporary test-host code was removed; certificate validation and system trust were not changed. Interactive browser acceptance requires the approved trusted HTTPS environment.

## A3 explicitly authorized follow-up

The statement above records the original A result. A3 later authorized trusting the exact existing certificate; the user completed Windows confirmation and normal HTTPS/browser access was verified. No TLS validation bypass or certificate replacement occurred.

| Choice / Team authority | Implemented result |
|---|---|
| Permanent Development provisioning approved in the user's reply | Small local console utility reuses ApplicationComposition, registered PasswordHasher, credential authenticator, trusted actor context and User Secrets. Active identity/username matching is checked before writes, including ordinal checks after database lookup. No database writes, web endpoints or identity module. Hidden prompts are normal; optional generated QA passwords remain only in local User Secrets. |
| Retain the two accepted identities, explicitly chosen after seed inspection | Public manifest uses demo.operator with Operator + Supervisor and demo.quality with Quality, matching the accepted live SQL fixture. No four-user seed assumption, new AppUser record or invented Administrator account. Browser evidence limits record the resulting role combinations. |
| Bounded dependency correction explicitly approved after advisory discovery | One-line Pkcs 9.0.21 override in Infrastructure; no manufacturing/security-policy change or warning suppression. Final patched build and regression pass. |
| Test-host isolation corrected as a routine verification defect | PostConfigure replaces fixture account lists rather than appending to actual developer User Secrets. Initial 12 failures are retained; final 278/278 passes with real secrets present. |
| Preserve actual browser manufacturing history | Keep completed A3 order/batch, evidence and audits produced through the approved UI. No destructive database cleanup; live SQL regression was executed before interactive data creation. |

## Bounded security patch re-verification and stop (historical; superseded by A4)

The Team's expanded approval was received after the previously authorized one-line Pkcs override and original passing regression/browser execution. The subsequent dependency check confirms EF Core SQL Server 10.0.12 → Microsoft.Data.SqlClient 6.1.6 → Pkcs (original requested version 9.0.11), with the explicit override resolving 9.0.21. Fresh restore, transitive advisory audit and full solution build pass with no vulnerability advisories or build warnings/errors. No suppression or unrelated upgrade was introduced.

The complete post-walkthrough regression is 277 passed / 1 failed / 0 skipped: the live SQL seed test requires globally empty manufacturing history and fails on retained `A3-B-20261014` at line 47. All other tests pass. The prior 278/278 result preceded browser record creation and is not presented as the current rerun. The Team instruction to stop on verification failure applies: no database clearing, test alteration, further browser execution or Git integration was performed. Resolution of the SQL fixture-state conflict requires Team review. Evidence is recorded in the completion report and `Security_Patch_*` artifacts.

## A4 Team-authorized permanent SQL isolation

Authority: `CodexPrompt_Sprint_5_Batch_A4.txt`, authorizing the disposable SQL environment and focused fixture correction while preserving development evidence.

| Decision | Implemented result |
|---|---|
| Explicit test-only connection | Only `SYNCBRIDGE_TEST_SQL_CONNECTION`, exact `localhost` / `SyncBridgeCore_IntegrationTests`, Windows authentication; canonical connection without inherited routing/configuration. Invalid identity fails before SQL access. |
| One serialized collection owner | New `SqlServerTestDatabase` fixture owns all 17 SQL tests; session-owned application lock rejects overlapping cooperating runs. |
| Accepted schema and seed | Existing EF migration and Development seeder only; verify 15 tables, 11 seed rows and exact migration history. Ownership extended property adds no manufacturing table/entity. |
| Guarded disposable lifecycle | Constant catalog identifiers; verify actual catalog and exact owner marker before every reset/drop; refuse unmarked databases. Existing transactional/test-owned cleanup remains. Zero history/two seed orders verified before disposal; no forced disconnect or broad cleanup. |
| Preserve test strength | Keep 261 non-SQL and 17 SQL tests; only fixture connection paths, collection registration and explicit recovery-test opt-in change. Empty-history and manufacturing assertions remain intact. |
| Preserve development evidence | Read-only all-table row hashes/counts and migration history match before/after both runs, including Completed A3 records, linked evidence, 21 audits and all reference data. |

Integrated regression: 278 passed / 0 failed / 0 skipped, 72.189 seconds. Independent SQL repeat: 17 passed / 0 failed / 0 skipped, 28.502 seconds. Both disposable databases were provisioned independently and dropped with no history residue. Missing explicit configuration and an explicit development catalog each produce the expected deliberate negative-check rejection before SQL access; absent opt-in skips all 17. Build passes without warnings/errors; advisory audit reports no vulnerable packages and Pkcs remains 9.0.21. Manufacturing source, migrations, architecture and Git branch/HEAD are preserved. No production/security-policy change or Git integration. Ownership/interrupted-run/concurrent-lock fault injection was not performed; safeguards were reviewed in source. See `Isolated_SQL_Test_Setup.md`, completion report and `A4_*` evidence. Recommended status: ready for Sam review, then Team acceptance review.
