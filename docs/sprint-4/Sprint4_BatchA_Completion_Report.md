# Sprint 4 Batch A secured Application services completion report

Date: 2026-10-13 (America/New_York)

**Result: Batch A implementation is complete and the available non-SQL verification passes. Live SQL verification remains unexecuted pending its separate explicit authorization. This is not Sprint 4 acceptance or permission for Git integration.**

The implementation provides secured production-order and batch services over the accepted Domain and persistence foundation. It covers authentication, approved role permissions, prerequisite verification, ordered execution, signatures, deviations, audits and Review by Exception. State/evidence mutations share one command transaction. Future manufacturing screens, REST controllers and reporting remain outside this delivery.

## Authority and initial repository verification

Executed prompts:

- `Z:\InstallationPackages\Transfer_Folder\Sprint_4\CodexPrompt_Sprint_4_Batch_A.txt`
- `Z:\InstallationPackages\Transfer_Folder\Sprint_4\CodexPrompt_Sprint_4_Batch_A2.txt`

The continuation prompt explicitly approves D1–D4 and authorizes consolidated A2–A4 implementation, routine corrections and internal implementation choices. It retains the separate live SQL approval requirement.

- Branch: `feature/wp6-sprint4-application-services`.
- HEAD: `c42fce5a345c5fe2bb8b58843302a170f0d841c1`, the accepted main baseline; it remains unchanged.
- Accepted Sprint 3 implementation commit: `a4bc59658b00ec464480eb6b346afd90dbaae5f6`, present in history.
- At the resumed A1 inspection, the working tree contained the user-uploaded, untracked Sprint 3 to Sprint 4 handoff only. Continuation also retained A1 report/TRX evidence.
- No AGENTS.md was found in the repository or inspected parent locations. No subagents were used.
- No commit, merge, push, migration reset, database recreation or Git integration was performed.

## Source coverage and traceability

| Authoritative source | Actual location | Implementation responsibility |
|---|---|---|
| Corrected SRS | `C:\Engineering\Golden_Documents\SyncBridge_Core_SRS_Corrected.docx` | §2.2 roles; FR-001–010; BR-001–006/008; NFR-001/003/005; §6.1 diagram; UC-001–004; §6.4 role data flows |
| Corrected SDD | `C:\Engineering\Golden_Documents\SyncBridge_Core_SDD_Corrected.docx` | §§3.2, 4.2–4.5, 5.1–5.4; inward dependencies, cohesive services, DTOs, Domain rules, technical identity/persistence |
| Amended DBDS | `C:\Engineering\Golden_Documents\Indy-1-SyncBridge-DBDS_v2.docx` | §5 identity/state/evidence fields; §7.2 transactions; §8.2 exception queries; AMD-001/002 |
| Realigned plan | `C:\Engineering\Golden_Documents\WP-6_Implementation_Execution_Plan_Realigned.docx` | §4.4 secured Application services, shared operations and service integration evidence |
| Accepted handoff | `docs/sprint-3/WP6_Sprint3_to_Sprint4_Engineering_Handoff.docx` | Existing contracts, restricted reconstruction, identity linkage, optional-field protection and later-sprint boundaries |
| Accepted source and reports | `src/`, `tests/`, `docs/sprint-3/` | Reused entities/operations, three repositories, scoped DbContext/UoW, migration and accepted decisions |

The SRS use-case diagram was visually inspected, not inferred from its caption. It establishes Supervisor order creation/batch completion, Operator manufacturing execution, Operator/Quality signatures and Quality deviation review. The A2 prompt resolves the remaining permission decisions. Golden documents and historical reports were not amended.

