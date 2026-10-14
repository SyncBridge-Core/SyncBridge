# Sprint 4 requirements and evidence matrix

Date: 2026-10-13. Final verification: **252 passed, 0 failed, 0 skipped; 17 live SQL cases passed.** Scope is secured Application services over accepted Domain/persistence. FR-011, FR-012 and BR-007 remain deferred.

Authority: corrected SRS/SDD, amended DBDS, realigned plan §4.4, accepted handoff and Team-approved D1–D4. Paths below are repository-relative. Test names refer to methods in the named classes, and all listed checks are included in `artifacts/test-evidence/sprint-4/batch-b/Sprint4_BatchB_Final_252.trx`.

## Functional requirements

| Requirement | Implementation | Passing evidence |
|---|---|---|
| FR-001 Authentication/authorization | `Infrastructure/Identity/ManufacturingIdentity.cs`, Web `Program.cs`, Application `ApplicationOperation.cs`, identity contracts | AuthenticationBoundaryTests (cookie/antiforgery/inactivity/rate limit); SecuredApplicationServiceTests denied actors, spoofing, missing/inactive users, additive roles; SQL trusted-user and full manufacturing cases |
| FR-002 Production orders | ProductionOrderService, ProductionOrderRepository, AuditPersistence; existing ProductionOrder transitions | ProductionOrderAudit_ExistsWithoutBatchAndCapturesEveryStatusCommand; OrderStatusTransitions_AreSupervisorOnly; InvalidQuantity; SQL full path and error-515 rollback/reuse |
| FR-003 Batch lifecycle | BatchService lifecycle operations; Batch Domain gates; BatchRepository/UoW | Accepted BatchLifecycleTests; end-to-end fresh-scope completion; live SQL resolved Exception then separate Resume; concurrent Ready single-transition case |
| FR-004 Guided workflow | BatchService StartStep/CompleteStep, ordered Recipe DTOs, RecordedData persistence; Batch/BatchStepExecution | OrderedWorkflowExecutionTests; SkippedStepsInvalidVerificationAndForeignSignatureTargets; SQL ordered execution/recorded-data scenario |
| FR-005 Material verification | VerifyMaterialAsync, recipe requirements, SaveMaterialVerificationAsync, current row plus audit history | MissingFailedPrerequisitesAndReviewQueries_RetainReady; stable-ID/optional-field tests; live SQL failed/successful verification and rollback |
| FR-006 Equipment verification | VerifyEquipmentAsync, recipe requirements, SaveEquipmentVerificationAsync | Accepted prerequisite tests; missing/failed readiness cases; live SQL calibration/comment and identity preservation |
| FR-007 Electronic signatures | SignStepAsync; Domain signature/step gate; repository same-Batch/target restriction; immutable UoW | ElectronicSignatureGateTests including wrong record type; denied-role/foreign-target tests; SignaturesAreImmutableAndCompletedBatchAndChildrenAreProtected; SQL signature/audit failure rollback |
| FR-008 Audit trail | Domain-generated evidence; standalone order audit; AuditPersistence; UoW append-only guard | ProductionOrderAudit; AuditMutation_IsRejectedByActualUnitOfWork; accepted SQL immutable evidence test; SQL end-to-end 19 Batch + 2 order audits with correct actors |
| FR-009 Deviations | CreateDeviation/ReviewDeviation/Resume; existing Deviation/Batch; explicit row updates preserving origin | Accepted deviation lifecycle tests; WrongSignatureAndRejectedDeviation_CannotAdvanceWorkflow; additive same-actor case; SQL fresh-scope review/disposition/resumption |
| FR-010 Review by Exception | BatchService ReviewExceptionsAsync over recipe requirements/current verifications/deviations | MissingFailedPrerequisitesAndReviewQueries; role-denied review tests; SQL review throughout prerequisite/deviation/completion lifecycle |
| FR-011 EBR generation | Deferred to reporting increment | No completion claim; persisted final evidence is available |
| FR-012 Manufacturing REST services | Deferred to API increment | No manufacturing controllers/API workflows implemented; authentication-only boundary is not FR-012 completion |

Implementation prefixes: Application services under `src/SyncBridge.Core.Application/Services`; Infrastructure under `src/SyncBridge.Core.Infrastructure`; Domain under `src/SyncBridge.Core.Domain`; Web host under `src/SyncBridge.Core`.

## Business rules

