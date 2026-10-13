# Sprint 3 Batch B — SQL Connectivity Diagnostic Report

## 1. Original failure summary

Accelerated Batch B stopped at its mandatory database preflight because a Windows-authenticated `sqlcmd` connection to `localhost` timed out. The failed call returned no server or database metadata. No migration, seed, or database change occurred.

## 2. Actual Codex execution environment

Codex commands run as native x64 Windows processes on the development VM, from `C:\Engineering\GitHub\SyncBridge.Core`, under the approved current Windows identity. `localhost` resolves to the VM's IPv4 and IPv6 loopback addresses.

The ordinary command sandbox is a Windows sandbox with network access disabled. The same command can be executed through an explicitly approved unsandboxed Windows process. This distinction is material: database connectivity fails inside the restricted sandbox and succeeds outside it.

The Windows account name and workstation/server name are intentionally omitted from committed evidence.

## 3. SQL Server service and protocol findings

- The default SQL Server Database Engine service is running with automatic startup.
- SQL Server Browser is stopped and disabled; it is not required for the default local instance.
- Shared Memory is enabled for the default instance.
- The registry reports TCP/IP disabled and Named Pipes disabled.
- The configured TCP static value is 1433, but the server is not accepting connections there.
- Process/listener inspection and filtered SQL startup-log evidence show active loopback listeners on port 1434.
- Explicit TCP to `127.0.0.1:1434` succeeds outside the sandbox; explicit TCP to port 1433 times out.
- Addressing the server as `localhost` outside the sandbox uses Shared Memory and succeeds.

The active 1434 listener was recorded diagnostically. The approved application endpoint remains `localhost`, which selects enabled Shared Memory for local Windows processes. No protocol reconfiguration is required for local Batch B work.

## 4. Commands executed, exit codes, and sanitized results

Read-only operations included:

1. Git branch, HEAD, and status inspection: exit 0; authorized branch and expected HEAD confirmed.
2. Existing Batch B completion report and preflight evidence reads: exit 0.
3. Windows execution context and localhost resolution: exit 0; native VM namespace confirmed.
4. `Win32_Service`, SQL process, SQL protocol registry, and listener inspection: exit 0.
5. Bounded `sqlcmd` connection tests with three-second login timeout: Shared Memory/local aliases succeeded outside the sandbox; TCP 1433 failed; loopback TCP 1434 succeeded.
6. Bounded Microsoft.Data.SqlClient tests: restricted-sandbox attempts failed; approved unsandboxed attempts succeeded.
7. Read-only server metadata query: local default instance and Windows login confirmed, current database `master`, exact `DB_ID(N'SyncBridgeCore')` absent, and create-database permission available.
8. Current-connection transport queries: `localhost` reported Shared Memory; explicit port 1434 reported TCP.
9. Active SSMS transport query: exit 0; no active SSMS session was present.
10. Direct filesystem read of the SQL error log: access denied. A filtered read-only `xp_readerrorlog` query for `Server is listening on` then succeeded and returned only the two local 1434 listener entries.
11. Configuration-presence checks: tracked connection value empty, user-secrets file absent, and session environment variable absent.

Full connection strings, account names, security identifiers, credentials, and unrelated database names are excluded from evidence.

## 5. Connectivity tests and outcomes

| Context | Endpoint | Transport | Outcome |
|---|---|---|---|
| `sqlcmd`, outside sandbox | `lpc:localhost` | Shared Memory | Success |
| `sqlcmd`, outside sandbox | `localhost` | Shared Memory | Success |
| `sqlcmd`, outside sandbox | explicit port 1433 | TCP | Timeout |
| `sqlcmd`, outside sandbox | loopback port 1434 | TCP | Success |
| .NET SqlClient, restricted sandbox | `localhost` | Provider-selected | Failed |
| .NET SqlClient, restricted sandbox | loopback port 1434 | TCP | Access denied |
| .NET SqlClient, outside sandbox | `localhost` | Shared Memory | Success |
| .NET SqlClient, outside sandbox | loopback port 1434 | TCP | Success |