| Requirement | Implemented path and verification |
|---|---|
| FR-001 | Cookie authentication boundary, PasswordHasher, current configured roles, active AppUser resolution, authorization in every Application operation; authentication and denial tests |
| FR-002 | ProductionOrderService create/read/status/audit operations; approved recipe, decimal(18,3) input bounds, independent order audit; role and round-trip tests |
| FR-003 | BatchService initiation, readiness, start, interruption, resumption and completion; existing five-state Domain operations; lifecycle and protection tests |
| FR-004 | Ordered recipe instructions in DTOs, StartStep/CompleteStep, optional recorded process data; skipped-step/signature gates and fresh-scope reads |
| FR-005 | Recipe-required material verification, current result plus historical audit, actor/time; missing/failed/retry and optional-field tests |
| FR-006 | Recipe-required equipment verification, current result plus historical audit, actor/time; readiness gates and calibration/comment preservation |
| FR-007 | Operator/Quality approval of an in-progress BatchStepExecution in the same Batch; immutable signature plus audit; denied/foreign/wrong-target/failure tests |
| FR-008 | Append-only manufacturing and independent production-order audit evidence; read permissions; immutable-record and accountability tests |
| FR-009 | Originating Batch/optional step, review, approval/rejection, disposition/resolution through existing Domain; separate Operator resumption; rejected/unresolved gates |
| FR-010 | Supervisor/Quality queries identify unresolved deviations and missing/failed recipe prerequisites from normalized current data; no added tables/report engine |

FR-011 EBR generation and FR-012 manufacturing REST services are not implemented or claimed complete. BR-007 reporting verification remains deferred. ADR-001/003/004/006/007/008/009 are preserved in the implemented layers; later Presentation/API/SSRS increments remain deferred.

## Approved authorization matrix

Every listed permission requires a trusted authenticated identity, an exact mapping to an existing active AppUser and the named current server-configured role. Roles are additive; Administrator does not imply any manufacturing role.

| Operation | Operator | Supervisor | Quality Engineer | Administrator alone |
|---|---|---|---|---|
| Authenticate | Yes | Yes | Yes | Yes |
| Read manufacturing order/batch/recipe context | Yes | Yes | Yes | No |
| Create production order | No | Yes | No | No |
| Release/start/complete/cancel production order (D1) | No | Yes | No | No |
| Initiate/prepare/start Batch | Yes | No | No | No |
| Verify required materials/equipment | Yes | No | No | No |
| Start/complete ordered workflow steps | Yes | No | No | No |
| Sign an in-progress BatchStepExecution | Yes | No | Yes | No |
| Create deviation/interrupt execution | Yes | No | No | No |
| Review/approve/reject/resolve deviation (D2) | No | No | Yes | No |
| Resume after blocking deviations close (D3) | Yes | No | No | No |
| Complete Batch | No | Yes | No | No |
| Read audit history/Review by Exception (D4) | No | Yes | Yes | No |

No self-approval restriction was added. The multi-role test proves that the same legitimately assigned Operator/Quality actor can report, review and resolve a deviation and then resume through the Operator permission. Domain gates still control the required sequence.

## Authentication and trusted identity implementation

Infrastructure owns ManufacturingIdentity, implementing the existing IActorContext seam and credential authentication. Deployment configuration supplies stable UserId/UserName, ASP.NET Core PasswordHasher hashes and role memberships. The manufacturing schema holds only existing AppUser references; no password/role tables or columns were added.

Login validates a configured account and password hash, then checks the existing AppUser and activation state. It emits an encrypted ASP.NET Core cookie containing the validated identifier/name, without caller-selected roles. Each controlled operation resolves the current account/roles and rechecks AppUser. Supplied role claims cannot elevate access. Missing/duplicate identifiers, unknown accounts, inactive users and mismatched mappings fail closed. Revocation takes effect in an existing service scope.

The authentication boundary supplies only sign-in/sign-out endpoints and a minimal sign-in form. Cookies are Secure, HttpOnly and SameSite Strict, with a fixed 30-minute lifetime. ASP.NET Core antiforgery protects both POSTs. The built-in rate limiter permits five login attempts per remote address per minute and returns 429 after that. Windows Data Protection provides the ordinary cookie/key infrastructure. No home-grown cryptography, registration framework or enterprise IAM was introduced.

Implementation references: [Microsoft cookie authentication guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0) and [Microsoft antiforgery guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).

No deployment credential was provisioned, read or committed. Empty identity configuration denies manufacturing access. Fictional test credentials are confined to the test fixture's transient configuration and are not deployment credentials.

## Application service inventory and results

