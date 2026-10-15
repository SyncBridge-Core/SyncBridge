# Sprint 5 Batch A2 HTTPS diagnosis

Date: 2026-10-14. Status: diagnosed; certificate-trust change awaits Team authorization. Interactive UC-001–UC-004 remains unexecuted. No successful walkthrough or screenshots are claimed.

## Actual findings

- Read `Z:\InstallationPackages\Transfer_Folder\Sprint_5\CodexPrompt_Sprint_5_Batch_A2.txt` and preserved the existing Batch A working tree.
- The accepted `https` launch profile uses `https://localhost:7099` plus the existing HTTP redirection listener at `http://localhost:5219`. The previous optional QA experiment used a different port, 5445; neither port 5445 nor 7099 had a listener at the beginning of this diagnosis.
- Sandbox certificate inspection saw no CurrentUser development certificate. Read-only inspection outside the sandbox found the actual user certificate. These are distinct execution contexts; the sandbox observation does not establish that the real user lacks a certificate.
- `dotnet dev-certs https --check --verbose` outside the sandbox passed. Existing certificate: `CN=localhost`, thumbprint `8E8EF86C71A1BF87D7B8DE76477E82D521564C21`, valid from 2026-08-02 17:15:48 UTC to 2027-08-02 17:15:48 UTC, with a private key and the ASP.NET HTTPS development-certificate marker.
- `dotnet dev-certs https --check --trust --verbose` failed because that certificate is not trusted. The combination of `--check` and `--trust` is a read-only trust check, not a trust installation.
- Started the existing host with `dotnet run --project src/SyncBridge.Core/SyncBridge.Core.csproj --no-build --launch-profile https` and the accepted localhost SQL connection configuration. Kestrel started normally and logged its untrusted development certificate. Port 7099 listened on `127.0.0.1` and `::1`; the existing port 5219 listener also started. No application configuration or production code changed.
- A normal `Invoke-WebRequest https://localhost:7099/authentication`, with certificate validation enabled, failed with `UntrustedRoot`.
- In-app browser navigation to the same HTTPS URL failed with `net::ERR_CERT_AUTHORITY_INVALID`. No security-warning bypass was attempted.
- The diagnostic host was stopped after recording the findings. No manufacturing data was created by this host check.

## Concrete proposed resolution and required decision

Run the standard command `dotnet dev-certs https --trust` in the real user context to trust the existing localhost development certificate. This changes the current user's certificate trust store; it does not require regenerating/deleting certificates or changing application HTTPS/cookie/Data Protection settings. The command may present the normal Windows trust confirmation.

The Batch A2 prompt explicitly states: **“Obtain Team authorization before changing system certificate trust.”** Authorization to execute the diagnostic prompt does not remove this specific approval gate. No trust change has been performed. Team authorization is requested for the command and existing certificate identified above.

After authorization, recheck validity/trust, restart an approved HTTPS host with fictional accounts, verify the browser connection, execute UC-001–UC-004 interactively, capture screenshots without credentials/tokens and update the Batch A report with actual outcomes. Passing Batch A HTTP tests are not substituted for this pending browser evidence.

## Evidence and preserved state

Evidence under `artifacts/test-evidence/sprint-5/batch-a/`:

- `A2_HTTPS_Host.log`: actual Kestrel startup/listening addresses and certificate warning.
- `A2_Certificate_Validity.log`: real-user read-only validity check.
- `A2_Certificate_Trust.log`: real-user read-only trust check.
- `A2_HTTPS_Diagnostics.json`: outcome summary, certificate identity and pending authorization.

Batch A's existing verified build and 278 passing tests remain historical evidence from Batch A; they were not rerun for this configuration diagnosis. No production file, certificate, trust store, cookie policy, database schema, or protected Sprint 4 artifact was changed. No staging, commit, merge, push or Batch B work occurred.