Successful .NET metadata results establish that `SyncBridgeCore` does not currently exist and that the approved Windows identity has the permission required to create it during separately authorized Batch B continuation.

## 6. Identified root cause and remaining hypotheses

**Confirmed root cause:** the original preflight ran in the Codex Windows sandbox whose network access is disabled. That sandbox blocks SQL client access even to Windows loopback/local SQL transports. The identical Windows-authenticated `localhost` connection succeeds from the approved unsandboxed Codex process.

**Contributing configuration fact:** no persistent `ConnectionStrings:SyncBridgeCore` value is presently supplied through tracked configuration, user secrets, or the current environment. This did not cause the original explicit `sqlcmd` timeout, but it must be supplied to the design-time factory before a future EF migration command.

**Not implicated:** Database Engine availability, Windows integrated-authentication identity, database-creation permission, firewall access for the successful local execution path, or an incorrect SQL instance name.

**Bounded uncertainty:** no active SSMS session existed, so its transport could not be observed. The current server configuration supports successful Shared Memory access for local tools; SSMS would normally be expected to use that local transport unless explicitly forced to TCP, but that expectation was not treated as evidence.

## 7. Corrective actions performed

- Re-ran bounded read-only `sqlcmd` and .NET SqlClient checks through the approved unsandboxed Windows execution context.
- Verified `localhost` as a working Shared Memory endpoint.
- Verified the exact target-database absence and create permission.
- Established that future database commands must use the approved unsandboxed execution context and must receive the connection value through session configuration or user secrets.

No persistent system, SQL Server, database, source-code, or security configuration was changed.

## 8. Required Team-approved corrections

No SQL Server service restart, protocol enablement, firewall change, authentication-mode change, login creation, or permission change is required for local Batch B execution.

Before Batch B database work resumes, the Team should approve continuation after this recovery review. The resumed migration invocation must:

1. Run outside the network-restricted Codex sandbox.
2. Supply the already approved `ConnectionStrings__SyncBridgeCore` value at session scope or configure the approved project user-secret key.
3. Repeat the non-destructive target preflight immediately before creation.

Migration application, database creation, and seeding were explicitly outside this recovery prompt and were not performed.

## 9. Successful connection evidence

- `sqlcmd` with Windows authentication connected to the local default instance through `lpc:localhost`, `(local)`, and `localhost`.
- Microsoft.Data.SqlClient with Windows authentication connected from the unsandboxed Codex/.NET context through `localhost`.
- Both confirmed `master` as the current database.
- An exact lookup confirmed that `SyncBridgeCore` is absent.
- A permission check confirmed that the current approved identity can create the authorized database.
- `CONNECTIONPROPERTY('net_transport')` confirmed Shared Memory for `localhost`.

## 10. Legacy database confirmation

The legacy database remained untouched. Its existence, contents, schema, metadata, state, and permissions were not enumerated or inspected. No connection or operation targeted it.

## 11. Remaining Batch B recovery requirements

1. Team review and authorization to resume Batch B.
2. Supply the approved connection value through nontracked configuration.
3. Run the target preflight outside the restricted sandbox.
4. Create/apply the reviewed initial migration only if that repeated preflight remains safe.
5. Verify the deployed 15-table schema through SQL metadata.
6. Execute and verify repeatable fictional seed data.
7. Run the deferred SQL Server integration and rollback tests.

Batch C must not begin without explicit Team authorization.

## 12. Final status

**CONNECTIVITY VERIFIED** — the actual local SQL Server endpoint, Windows-authenticated execution identity, target-database absence, and required create permission were established. The original timeout is explained by the network-restricted Codex sandbox. No unauthorized database, service, protocol, firewall, security, source, or Git operation occurred.