ProductionOrderService implements CreateAsync, GetAsync, ChangeStatusAsync and GetAuditAsync. Status changes invoke Release, StartProcessing, Complete or Cancel on the existing ProductionOrder; it adds no editing, scheduling or quantity allocation workflow.

BatchService implements InitiateAsync, GetAsync, MarkReadyAsync, StartProcessingAsync, VerifyMaterialAsync, VerifyEquipmentAsync, StartStepAsync, SignStepAsync, CompleteStepAsync, CreateDeviationAsync, ReviewDeviationAsync, ResumeAsync, CompleteAsync, GetAuditAsync and ReviewExceptionsAsync.

DTOs expose ordered recipe steps/instructions and requirements, execution results and recorded process data, verification actors/times, deviations, signatures and permitted audit/query results. No service returns mutable Domain entities. Requests do not take actor IDs. Timestamps and generated technical IDs come from the trusted service boundary; technical strings remain canonical lowercase GUID D values.

ApplicationOperation is one small internal execution helper, not a command framework. It resolves/authorizes the actor, starts a command transaction for mutations, rechecks identity within the transaction, executes existing Domain behavior and commits once. Results distinguish success, invalid input, business-rule rejection, unauthenticated, forbidden, not found and technical failure. Expected failures return bounded messages; unexpected exceptions are logged through ILogger and do not expose implementation details. Requested cancellation propagates after cleanup.

## Repository, persistence and transaction changes

- Existing ProductionOrderRepository adds status-only updates, standalone audit append and order-audit retrieval.
- Existing BatchRepository gains explicit updates, verification/signature/recorded-data persistence, reconstructed evidence and review candidates. BatchCommandPersistence is a partial implementation of that same repository, not a fourth repository or generic data-access subsystem.
- Existing assembler validation/rehydration methods now return the verified historical evidence instead of discarding it, so services can reuse those objects without replaying manufacturing commands.
- AuditPersistence shares the approved row mapping and append/deduplication path across order creation, Batch creation and updates. The diff review removed the duplicated Batch creation audit mapping.
- ApplicationUnitOfWork starts a serializable command transaction, retains the existing single SaveChanges boundary, commits/disposes it and clears tracking. Failure/cancellation rolls back and discards staged changes. Begin rejects a dirty/nested persistence scope and clears unchanged stale tracking.
- Existing audit/signature mutation guards remain. Completed-record protection now covers changed child execution/verification/deviation/signature rows as well as Batch rows.
- Updates preserve unrelated lot numbers, comments, calibration evidence, recorded execution values and final disposition. A supplied recorded-data value is written only by the approved step-completion operation; omitted data is preserved.

Schema mappings, the model snapshot, migration `20261013010705_InitialCreate`, seed implementation and 15-table inventory were not changed. No database was accessed, recreated or reset.

Accepted decisions remain intact: order quantity is authoritative; DeviationCode is separate from its technical ID; technical identifiers are canonical; signatures target only BatchStepExecution; audit IDs retain the approved 256-character Unicode boundary; the original restricted rehydration path remains in use.

### Bounded Domain correction

The accepted rehydration constructor rejected an Exception Batch when its last deviation had become Closed. Existing Resolve and Resume operations allow that condition between two separately authorized commands. Persisting Quality resolution therefore made the next Operator resumption unreadable.

The constructor now requires an originating deviation for Exception without requiring that at least one remain blocking. Completed still rejects blocking deviations; normal execution still cannot advance in Exception until ResumeProcessing succeeds. No state, transition, correction/reopening workflow or alternative rehydration mechanism was introduced. The end-to-end test loads this intermediate condition in a fresh scope before resuming it.

Two audit action enum values were appended for required production-order creation/status evidence. Existing action values retain their ordinals; their approved string converter requires no schema change.

## Executed verification and evidence

