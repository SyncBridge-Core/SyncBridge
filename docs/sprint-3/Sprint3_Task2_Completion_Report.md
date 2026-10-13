# WP-6 Sprint 3 Task 2 Completion Report

## 1. Task Objective

Sprint 3 Task 2 establishes the Entity Framework Core and Microsoft SQL Server configuration foundation for later persistence implementation. It adds compatible EF Core dependencies to Infrastructure and establishes a secret-free connection-string configuration convention without creating a DbContext, mappings, repositories, migrations, schema, or database objects.

**Final status: READY FOR TEAM REVIEW.** Task 2 is implemented and validated but is not formally accepted.

## 2. Starting Git Baseline

| Item | Actual value |
|---|---|
| Repository root | `C:\Engineering\GitHub\SyncBridge.Core` |
| Branch | `feature/wp6-sprint3-core-domain` |
| Starting HEAD | `887a01755c8b17139c3a1eac3692367440614aa9` |
| Accepted Task 1 commit in history | Yes; `git merge-base --is-ancestor` exit 0 |
| Starting working tree | Clean |

No branch switch, rename, merge, commit, tag, or push was performed.

## 3. Authoritative Engineering Sources Inspected

- `docs/sprint-3/Sprint3_Task1_Approved_Persistence_Design_Decisions.md`.
- `docs/sprint-3/Sprint3_Task1_Design_Gate_Closure_Report.md`.
- `docs/sprint-3/Sprint3_Task1_Final_Evidence_Index.md`.
- `docs/WP6_Sprint2_to_Sprint3_Engineering_Handoff_Team_Reviewed.docx`.
- `C:\Engineering\Golden_Documents\Indy-1-SyncBridge-DBDS_v2.docx`, including DBDS-AMD-001 and DBDS-AMD-002.
- `C:\Engineering\Golden_Documents\SyncBridge_Core_SDD_Corrected.docx`.
- `C:\Engineering\Golden_Documents\SyncBridge_Core_SRS_Corrected.docx`.
- `C:\Engineering\Golden_Documents\WP-6_Implementation_Execution_Plan_Realigned.docx`.

The sources require EF Core and SQL Server concerns to remain in Infrastructure, preserve inner-layer contracts, use one scoped DbContext per controlled command when implemented, and defer schema/migration work until the approved implementation sequence reaches those tasks.

## 4. Installed .NET and Project Frameworks

- Installed SDK: .NET SDK 10.0.401.
- Installed matching runtime: Microsoft.NETCore.App 10.0.12.
- Every solution project targets `net10.0`.
- No `global.json` pins a different SDK.
- `dotnet-ef` is not installed. This is recorded rather than changed because migrations are not authorized in Task 2.

## 5. EF Core Package Selection

| Package | Requested | Resolved | Purpose |
|---|---:|---:|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.12 | 10.0.12 | Stable SQL Server provider for the `net10.0` Infrastructure project |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.12 | 10.0.12 | Design-time services required by later authorized migration/tooling work; marked `PrivateAssets=all` |

EF Core 10 is the stable major aligned to .NET 10. Version 10.0.12 was selected as the current stable patch available from the configured `nuget.org` source. No preview or prerelease package was selected. `Microsoft.EntityFrameworkCore.Tools` was not added because the current workflow uses the .NET CLI and no migration/tool execution is authorized yet.

The provider resolves `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational` 10.0.12 and `Microsoft.Data.SqlClient` 6.1.6 transitively. The complete resolved inventory is stored with the raw evidence.

## 6. Project and Package Changes

`SyncBridge.Core.Infrastructure.csproj` now contains the two explicit EF Core package references. Existing ProjectReferences to Application and Domain are unchanged.

Domain and Application continue to resolve no NuGet package references. Presentation has no direct EF Core or SQL Server package. No unrelated dependency was upgraded.

## 7. Infrastructure Configuration Implemented

Task 2 establishes the provider and design-time dependency baseline only. It intentionally does not add a service-registration extension because there is no authorized DbContext, repository, or unit-of-work implementation to register.

The future Task 3 approach is:

1. Implement `SyncBridgeDbContext` inside the Infrastructure persistence namespace.
2. Configure `UseSqlServer(...)` only inside Infrastructure registration code.
3. Read and validate the `SyncBridgeCore` connection string at the application composition boundary, then pass it to the Infrastructure registration method without exposing EF types through Application or Domain.
4. Register the DbContext, repositories, and unit-of-work with scoped lifetimes after those implementations are authorized.

## 8. Connection-String Configuration Approach

The tracked configuration now contains the empty key `ConnectionStrings:SyncBridgeCore`. The empty value documents the configuration contract without assuming a SQL Server instance, authentication mode, server name, or credential.

The Presentation project now has a `UserSecretsId`, enabling ASP.NET Core development-time user secrets. Developers may provide the value with user secrets or the `ConnectionStrings__SyncBridgeCore` environment variable. No command setting a secret was executed, and no connection string value or credential was committed.

Non-destructive discovery found installed SQL command-line tools but could not reliably enumerate a SQL Server instance because LocalDB registry enumeration failed and service-manager access was denied. Task 2 therefore makes no instance or authentication assumption. Task 3 must obtain an approved development connection string before configuring the DbContext.

## 9. Architectural Dependency Compliance

- Presentation still references Application only.
- Application still references Domain only.
- Domain has no project reference and no EF Core or SQL Server package.
- Infrastructure references Application and Domain and exclusively owns the EF Core dependencies.
- Existing repository, unit-of-work, and actor-context contracts are unchanged.
- No manufacturing behavior, OQ implementation, or Domain type was changed.
- OQ-1, OQ-2, OQ-3, OQ-3A, and OQ-4 remain future persistence constraints and were not implemented by Task 2.

