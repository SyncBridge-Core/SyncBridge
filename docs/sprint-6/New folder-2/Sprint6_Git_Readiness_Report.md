# Sprint 6 Git readiness report

Date: 2026-10-15, America/New_York. Team acceptance: **PENDING**. Git integration: **NOT AUTHORIZED / NOT EXECUTED**. This is a proposed inclusion and execution plan; it grants no authorization.

Active branch: `codex/wp6-sprint6-rest-api`. HEAD: `e2cbbb56e786f2ba2dbc57750b1c06f370110452`. Upstream: `origin/codex/wp6-sprint6-rest-api`. Local branch/upstream ahead/behind: 0/0. Read-only `git ls-remote origin refs/heads/main refs/heads/codex/wp6-sprint6-rest-api` independently confirmed both remote heads at this SHA; local main matches. No fetch, ref mutation or history change occurred.

There are zero staged files. Production changes are exactly the accepted Batch A Web Program additions and new controller; Batch B adds no production code. Batch B adds two test files, shares the existing fixture clock with its HTTP host and exposes the existing guarded test-owned SQL cleanup method internally. No Domain/Application/Infrastructure/project/package/migration changes are proposed.

Exact per-file staging proposal: `artifacts/test-evidence/sprint-6/batch-b/Git_Include_Paths.txt`, with categories/status in `Git_Inclusion_Manifest.json`. This manifest enumerates individual paths for production/tests, all Sprint 6 documents (including the relocated Batch A reports) and reviewed sanitized Sprint 6 Batch A/B evidence. Directory-wide or repository-wide staging is not proposed. It includes the final manifest/readiness artifacts themselves; it does not include protected historical files. Reconcile all hashes and inventory again before any later authorized staging.

Final proposal: **82 exact files** — 2 production, 5 test, 11 documentation and 64 sanitized evidence files. Three are tracked modifications (Program, SecuredServiceFixture and SqlServerBatchBIntegrationTests); the other 79 are new untracked candidates. The per-path Git status in the manifest is authoritative; all remain unstaged.

Accepted Batch A documents were already located under `docs/sprint-6/New folder-1/` at Batch B entry. Preserve that user-established location. The original five documents remain; only the decision register receives the clearly marked Batch B addendum. Historical paths in Batch A reports describe their original writing state; the final evidence index resolves current locations. The previously referenced empty `Inner_Layer_Changes.txt` was absent at B preflight; fresh `Architecture.json` supplies explicit unchanged-layer evidence instead. No accepted history is silently rewritten.

Exclusions are enumerated in `Git_Exclusions.json`. Preserve and do not stage all ten inherited files:

- `artifacts/test-evidence/sprint-4/batch-b/Sql_Preflight_Output.txt`
- `artifacts/test-evidence/sprint-5/batch-a/A3_Initial_Checkpoint_Historical.md`
- `artifacts/test-evidence/sprint-5/batch-a/Final_SQL_Postflight.txt`
- `artifacts/test-evidence/sprint-5/batch-a/Security_Patch_Dependency_Chain.log`
- `artifacts/test-evidence/sprint-5/batch-a/Sql_Postflight.txt`
- `artifacts/test-evidence/sprint-5/batch-a/Sql_Preflight_Elevated.txt`
- `docs/WP6_Sprint5_to_Sprint6_Engineering_Handoff.docx`
- `docs/sprint-4/Sprint4_Git_Integration_Completion_Report.md`
- `docs/sprint-5/Sprint5_Git_Commit_Completion_Report.md`
- `docs/sprint-5/Sprint5_Git_Integration_Completion_Report.md`

Also exclude all inherited ignored `.vs`, `bin`, `obj`, `.csproj.user` outputs; User Secrets, private keys/certificates, credentials and database files. None are copied into the proposal. The source handoff remains an unchanged reference, not silently included as new Sprint 6 content. Golden documents remain outside the proposal and unchanged.

The final evidence index and engineering verification report record exact passing gates, preservation, source hashes and limitations. `Sensitive_Content_Check.json` records bounded candidate scans and `Diff_Check.txt` records whitespace validation. Fictional test password constants already accepted in test fixtures are test values, not deployment credentials. No raw development row payloads are proposed; preservation JSON contains ordered hashes/counts and retained demonstration summary only.

Proposed later consolidated commit: `feat(wp6): complete Sprint 6 secured REST services and integrated verification`.

After a separate explicit Team acceptance and Git-closure instruction only:

1. Reconfirm branch/HEAD/remotes, exact inclusion/exclusion manifest, source hashes, index, sensitive-content and preservation checks. Stop on new unreviewed changes or divergence.
2. Stage only exact manifest paths; inspect the staged diff and security inventory; create the single consolidated Sprint 6 implementation commit.
3. Merge to main preserving history with the explicitly authorized normal merge strategy. Do not reset, clean, force-push, rewrite history or include inherited artifacts.
4. Run authorized post-merge restore/build/advisory/integrated SQL regression, independent SQL repeat and read-only development preservation checks.
5. Publish feature/main without force, verify remote SHAs and retain exact evidence.
6. Create/publish the Sprint 7 branch from verified main only under the separately issued closure instruction, using its approved branch name. No Sprint 7 implementation is part of this proposal.
7. Record actual final commit/merge/publication/next-branch identities and final inventory.

No staging, commit, merge, push, branch creation, reset, cleanup or history rewriting has occurred in Batch B. Technical readiness is a recommendation for Sam/Team review, not Sprint acceptance.
