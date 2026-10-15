# Sprint 5 final engineering verification — Batch B

Date: 2026-10-14, America/New_York. Batch A was explicitly accepted by the Team after Sam's review, as recorded in the Batch B authorization. Final Sprint 5 acceptance and Git integration remain pending. Recommendation: ready for Sam's final evidence review and Team acceptance/integration authorization.

## Independently executed verification

| Check | Batch B actual result |
|---|---|
| Starting branch / HEAD | `feature/wp6-sprint5-razor-pages` / `65d324bd2e0f0a82515dd20eca89efe5984baf96` |
| Fresh solution restore | Passed; no NU1902/NU1903 or other restore warnings reported |
| Solution build | Passed, 0 warnings, 0 errors |
| Development utility build (outside solution) | Passed, 0 warnings, 0 errors |
| Full integrated regression | 278 passed, 0 failed, 0 skipped; 261 non-SQL plus 17 SQL; TRX wall duration 72.316 seconds |
| Independent SQL repeat | 17 passed, 0 failed, 0 skipped; TRX wall duration 28.792 seconds |
| Vulnerability audit | No vulnerable packages reported in any of the five solution projects, including transitive dependencies |
| Pkcs resolution | 9.0.21; EF SQL Server 10.0.12 → SqlClient 6.1.6 → Pkcs, with the approved direct override |
| SQL disposal / preservation | Both runs report zero history rows and two seed orders before dropping the owned catalog; subsequent SELECT confirms it is absent. All development row hashes/counts and migration history match before/after |
| Architecture / Git | Accepted source hashes and golden-document hashes match; no Domain/migration changes or PageModel persistence references; diff whitespace check passes; zero staged files; no local/remote divergence |

Raw evidence is under `artifacts/test-evidence/sprint-5/batch-b/`, indexed in `Sprint5_Final_Evidence_Index.md`. `Test_Summary.json` derives counts/timings from actual TRX; `Test_Catalog.json` retains test identities/outcomes. These results independently supersede use of historical A4 counts for final readiness. Environment: Windows, .NET SDK 10.0.401, .NET/ASP.NET Core 10.0.12, local SQL Server 17.0.1145.4 and Windows authentication.

## Security and manufacturing review

`ManufacturingIdentity` validates one trusted cookie identity, an exact active AppUser association and current server-configured roles. Caller role claims and form actor/role fields do not grant authority. `ApplicationOperation` checks roles before work and again inside mutation transactions. Roles remain additive, including approved own-deviation review when both roles are present. No new separation-of-duties restriction or Administrator manufacturing privilege was added.

The accepted secure/HttpOnly/Strict host cookie, fixed expiry, principal revalidation, antiforgery, HTTPS redirection and login rate limiter remain. Authentication presentation is a native Razor Page; the accepted login/logout POST boundaries remain. `AuthenticationBoundaryTests` and the passing direct-request/revocation/forgery cases corroborate source review. Development credentials remain outside Git in User Secrets; the support utility is Development-only and verifies existing active users without creating database users.

Reviewed PageModels delegate manufacturing reads/commands to secured `ProductionOrderService` or `BatchService`. Shared feedback/navigation resolves trusted actor context; it contains no EF/SQL/repository access. Page visibility does not replace service authorization. The pending Application/Infrastructure additions are bounded dashboard listing, summary DTO and stored-identifier validation, plus the explicitly approved Pkcs override. No Batch B production/test/tooling source correction was needed.

Domain and persistence retain Created, Ready, InProgress, Exception and Completed; review/approval remain activities. Order quantity remains authoritative. Invalid transitions, missing/failed material/equipment prerequisites, skipped steps, wrong/missing signature associations, blocking deviations, closure without auto-resume, terminal completed protection and trusted audit attribution are covered by current passing HTTP/service/Domain/SQL tests. `ApplicationUnitOfWork` preserves immutable signatures, append-only audits and completed batch/evidence guards. The unchanged migration and all 15 manufacturing tables retain accepted DBDS amendments. No manufacturing REST controller, SSRS/EBR implementation or enterprise subsystem was introduced.

## SQL and browser evidence boundaries

The fixture selects only `SYNCBRIDGE_TEST_SQL_CONNECTION`, exact `localhost` / `SyncBridgeCore_IntegrationTests`, with `SYNCBRIDGE_APPROVED_LIVE_SQL=1`. Canonical connection construction, actual catalog/ownership checks, application lock, existing EF migration/seed and guarded disposal remain as accepted in A4. The inherited development connection was present during both successful B runs but was not selected. The protected legacy database was not accessed.

Read-only before/after checks cover all 15 development manufacturing tables, not only summary counts. Completed `A3-B-20261014` / `A3-PO-20261014` retain two steps, material/equipment verification, one deviation, one signature and 17 batch plus four order audits. Exact values/identities and reference data are preserved by matching ordered-row hashes.

A3's actual HTTPS forms and 18 unchanged screenshots remain accepted interactive evidence; B did not manufacture a new browser run. The retained accounts are Operator+Supervisor and Quality. Standalone Operator/Supervisor and Administrator browser sessions, crafted skipped-step browser POST and browser Quality rejection were not executed. Quality direct order-create navigation was blocked by the browser client, so no application status is inferred. Automated rejection evidence remains distinct. See the accepted A3 report for the full walkthrough and limits.

No unresolved implementation defect was demonstrated. One Git evidence-preparation correction trims console padding in five derivative copies while preserving original historical logs; provenance and exact exclusions are in the readiness report. The new dependency-chain log is whitespace-sanitized. No source or result text changed. Limits remain: no formal penetration, accessibility, load/capacity or deployed-host certification; no new fault injection of ownership refusal/interrupted-run/concurrent-run safeguards. A4's deliberate guard failures and opt-out skips are separate historical negative evidence, not ordinary passing regressions. FR-011/BR-007/UC-005 and FR-012 remain later-sprint work. Golden documents and protected Sprint 4 files are preserved.
