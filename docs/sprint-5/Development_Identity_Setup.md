# Development identity setup

Scope: local Development only. This setup reuses ManufacturingIdentity, PasswordHasher, current role constants, active AppUser associations, User Secrets and existing SQL persistence. It adds no authentication endpoint, identity database or administration UI.

The Team explicitly chose to retain the two accepted seed identities after inspection confirmed there are not four seeded users. The approved manifest is `tools/SyncBridge.DevelopmentIdentity/ApprovedDevelopmentAccounts.json`:

| Account | Exact configured roles | Permitted manufacturing responsibilities |
|---|---|---|
| demo.operator | Production Operator; Production Supervisor | Order creation/status; batch initiation/verification/execution/signing/deviation entry/resume; batch completion; context/audit/exception reads |
| demo.quality | Quality Engineer | Context/audit/exception reads; electronic step signatures; deviation review/approval/rejection/resolution |

These are the existing live SQL fixture assignments, preserved as additive roles. Neither account is a System Administrator. Standalone Operator/Supervisor separation and Administrator-only denial retain their prior HTTP/service evidence; do not claim this two-account configuration independently demonstrates those role combinations through the browser.

## Prerequisites

- Run from the repository root on the approved local development environment with .NET 10.
- The existing schema and accepted reference seed must already be present. The utility only reads AppUser; it never seeds, inserts or updates database records. UserId and UserName must match an active record exactly. Missing/inactive identities cause setup to fail before any credential write.
- Use an approved local SQL connection through the existing environment or web project's User Secrets. Do not put secrets in source, manifests, shell transcripts or evidence. The manifest contains public fictional IDs and roles only.
- The localhost HTTPS development certificate must be valid and trusted. Use `dotnet dev-certs https --check --trust`; changing trust requires the applicable Team authorization. Do not delete/replace certificates or bypass TLS checks.
- The Team-authorized Infrastructure Pkcs override is 9.0.21. The utility reuses this same dependency through Infrastructure; no separate SQL/identity stack or suppressed vulnerability warning is introduced.

## Configure and verify

Use the existing connection variable, with the actual value provided privately for your environment:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__SyncBridgeCore = '<approved local SQL connection>'
dotnet build tools/SyncBridge.DevelopmentIdentity/SyncBridge.DevelopmentIdentity.csproj
dotnet run --no-build --project tools/SyncBridge.DevelopmentIdentity -- --provision tools/SyncBridge.DevelopmentIdentity/ApprovedDevelopmentAccounts.json
dotnet run --no-build --project tools/SyncBridge.DevelopmentIdentity -- --verify
dotnet run --project src/SyncBridge.Core/SyncBridge.Core.csproj --no-build --launch-profile https
```

Provisioning and verification prompt for each password with input hidden. No password is passed in process arguments or printed. Passwords must be nonempty and within the existing 1024-character boundary. Hashes use the existing registered `IPasswordHasher<ManufacturingAccount>` and are piped privately to the standard `dotnet user-secrets set` command.

For agent-assisted local verification, the optional commands below generate independent random development passwords, place the passwords and hashes only in this web project's User Secrets, and verify them without printing them:

```powershell
dotnet run --no-build --project tools/SyncBridge.DevelopmentIdentity -- --provision tools/SyncBridge.DevelopmentIdentity/ApprovedDevelopmentAccounts.json --generate-passwords
dotnet run --no-build --project tools/SyncBridge.DevelopmentIdentity -- --verify --stored-development-passwords
```

The optional generated password map uses `DevelopmentIdentity:Passwords:<UserId>`; the web authenticator does not read those keys. User Secrets is a local development store, not an encrypted production vault. Do not copy its contents into repository files, screenshots, logs or chat. With manually chosen passwords, use normal hidden-prompt verification rather than a previously generated password map. Use hidden prompts if retaining local plaintext development passwords is undesirable. The optional `--passwords-stdin` accepts a private JSON map for automation; never save that input in evidence or pass it as a command-line argument.

The utility derives the web project's existing UserSecretsId from its project file. It does not create a separate credential store. Configuration remains the accepted appsettings/Development settings/User Secrets/environment ordering. Environment overrides still win; clear unexpected ManufacturingIdentity environment overrides explicitly before setup. Setup refuses a differing existing account roster or roles to avoid leaving stale privileges. Re-running the same approved manifest is supported and replaces its hashes; restart the host and verify again after provisioning. To change the approved roster, obtain the applicable Team decision and explicitly maintain the affected User Secrets keys without clearing unrelated configuration.

Stored structure (values below describe fields; they are not usable secrets):

```text
ManufacturingIdentity:Accounts:0:UserId       = demo.operator
ManufacturingIdentity:Accounts:0:UserName     = demo.operator
ManufacturingIdentity:Accounts:0:PasswordHash = <locally generated hash>
ManufacturingIdentity:Accounts:0:Roles:0      = Production Operator
ManufacturingIdentity:Accounts:0:Roles:1      = Production Supervisor
ManufacturingIdentity:Accounts:1:UserId       = demo.quality
ManufacturingIdentity:Accounts:1:UserName     = demo.quality
ManufacturingIdentity:Accounts:1:PasswordHash = <locally generated hash>
ManufacturingIdentity:Accounts:1:Roles:0      = Quality Engineer
```

`--verify` invokes the existing credential authenticator and trusted actor context, checks the active AppUser association and exact configured role set, and prints only fictional IDs/roles. It does not replace browser authorization tests. Sign in at `https://localhost:7099/authentication`, execute permitted operations, and confirm forbidden operations remain rejected. Quality resolution must leave Exception until explicit Operator resume; completed records must remain read-only. Use the A3 execution report for actual browser results rather than assuming setup alone proves them.

No source or manufacturing database modification is necessary to configure these two accepted accounts in a fresh environment that already has the approved schema/seed. No self-registration, password reset, new role model, account database, certificate export or administration module is provided.