| Verification | Actual outcome |
|---|---|
| Initial non-SQL Sprint 3 regression | 191 passed, 0 failed, 0 skipped |
| Initial focused service tests | 35 passed, 0 failed, 0 skipped |
| Expanded focused run | 41 passed, 3 failed because the token helper expected an exact Cache-Control string rather than testing NoStore |
| Authentication retest after correcting the assertion | 3 passed, 0 failed, 0 skipped |
| Final available regression | 235 passed, 0 failed, 0 skipped: all 191 accepted non-SQL cases plus 44 new non-SQL cases |
| Final solution build | Exit 0; 0 warnings; 0 errors |
| Local publish | Exit 0; runtime composition/dependency delivery succeeded |
| Published authentication page over localhost HTTPS | HTTP 200; sign-in form present; no SQL access |
| Diff whitespace and schema/migration change inspection | `git diff --check` passed; no mapping/migration changes |
| Existing 10 live SQL tests and 2 new SQL service tests | Not executed; separate approval remains outstanding |

Final regression command:

```powershell
dotnet test src/SyncBridge.Core.slnx --no-build --no-restore --filter 'Category!=LiveSqlServer' --logger 'trx;LogFileName=Sprint4_BatchA_Final_Regression.trx' --results-directory artifacts/test-evidence/sprint-4/batch-a
```

The total discoverable inventory is 247 cases (201 accepted plus 46 new), of which 235 are available without live SQL permission. The historical accepted 201/201 and 10/10 SQL results are not claimed as newly rerun results.

Relational service tests use the actual repositories, identity implementation and unit of work with an isolated in-memory SQLite database. A test-only model customizer removes SQL-specific type/schema annotations and maps DateTimeOffset for portable querying. This does not validate SQL Server storage/collation/provider behavior or change production mappings.

An actual executed INSERT is interrupted by a command interceptor to test rollback, not merely a mocked repository failure. Tests verify that neither order/audit nor signature/audit is partially persisted, a changed current verification retains its old result after failure, and reuse of the service scope cannot commit failed-command tracking. Existing normalized state is reloaded across fresh scopes throughout manufacturing execution.

Cookie/antiforgery tests execute the actual Web host in an ASP.NET Core test server. Architecture tests confirm no Infrastructure/EF runtime assembly reference from Presentation and no ASP.NET Core/EF/Infrastructure reference from Application or Domain. The runtime-only project build dependency delivers Infrastructure libraries; its composition entry point is loaded only at startup. Local publish and HTTPS smoke checks exercise the delivered host independently of the test server.

Environment issues were also recorded. Sandboxed restore/publish/build sometimes returned failure without compiler diagnostics; normal unsandboxed tooling retries succeeded. The sandboxed host could not write the Windows Data Protection key store. A subsequent HTTP smoke request correctly failed because antiforgery requires HTTPS; the smoke harness was changed to HTTPS using the existing development certificate. Certificate verification was bypassed only in the local smoke client; no certificate was installed/trusted or application TLS policy weakened. These were tooling/harness issues, not passing-test claims. The final cookie implementation lets antiforgery supply its own no-cache/no-store headers instead of redundantly setting them.

Raw artifacts are under `artifacts/test-evidence/sprint-4/batch-a/`, including:

- `Sprint4_A1_Regression.trx`
- `Sprint4_A2_Focused.trx`
- `Sprint4_A2_A4_Focused.trx` (retains the three initial assertion failures)
- `Sprint4_Authentication_Retest.trx`
- `Sprint4_BatchA_Final_Regression.trx`
- `Final_Build.log`, `Final_Publish.log`
- `PublishedHost_Smoke_Result.txt`, HTTPS host logs and earlier failed smoke/build logs

## Production implementation inventory and minimum code review

**9 production files added; 14 production files modified; approximately +875 physical production lines**, including C# comments, blank lines and project XML. Calculated from tracked `git diff --numstat -- src` plus physical lines in untracked production files; excludes tests, reports and evidence.

Added production files:

