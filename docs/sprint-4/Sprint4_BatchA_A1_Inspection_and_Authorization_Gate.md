# Sprint 4 Batch A inspection and authorization gate

Date: 2026-10-13 (America/New_York)

Status: **A1 inspection performed; Team authorization decisions pending. Batch A implementation is not complete.**

Subsequent disposition: `CodexPrompt_Sprint_4_Batch_A2.txt` explicitly approved D1–D4 and authorized consolidated A2–A4 implementation. This document retains the original A1 inspection status as historical evidence; see `Sprint4_BatchA_Completion_Report.md` for current implementation and verification.

## Repository verification

- Execution branch: `feature/wp6-sprint4-application-services`.
- HEAD: `c42fce5a345c5fe2bb8b58843302a170f0d841c1`, matching the accepted main baseline.
- Accepted implementation commit `a4bc59658b00ec464480eb6b346afd90dbaae5f6` is present immediately below the merge in the inspected history.
- Initial working tree contains only the user-provided, untracked `docs/sprint-3/WP6_Sprint3_to_Sprint4_Engineering_Handoff.docx`. It is preserved.
- No production source, migration, database, or historical commit was changed during this inspection. No commit, merge, or push was performed.

## Source register and inspected requirements

| Source | Location | Relevant coverage |
|---|---|---|
| Batch A execution prompt | `Z:\InstallationPackages\Transfer_Folder\Sprint_4\CodexPrompt_Sprint_4_Batch_A.txt` | A1–A4 checkpoints, authorization approval gate, minimum code, scope and Git/database controls |
| Corrected SRS | `C:\Engineering\Golden_Documents\SyncBridge_Core_SRS_Corrected.docx` | §2.2 roles; FR-001–FR-010; BR-001–BR-008; NFR-001/003/005; §6.1 use-case diagram; UC-001–004; §6.4 role data flows |
| Corrected SDD | `C:\Engineering\Golden_Documents\SyncBridge_Core_SDD_Corrected.docx` | §3.2 inward dependencies; §§4.2–4.5 ownership; §§5.1–5.4 Domain operations, service DTOs, persistence and identity support |
| Amended DBDS | `C:\Engineering\Golden_Documents\Indy-1-SyncBridge-DBDS_v2.docx` | AppUser and role separation; evidence tables; §7.2 atomic commands; §8.2 exception queries; AMD-001/002 |
| Realigned execution plan | `C:\Engineering\Golden_Documents\WP-6_Implementation_Execution_Plan_Realigned.docx` | §4.4 secured services, Team role/action interpretation, DTOs, transactions and tests |
| Sprint 3 to Sprint 4 handoff | `docs/sprint-3/WP6_Sprint3_to_Sprint4_Engineering_Handoff.docx` | Accepted baseline, persisted evidence, optional fields, secured update gaps, review before new use-case contracts |
| Accepted reports | `docs/sprint-3/Sprint3_Final_Closure_Report.md`, `Sprint3_Final_Evidence_Index.md`, `Sprint3_Task1_Approved_Persistence_Design_Decisions.md` | Historical verification and accepted OQ-1/2/3/3A/4 decisions |
| Accepted code | `src/SyncBridge.Core.Application/Contracts`, `src/SyncBridge.Core.Domain/Entities`, `src/SyncBridge.Core.Infrastructure/Persistence`, existing tests | Current contracts, actor seam, Domain operations, row assembly, scoped commit and regression checks |

All five required DOCX sources are now available. The SRS use-case diagram was inspected as an image, in addition to its surrounding text. It explicitly links order creation and batch completion to the Supervisor, signatures to Operator and Quality Engineer, deviation review to Quality Engineer, and audit review to Supervisor and Quality Engineer. The diagram resolves permissions that the prose alone leaves less specific.

## Proposed role to operation matrix

This is a review proposal, not an implemented authorization policy. Only the listed roles would permit each operation. A role does not inherit another role's permissions; an actor with multiple assigned roles may use the union of those permissions. Every operation additionally requires an authenticated identity mapped to an existing, active AppUser.

| Application operation | Proposed permitted roles | Authority and approval status |
|---|---|---|
| Authenticate | All four roles | SRS FR-001 and §6.1 diagram; established |
| Create production order | Production Supervisor | SRS §6.1 diagram and §6.4 production-order flow; established |
| Release, cancel, start processing, complete production order | Production Supervisor | Existing Domain operations and FR-002 management; specific role allocation needs Team approval (D1) |
| Initiate batch, prepare Ready state, begin manufacturing | Production Operator | UC-001 and §2.2; grouping preparatory operations under initiation is a bounded implementation interpretation |
| Verify materials and equipment | Production Operator | SRS §2.2, §6.1 diagram, UC-002, FR-005/006; established |
| Create/start/complete ordered step execution | Production Operator | SRS §6.1 diagram and UC-003; established |
| Record electronic signature on BatchStepExecution | Production Operator, Quality Engineer | SRS §6.1 diagram and §6.4; target restriction follows accepted OQ-3 |
| Create deviation and interrupt batch | Production Operator | SRS §6.1 diagram, UC-004; existing Domain rules determine valid interruption |
| Begin deviation review | Quality Engineer | SRS §6.1 diagram, §2.2 and UC-004 supporting actor; established |
| Approve/reject deviation and resolve with final disposition | Quality Engineer | Quality responsibilities and existing Domain methods support this interpretation; exact permission allocation needs Team approval (D2) |
| Resume batch after all blocking deviations close | Production Operator | Consistent with execution ownership; explicit resumption role needs Team approval (D3) |
| Complete batch | Production Supervisor | SRS §6.1 diagram and §6.4 completion-request flow; established |
| Read orders, recipes, batch execution context and associated evidence | Production Operator, Production Supervisor, Quality Engineer | Necessary execution/review reads; precise shared visibility needs Team approval (D4) |
| Review audit history and Review by Exception | Production Supervisor, Quality Engineer | SRS §2.2, §6.1 audit review, FR-010; established supervisory/quality responsibility |
| Manufacturing commands or review reads for Administrator alone | None | §2.2 separates administration from production; proposed default denial, included in D4 |

