# Sprint 4 implementation decision register

Date: 2026-10-13. Status: decisions implemented and verified in Batch B; golden documents unchanged. Authority is the corrected SRS/SDD, amended DBDS and explicit Batch A2/B prompts. This register authorizes no commit, merge, push or future scope expansion.

## Approved role decisions

| ID | Team-approved decision | Implementation/evidence |
|---|---|---|
| D1 | Production Supervisor exclusively controls existing ProductionOrder status transitions | ProductionOrderService; all status actions deny Operator/Quality/Admin without Supervisor; positive transition and audit tests |
| D2 | Quality Engineer handles deviation review/approval/rejection/resolution using existing Domain; no new self-approval restriction | BatchService; disposition denial, same-actor multi-role and live quality-resolution tests |
| D3 | Production Operator resumes only after all blocking deviations close | ResumeAsync delegates to Batch.ResumeProcessing; unresolved/rejected cases denied; separate live resolution/resumption |
| D4 | Operator/Supervisor/Quality can read manufacturing context; audit/exception review only Supervisor/Quality; Administrator alone has no manufacturing access; roles additive | Every Application operation authorizes current trusted roles; role, spoofing, revocation, review and Administrator cases pass |

D1–D4 were explicitly approved in `CodexPrompt_Sprint_4_Batch_A2.txt`. SRS §2.2, §6.1 diagram, §6.4 and realigned plan §4.4 establish their source context. Future SRS role/action clarification may incorporate this approved matrix through Team change control, without changing policy in this sprint.

## D5 Resolved Exception rehydration clarification

Original problem: Batch's restricted rehydration constructor required at least one blocking deviation whenever stored state was Exception. Existing Deviation.Resolve closes the last deviation while the separate Operator Batch.ResumeProcessing command has not yet run. Saving Quality resolution therefore produced a valid intermediate condition that the constructor refused to reload.

Valid condition: Exception with one or more permanently associated originating deviations, all Closed, awaiting separately authorized Operator resumption. It is not InProgress and cannot execute steps or complete until resumption. Unresolved deviations continue to block resumption. Completed continues to reject blocking deviations.

Minimal correction: require an originating deviation for Exception, instead of requiring an unresolved one. Reuse the original restricted factory and assembler; no command replay, new state, transition, reopening/correction workflow or schema modification.

Sources: SRS FR-003/FR-009, UC-004, BR-006; SDD §§5.1/5.3 controlled exception review/resumption; DBDS §§5.9/5.12/7.2 separate persisted state/disposition; accepted OQ-4 and D2/D3. `EndToEndManufacturing_UsesTrustedActorsAndSurvivesFreshScopes` and live `CompleteManufacturingPath_RoundTripsSecurityOptionalFieldsAndEvidenceOnSqlServer` prove fresh-scope loading after final resolution and subsequent Operator resumption.

Recommendation: future SDD §5.3 may explicitly describe this intermediate condition and separate roles. It is a clarification of existing behavior, not a new workflow policy. Batch B authorizes recording it; no SDD edit was made.

## Other bounded implementation decisions

| ID | Decision and rationale | Verification / future documentation disposition |
|---|---|---|
| D6 | Use ASP.NET Core cookies, PasswordHasher, antiforgery/rate limiter and a small configured infrastructure account/role store linked to active AppUser. Never trust role/actor request claims | Host and actor tests; live trusted mapping; no new tables/dependencies for IAM. Document concrete deployment in implementation guidance; no DBDS change |
| D7 | Authorize inside Application; use one serializable command transaction/scoped UoW; clear tracking on failure. Avoid automatic retry/framework additions | SQL rollback, error 515 same-scope reuse and bounded two-context collision; SQL1205 victim is a sanitized failure with no partial state. SDD may document concrete transaction isolation/retry responsibility, without load guarantees |
| D8 | Keep optional DB-only fields unless the requested operation intentionally supplies recorded step data; keep one current verification and append immutable audit history | SQL stable-ID, lot/calibration/comment/recorded-data/disposition checks. No verification-attempt table or new workflow needed |
| D9 | Deliver Infrastructure at build/runtime composition only, with no Presentation compile reference; return DTOs, reuse three repositories and existing assembler evidence | Assembly-reference checks; published HTTPS authentication and credential-service DI activation. This is a bounded composition choice, not a fifth architectural layer |
| D10 | Append production-order creation/status audit action values, with nullable Batch context | Standalone order evidence and live 2-row order audit; existing enum ordinals/string persistence unchanged. No schema amendment required |
| D11 | Persisted reconstruction failures are technical/persistence failures, distinct from valid command rule/input rejection; retain original exceptions for logging | Live corruption regression initially failed, then Batch/order/recipe reconstruction boundaries were corrected; final TechnicalFailure results and diagnostic errors pass. Future SDD implementation/error-handling guidance may clarify this boundary |

No manufacturing policy ambiguity remains unresolved within the implemented scope. Deployment accounts/certificates, production-scale capacity, UI/API/reporting and any rejected-deviation recovery enhancement remain separate responsibilities, not implicit approvals from this register.