| File under src | Justification |
|---|---|
| `SyncBridge.Core.Application/Contracts/Identity/AuthenticatedActor.cs` | Trusted actor/role and credential-result contracts, with four role constants |
| `SyncBridge.Core.Application/Services/OperationResult.cs` | Small result vocabulary and the DTOs required by supported operations |
| `SyncBridge.Core.Application/Services/ApplicationOperation.cs` | Shared authorization/result/transaction cleanup boundary |
| `SyncBridge.Core.Application/Services/ProductionOrderService.cs` | Cohesive production-order operations and standalone audit |
| `SyncBridge.Core.Application/Services/BatchService.cs` | Cohesive manufacturing execution and review operations |
| `SyncBridge.Core.Infrastructure/ApplicationComposition.cs` | Infrastructure DI composition entry point |
| `SyncBridge.Core.Infrastructure/Identity/ManufacturingIdentity.cs` | Established password hashing, configured identities/roles and active AppUser resolution |
| `SyncBridge.Core.Infrastructure/Persistence/Repositories/AuditPersistence.cs` | Reused evidence row mapping/append path |
| `SyncBridge.Core.Infrastructure/Persistence/Repositories/BatchCommandPersistence.cs` | Needed command/evidence additions to the existing BatchRepository |

Modified production files:

- Application: `Contracts/Identity/IActorContext.cs`; `Contracts/Persistence/IApplicationUnitOfWork.cs`, `IBatchRepository.cs`, `IProductionOrderRepository.cs`; Application project file.
- Domain: `Entities/Batch.cs`; `Enumerations/AuditEventType.cs`.
- Infrastructure: `Persistence/ApplicationUnitOfWork.cs`; `Persistence/Assembly/BatchAggregateAssembler.cs`; `Persistence/Repositories/BatchRepository.cs`, `ProductionOrderRepository.cs`; Infrastructure project file.
- Presentation: `Program.cs`; Presentation project file.

Added test files: SecuredServiceFixture, SecuredApplicationServiceTests, SecuredPersistenceIntegrityTests, AuthenticationBoundaryTests and SqlServerApplicationServiceTests. Modified test project dependencies and added a LiveSqlServer category to the existing SQL test class; its ten test bodies/assertions remain unchanged.

Production adds only the already-used .NET logging abstraction package and the installed ASP.NET Core framework reference. SQLite and MVC Testing 10.0.12 are test dependencies. No MediatR, CQRS, generic repositories, messaging, custom security framework, new manufacturing tables or speculative handler/service-per-operation structure was added.

## Deployment configuration and Batch B prerequisites

Before exercising manufacturing services against the accepted SQL database, the Team must supply approved configuration:

1. `ConnectionStrings__SyncBridgeCore` or approved Presentation development user secrets, targeting localhost `SyncBridgeCore` with Windows Authentication.
2. `ManufacturingIdentity:Accounts` entries with UserId/UserName exactly matching existing active AppUser records, a PasswordHasher-produced PasswordHash and approved role strings from the matrix. Keep credential hashes in approved secrets/environment configuration, not source files. No default passwords or automatic AppUser provisioning are supplied.
3. An HTTPS host and ordinary Windows Data Protection access. The authentication form is `/authentication`. No manufacturing workflow UI exists in this batch.

The two new live service tests require both Team-approved unsandboxed SQL execution and `SYNCBRIDGE_APPROVED_LIVE_SQL=1`; their connection guard permits only localhost `SyncBridgeCore` with Windows Authentication. The live test fixture temporarily assigns Operator/Supervisor roles to existing fictional `demo.operator` through test configuration, not database changes. One new test reads the accepted order through trusted identity; the other induces a write failure and checks that both order and audit are rolled back. Neither recreates, seeds or resets the accepted database.

Batch B must obtain that explicit SQL approval, rerun the existing ten SQL tests and the two new SQL service tests, and perform any additional provider-specific end-to-end/concurrency verification its prompt requires. Production SQL Server concurrency behavior has been designed around serializable transactions but has not been exercised in this execution. SQLite success is not a substitute for this evidence.

No known failing implemented path remains in the executed tests. Remaining limitations are unexecuted live SQL/provider verification, deployment account configuration and Team review/acceptance. A rejected deviation remains blocking under the existing Domain workflow; no new rejection recovery or reopening path is supplied. Review queries favor straightforward bounded-demonstration data loading, without claiming production-scale performance qualification.

Batch A deliverables are the uncommitted source/tests, this complete report, the retained A1 inspection/decision record and raw evidence. The uploaded handoff remains preserved. Batch B was not started; final consolidated commit, merge and push remain pending Batch B and Team authorization.