## 10. Build Result and Command Evidence

Command:

`dotnet build src\SyncBridge.Core.slnx --no-restore --verbosity minimal`

Exit code: 0. The full solution built successfully with 0 errors and 0 warnings. Sanitized output is stored at `artifacts/test-evidence/sprint-3/task-2/build-output.txt`.

Package restore used:

`dotnet restore src\SyncBridge.Core.slnx --verbosity minimal`

The first sandboxed attempt exited 1 without a package diagnostic. The authorized retry completed with exit code 0, restored Infrastructure, and reported the remaining four projects up to date.

## 11. Regression Test Results

Command:

`dotnet test src\SyncBridge.Core.slnx --no-restore --verbosity minimal --results-directory artifacts\test-evidence\sprint-3/task-2 --logger "trx;LogFileName=test-results.trx"`

Exit code: 0. Total 159; passed 159; failed 0; skipped 0. The generated TRX contained local run-user and workstation metadata, so it was removed and replaced with sanitized aggregate test evidence.

No SQL Server integration test was executed. Task 2 added configuration dependencies only and did not create a DbContext, schema, migration, or database connection.

## 12. Files Created or Modified

Modified:

- `src/SyncBridge.Core.Infrastructure/SyncBridge.Core.Infrastructure.csproj`.
- `src/SyncBridge.Core/SyncBridge.Core.csproj`.
- `src/SyncBridge.Core/appsettings.json`.

Created:

- `docs/sprint-3/Sprint3_Task2_Completion_Report.md`.
- `artifacts/test-evidence/sprint-3/task-2/build-output.txt`.
- `artifacts/test-evidence/sprint-3/task-2/test-output.txt`.
- `artifacts/test-evidence/sprint-3/task-2/package-inventory.txt`.
- `artifacts/test-evidence/sprint-3/task-2/dependency-inventory.txt`.
- `artifacts/test-evidence/sprint-3/task-2/sdk-runtime-inventory.txt`.
- `artifacts/test-evidence/sprint-3/task-2/git-diff-status-summary.txt`.

## 13. Configuration and Security Review

- The tracked connection-string value is empty.
- No server name, instance name, authentication mode, user name, password, token, or credential was added.
- User-secrets support is metadata only; no secret was created or read.
- A targeted secret-pattern scan of modified configuration and project files found no credential-like value.
- The raw evidence omits package caches and removes generated TRX metadata identifying the local run user and workstation.

## 14. Outstanding Issues and Implementation Risks

- An approved SQL Server instance and authentication mode must be supplied outside tracked source before Task 3 runtime registration can be validated.
- `dotnet-ef` tooling remains uninstalled and should be installed only when a separately authorized migration task requires it, with its version aligned to EF Core 10.0.12.
- Task 3 must validate a non-empty connection string before registration and keep `UseSqlServer` inside Infrastructure.
- Later persistence work must enforce all DBDS lengths, nullability, keys, restricted deletes, OQ conversions, and transaction boundaries.

No blocking Task 2 defect remains.

## 15. Task 3 Prerequisites

Before Task 3 implementation begins, the Team must:

1. Accept this Task 2 package and dependency baseline.
2. Authorize Task 3 explicitly.
3. Provide or approve a development connection string through user secrets or an environment variable.
4. Confirm the Task 3 scope for DbContext, persistence rows, and Fluent API configuration.
5. Preserve the accepted OQ decisions and amended DBDS as implementation authority.

## 16. Command and Evidence Register

| Operation | Exit/status | Result |
|---|---:|---|
| Repository root, branch, HEAD, ancestry, status | 0 | Correct branch; accepted Task 1 commit is current HEAD; clean start |
| Project/package/reference inspection | 0 | `net10.0`; approved dependency direction; no Domain/Application packages |
| `dotnet --version`, `--list-sdks`, `--list-runtimes` | 0 | SDK 10.0.401; .NET 10.0.12 runtime available |
| `dotnet ef --version` | 1 | Tool not installed; no installation attempted |
| Initial sandboxed `dotnet restore` | 1 | No package diagnostic; no source change |
| Authorized `dotnet restore` retry | 0 | Infrastructure restored; other projects up to date |
| Initial package inventory with implicit restore | 1 | Blocked by sandboxed restore attempt |
| Package inventory with `--no-restore` | 0 | Both top-level packages resolved at 10.0.12 |
| Full solution build | 0 | 0 errors, 0 warnings |
| Regression tests | 0 | 159 passed, 0 failed, 0 skipped |
| Schema/migration/manufacturing diff checks | 0 | No unauthorized artifact or source change |
| Targeted secret scan | 1 from `rg` | No matching credential pattern; `rg` uses 1 for no match |

Final Git/diff validation is recorded in the raw Git evidence after all deliverables are generated.

## 17. Approved One-Branch-Per-Sprint Git Workflow

All Sprint 3 work remains on `feature/wp6-sprint3-core-domain`. Task-level commits require explicit Team approval. The branch remains local unless the Team authorizes a push. After Sprint 3 acceptance, the Team will authorize merging the completed sprint into `main`; Sprint 4 will then start from updated `main` on a new Sprint 4 feature branch.

Task 2 created no commit and performed no push, merge, branch switch, branch rename, or tag operation. The next action is Team engineering review and acceptance. Sprint 3 Task 3 was not started.
