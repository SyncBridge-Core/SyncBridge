# Sprint 5 final requirements and evidence matrix

Date: 2026-10-14, America/New_York. Scope follows corrected SRS/SDD, amended DBDS, realigned plan §4.5 and inherited handoff decisions. Exact source hashes are verified in Batch B `Source_Documents.json`. Detailed accepted handler mappings remain in `Sprint5_BatchA_Requirements_and_Evidence_Matrix.md`; this matrix reconciles them with fresh B verification and accepted A3 interactive evidence.

Evidence keys: **HTTP** = `RazorWorkflowTests.UC001To004_HttpWorkflow_PreservesPrerequisitesSignaturesResolutionResumeAndCompletedProtection`; **AUTH** = `AuthenticationBoundaryTests`; **DIRECT** = `RazorWorkflowTests.DirectPost_ForbiddenRolesCannotSubstituteActorOrRole` and `DirectCreationPosts_AreForbiddenForNonSupervisors`; **SQL** = all 17 SQL tests in B integrated and independent TRX; **A3** = accepted A3 report/screenshots/retained records. Exact individual case names/outcomes are in B `Test_Catalog.json`. All automated references below belong to the passing 278-test run, not only source inspection.

| Requirement / use case | Implementation / architectural connection | Actual evidence and bounds |
|---|---|---|
| FR-001 authentication/authorization | Native authentication Page, accepted cookie endpoints, ManufacturingIdentity active AppUser mapping, ApplicationOperation role gates | AUTH, DIRECT, missing/revoked cookie, forged role/actor and additive-role HTTP cases; A3 two-account login/logout. No self-registration/admin module |
| FR-002 production orders | Index/Create/Details Pages → ProductionOrderService; existing repositories/Domain | HTTP, DIRECT, secured order status tests, SQL round-trip/rollback; A3 create/release/InProcess/Complete and order audit |
| FR-003 lifecycle; UC-001 initiate | Initiate/Details handlers → secured BatchService → existing five-state Batch | HTTP rejects unreleased order/missing prerequisites; SQL all-five-states reconstruction and full path; A3 initiation, Ready/start, Exception/resume, Supervisor completion |
| FR-004 guided execution; UC-003 execute step | StartStep/CompleteStep → BatchService/BatchStepExecution ordering | HTTP/Domain skipped-step rejection and data persistence; SQL full path; A3 step order/recorded data. Crafted skipped-step browser request not executed |
| FR-005 material verification; UC-002 | Material handler → VerifyMaterialAsync; RecipeMaterialRequirement/MaterialVerification | HTTP missing/failed prerequisites, secured persistence failure rollback, SQL evidence round-trip; A3 missing rejection and passed verification |
| FR-006 equipment verification | Equipment handler → VerifyEquipmentAsync; RecipeEquipmentRequirement/EquipmentVerification | HTTP/SQL missing/failed verification gates and evidence; A3 missing rejection and passed equipment confirmation |
| FR-007 electronic signatures; UC-003 gate | Sign/CompleteStep → secured services; actual BatchStepExecution target | HTTP missing/wrong target/signature cases; immutable evidence tests; SQL full path/rollback; A3 unsigned step rejection, Quality Approval and correct association |
| FR-008 audit trail | Audit Page → secured order/batch audit services; append-only AuditEvent | HTTP workflow audit assertions; `AuditMutation_IsRejectedByActualUnitOfWork`, SQL atomicity/attribution; A3 audit views and retained 21 linked events |
| FR-009 deviation; UC-004 resolve | Deviation/Review/Resume → secured service and Domain; persistent batch/activity origin | HTTP Quality rejection and additive own-review; SQL full path; A3 open/review/approve/resolve, closed-but-Exception, explicit Operator resume |
| FR-010 Review by Exception | Review/dashboard/batch sections → ReviewExceptionsAsync | HTTP and SQL missing/failed verification/blocking deviation checks; A3 review before verification, Quality disposition and final clear review. No reporting/analytics engine |
| BR-001, BR-002 | Domain start requires all mandatory material/equipment evidence | HTTP, Domain prerequisite tests, SQL full path; A3 rejected start then accepted evidence |
| BR-003 | Prescribed RecipeStep order, no bypass | HTTP/Domain skipped-step negative cases; SQL full path; A3 step-2 control withheld until eligible |
| BR-004 | Required controlled signature before step progression | HTTP missing/foreign target rejection and SQL full path; A3 rejected unsigned completion then approved target signature |
| BR-005 | Significant mutations append trusted actor/time/entity/result audit atomically | HTTP audit assertions; SQL full path/rollback/concurrency tests; A3 order/batch audit views and preservation hashes |
| BR-006 | Permanent Deviation.BatchId plus optional originating execution | HTTP/Domain deviation cases, SQL reconstruction/FKs; A3 step-associated closed deviation preserved |
| BR-008 | Completed normal manufacturing records/evidence immutable; no correction/reopen feature | HTTP late mutations; `SignaturesAreImmutableAndCompletedBatchAndChildrenAreProtected`; SQL approved unit-of-work tests; A3 real stale form rejected after completion |
| NFR-001 security | Trusted identity, least operation roles, cookie/HTTPS/antiforgery/rate limit, safe errors | AUTH/DIRECT/revocation/forgery/rollback tests; source review and secret scan. No formal security certification |
| NFR-004 usability | Thin native Pages, role/state controls, ordered tasks, labels/validation/feedback, completed review | HTTP rendering assertions and accepted A3 walkthrough; no independent accessibility or broad user-study certification |
| NFR-005 maintainability | Inward dependencies, DI, secured facade services, repositories behind contracts | Passing architecture test; project-reference and PageModel review; no new framework or Batch B source refactor |

| Applicable ADR | Verified disposition |
|---|---|
| ADR-001 Clean Architecture | Four existing layers; PageModels contain no persistence references; runtime Infrastructure composition remains accepted |
| ADR-002 Razor Pages | Native protected manufacturing adapters, authentication presentation and feedback |
| ADR-003 EF Core; ADR-004 SQL system of record | Existing Fluent API/migration, 15 tables and amendments; real isolated SQL tests and unchanged retained development history |
| ADR-006 rich Domain; ADR-007 explicit state machine | Domain gates remain authoritative; exactly five Batch states; Completed terminal; review/approval not extra states |
| ADR-008 repositories; ADR-009 DI | Existing inner contracts/Infrastructure implementations; only bounded dashboard listing extensions; no reverse runtime references |
| ADR-005 REST; ADR-010 isolated SSRS | Preserved as later-sprint boundaries; no claim of implementation in Sprint 5 |

FR-011, BR-007 and UC-005 EBR reconciliation remain Sprint 7; FR-012 manufacturing REST remains Sprint 6. NFR-002 load/performance targets and broader final-system acceptance are not certified by this sprint's bounded test timings. The two-account A3 coverage limits are retained in the final engineering report. Batch A is Team accepted; this matrix supports final Sprint 5 review without claiming final Team acceptance.