| Rule | Authority/implementation gate | Evidence |
|---|---|---|
| BR-001 Mandatory successful materials | Batch.StartProcessing evaluates Recipe.MaterialRequirements | Accepted prerequisite tests; non-SQL missing/failed/retry; live SQL Ready rejection and eventual start |
| BR-002 Verified equipment | Same Domain gate evaluates Recipe.EquipmentRequirements | Accepted prerequisite tests; live failed then successful equipment verification |
| BR-003 Prescribed sequence | Batch.StartStep and CompleteStep | Accepted ordered workflow tests; live skipped step rejected without partial execution |
| BR-004 Required signatures | Batch.CompleteStep checks target/type/Batch | Accepted signature-gate positives/negatives; live unsigned completion rejected; Quality-signed completion succeeds |
| BR-005 Significant action audit | Domain evidence plus standalone order audit; atomic UoW | Live final audit reconciliation; rollback tests prove state/evidence consistency |
| BR-006 Permanent deviation origin | Existing Deviation identity/origin; update changes only status/review/disposition fields | Accepted deviation tests and live step-associated deviation round trip |
| BR-007 Accurate EBR | Deferred | No SSRS/EBR implementation or report reconciliation claim |
| BR-008 Completed protection | Application mutation gate; Domain terminal state; UoW parent/child protection | Accepted CompletedRecordProtectionTests; direct immutable/child tests; live normal Completed mutation denial |

## Non-functional obligations

| Requirement | Sprint 4 evidence and practical boundary |
|---|---|
| NFR-001 Security | All four roles tested through real identity/service/persistence; spoofed role claims ignored; active mapping required; encrypted secure cookies, antiforgery and rate limiting; no deployment passwords/hashes stored in source |
| NFR-002 Performance/capacity | Final live fresh-scope secured Batch read 43.455 ms; meets approximate two-second local demonstration target. No production-scale load qualification or EBR timing claim |
| NFR-003 Integrity/reliability | Real SQL atomic state/audit/signature rollback, error-515 constraint failure, service-scope reuse, immutable evidence, 1205 deadlock victim with exactly one transition/audit; invalid persisted data classified TechnicalFailure |
| NFR-004 Usability | Ordered instructions/consistent service results support future adapters. Manufacturing UI usability is deferred to Sprint 5; only minimal authentication form exists |
| NFR-005 Maintainability | Inner-layer assembly checks pass; focused existing repositories/UoW and Domain reused; no generic handler/security framework; +902 physical production lines, only +27 in Batch B |

## Architectural decisions and persistence

| ADR / accepted decision | Verification |
|---|---|
| ADR-001 Lightweight Clean Architecture | Runtime dependency test and project-reference inspection; Presentation has no Infrastructure compile reference; Domain/Application isolated |
| ADR-002 Razor Pages | Existing host retained; manufacturing screens deferred to Sprint 5 |
| ADR-003 EF Core | Existing scoped DbContext/mappings/repositories; SQL tests use real provider; no migration changes |
| ADR-004 SQL system of record | Windows-auth localhost SyncBridgeCore; 15 deployed core tables; executed provider round trips |
| ADR-005 REST | Shared DTO/service boundary ready; manufacturing REST implementation deferred |
| ADR-006 Rich Domain | Existing manufacturing gates/transitions invoked; rehydration clarification changes no command path |
| ADR-007 Explicit state machine | Exactly Created, Ready, InProgress, Exception, Completed; accepted and live state/gate tests |
| ADR-008 Repositories | Existing three repositories reused; no generic/fourth repository |
| ADR-009 DI | Scoped persistence/UoW/actor/services; public Infrastructure composition entry point; published host HTTP 200/401 checks |
| ADR-010 Isolated SSRS | Reporting remains deferred; no reporting state mutations/subsystem introduced |
| OQ-1 Quantity | Order.Quantity authoritative; Batch.PlannedQuantity derived; decimal(18,3) boundary; no UnitOfMeasure |
| OQ-2 Deviation identity | Separate DeviationId and required unique DeviationCode; live Unicode/unique tests and metadata |
| OQ-3 Technical IDs/signature target | Lowercase GUID D conversion; BatchStepExecution-only signatures; negative Domain/service/repository evidence |
| OQ-3A Audit identity | Required nvarchar(256), 512-byte SQL metadata; accepted Unicode and length tests pass |
| OQ-4 Restricted rehydration | Existing assemblers/factories retained; reads replay no actions; closed-deviation Exception reload/resume proven live |

SQL metadata and cleanup evidence: `Sql_Postflight_Output.txt`; final live outcomes and scenario output: `Final_SQL_Test_Results.json`, `SQL_Scenario_Summary.txt`. Acceptance remains a Team decision; no Git integration occurred.