Administrative account/configuration workflows are not added to this manufacturing batch. All four roles remain valid identity roles. An Administrator needing production access would require an explicitly assigned manufacturing role.

## Decisions requiring Team disposition

- **D1:** Assign existing production-order status commands exclusively to Production Supervisor. Expose only existing Domain transitions; do not add editing, scheduling or allocation rules.
- **D2:** Assign deviation approval, rejection and resolution exclusively to Quality Engineer, with current Domain transition gates. No new deviation workflow or self-approval prohibition is proposed.
- **D3:** Assign resumption to Production Operator, only after Domain confirms every blocking deviation is closed. Supervisor-only resumption would be a different Team policy choice.
- **D4:** Allow Operator, Supervisor and Quality Engineer the manufacturing-context reads needed by their operations; reserve audit-review and exception-review operations to Supervisor/Quality Engineer. Administrator alone has no manufacturing permissions. Role membership is additive, without implicit Administrator bypass.

Batch A prompt §8 states: "Before implementing ambiguous role permissions, produce a concise role-to-operation matrix and request Team approval for choices not unambiguously established by the SRS." Checkpoint A1 also requires stopping significant unresolved policy choices. The realigned plan §4.4 assigns role/action interpretation to The Team. Implementation depending on D1–D4 is therefore pending explicit Team disposition.

## Reuse and bounded implementation proposal

These are proposed implementation directions, not completed functionality or approved final method signatures.

- Reuse IActorContext as the actor boundary, with the minimum additional authentication/role information needed. Infrastructure must validate the authenticated identifier against active AppUser; no request DTO may substitute an actor ID.
- Reuse ASP.NET Core authentication/authorization facilities. Application operations enforce role policy through an inward-facing identity contract. Domain remains independent of ASP.NET Core and EF Core. The precise authentication configuration will be documented when implementation proceeds; no identity store or manufacturing role tables have been introduced.
- Use cohesive production-order, batch-execution and manufacturing-review service capabilities. Reuse existing Domain transitions and evidence constructors. Return DTOs instead of mutable Domain entities.
- Extend existing order/Batch repositories only for required updates and narrowly scoped evidence/query operations. Existing reads reconstruct detached Domain objects; secured updates need explicit row synchronization rather than assuming Domain objects are EF-tracked.
- Preserve one-current-verification records and append audit evidence on controlled changes. Verification evidence must persist even before manufacturing starts. Preserve optional fields such as lot number, recorded step data, calibration evidence and final disposition when unrelated commands update rows.
- Persist production-order audit evidence without BatchId. Reuse the approved nullable BatchId column and audit identity limits, not a new table.
- Continue using one scoped DbContext and IApplicationUnitOfWork commit for each controlled command. Unexpected failures need sanitized caller results, diagnostic logging, and scope cleanup so a later commit cannot save a failed command's tracked changes.
- Reuse Domain signature validation and immutable persistence safeguards. Only BatchStepExecution within the same batch is allowed. Add tests for role denial, actor mapping, required signature gates and unchanged evidence.
- Implement exception queries from recipe requirements and normalized current verification/deviation records; no report engine or exception table.

Before A2/A3, resolve the role decisions above and record concrete identity/DTO/repository choices as required by the handoff. This proposal requests no schema or manufacturing behavior change.

## Verification actually executed

| Check | Actual result |
|---|---|
| `dotnet build src/SyncBridge.Core.slnx --no-restore --verbosity minimal` | Exit 0; 0 warnings; 0 errors |
| `dotnet test src/SyncBridge.Core.slnx --no-restore --filter 'FullyQualifiedName!~SqlServerRecoveryIntegrationTests' --logger 'trx;LogFileName=Sprint4_A1_Regression.trx' --results-directory artifacts/test-evidence/sprint-4/batch-a` | Exit 0; 191 passed; 0 failed; 0 skipped |
| Live SQL Server integration | Not run in this inspection; requires explicitly approved unsandboxed Windows execution under prompt §14 |
| Full accepted 201-test suite | Not rerun; the 10 SQL Server tests were deliberately excluded from the executed regression command |

Raw executed test evidence: `artifacts/test-evidence/sprint-4/batch-a/Sprint4_A1_Regression.trx`.

The accepted 201/201 and 10/10 SQL counts are historical Sprint 3 results confirmed by the handoff, not new Sprint 4 execution results. No Sprint 4 service tests exist yet. Neither development database nor protected legacy database was accessed.

## Implementation and deliverable status

- Production files added: 0. Production files modified: 0. Net production LOC: 0.
- Added inspection artifact: this report. Added executed test artifact: the regression TRX.
- Authentication/authorization, Application services, repository updates, atomic command tests and end-to-end service execution remain unimplemented.
- FR-001–FR-010 are inspected scope, not completed Sprint 4 requirements. FR-011/012 remain outside Batch A.
- No new production defect was established by the passing non-SQL regression. Pending policy decisions are implementation blockers, not evidence that existing Domain or persistence code is defective.
- Batch B is not ready: it requires completed A2–A4, final Batch A implementation evidence, full regression and explicitly approved SQL integration execution. Do not begin Batch B or perform Git integration at this gate.

Next action: Team approval or revision of D1–D4, followed by Batch A implementation and verification on the existing feature branch.
