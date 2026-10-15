$ErrorActionPreference = 'Stop'
$evidence = 'artifacts/test-evidence/sprint-5/batch-b'
$protected = @(
    'artifacts/test-evidence/sprint-4/batch-b/Sql_Preflight_Output.txt',
    'docs/sprint-4/Sprint4_Git_Integration_Completion_Report.md'
)
$paddedHistoricalEvidence = @(
    'artifacts/test-evidence/sprint-5/batch-a/A3_Initial_Checkpoint_Historical.md',
    'artifacts/test-evidence/sprint-5/batch-a/Final_SQL_Postflight.txt',
    'artifacts/test-evidence/sprint-5/batch-a/Security_Patch_Dependency_Chain.log',
    'artifacts/test-evidence/sprint-5/batch-a/Sql_Postflight.txt',
    'artifacts/test-evidence/sprint-5/batch-a/Sql_Preflight_Elevated.txt'
)
# Inventory paths first. Never read protected contents or use recursive git add.
foreach ($name in @('Git_Inclusion_Manifest.json','Git_Include_Paths.txt','Git_Exclusions.json','Security_Scan.json','Architecture_Checks.json','Test_Summary.json','Source_Documents.json','Pending_Source_Hashes.json','Final_Git_State.json')) {
    $path = Join-Path $evidence $name
    if (-not (Test-Path -LiteralPath $path)) { Set-Content -LiteralPath $path -Value '' }
}
$modified = @(git diff --name-only --diff-filter=ACMRT)
$untracked = @(git ls-files --others --exclude-standard)
$all = @(@($modified + $untracked) | Sort-Object -Unique)
$include = @($all | Where-Object { $_ -notin $protected -and $_ -notin $paddedHistoricalEvidence })
$manifest = foreach ($path in $include) {
    $category = switch -Regex ($path) {
        '^src/' { 'Production'; break }
        '^tests/' { 'Tests'; break }
        '^tools/SyncBridge.DevelopmentIdentity/' { 'DevelopmentTool'; break }
        '^docs/sprint-5/' { 'Documentation'; break }
        '^docs/WP6_Sprint4_to_Sprint5_Engineering_Handoff.docx$' { 'AuthoritativeHandoff'; break }
        '^artifacts/test-evidence/sprint-5/' { 'Evidence'; break }
        default { throw "Unexpected pending path: $path" }
    }
    [pscustomobject][ordered]@{Path=$path; GitState=$(if($path -in $modified){'TrackedModified'}else{'Untracked'}); Category=$category; ProposedAction='Include only after final Team Git authorization'}
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'Git_Inclusion_Manifest.json')
$include | Set-Content (Join-Path $evidence 'Git_Include_Paths.txt')
[ordered]@{
    ExactProtectedExclusions=$protected
    PreservedPaddedEvidenceExclusions=$paddedHistoricalEvidence
    EvidenceDerivatives='See Evidence_Sanitization.json for included whitespace-trimmed copies; originals remain unchanged locally.'
    IgnoredExclusions=@('**/bin/','**/obj/','.vs/','TestResults/','*.user','*.suo')
    NeverInclude=@('User Secrets and generated credentials','Data Protection key stores','private keys/certificates','database files','unreviewed/new artifacts after this inventory')
    IntentionalBinaries=@('18 accepted A3 JPG screenshots','authoritative Sprint 4-to-5 DOCX handoff')
} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'Git_Exclusions.json')

$baseline = Get-Content artifacts/test-evidence/sprint-5/batch-a/A3_Production_Baseline.json -Raw | ConvertFrom-Json
$changed = @($baseline | Where-Object { (Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash -ne $_.SHA256 } | ForEach-Object Path)
$allowedProduction = @($baseline.Path) + 'src/SyncBridge.Core.Infrastructure/SyncBridge.Core.Infrastructure.csproj'
$unexpectedProduction = @($manifest | Where-Object { $_.Category -eq 'Production' -and $_.Path -notin $allowedProduction } | ForEach-Object Path)
$a4 = Get-Content artifacts/test-evidence/sprint-5/batch-a/A4_Final_Artifact_Hashes.json -Raw | ConvertFrom-Json
$a4Changed = @($a4 | Where-Object { (Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash -ne $_.SHA256 } | ForEach-Object Path)
$sources = Get-Content artifacts/test-evidence/sprint-5/batch-a/Source_Register.json -Raw | ConvertFrom-Json
$sourceChecks = @($sources | ForEach-Object { [ordered]@{Source=$_.Source; SHA256=(Get-FileHash -LiteralPath $_.Source -Algorithm SHA256).Hash; MatchesAcceptedHash=((Get-FileHash -LiteralPath $_.Source -Algorithm SHA256).Hash -eq $_.SHA256)} })
$sourceChecks | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'Source_Documents.json')
$sourceManifest = @($manifest | Where-Object Category -in @('Production','Tests','DevelopmentTool') | ForEach-Object { [ordered]@{Path=$_.Path; SHA256=(Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash} })
$sourceManifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'Pending_Source_Hashes.json')
$assets = Get-Content src/SyncBridge.Core.Infrastructure/obj/project.assets.json -Raw | ConvertFrom-Json
$references = foreach($path in @(rg --files src tools -g '*.csproj')) {
    [xml]$project = Get-Content -LiteralPath $path -Raw
    [ordered]@{Project=$path; References=@($project.SelectNodes('//ProjectReference') | ForEach-Object { [ordered]@{Include=$_.Include; ReferenceOutputAssembly=$_.ReferenceOutputAssembly} })}
}
$pageReferences = @(rg -n 'DbContext|Microsoft.EntityFrameworkCore|SqlConnection|I\w*Repository' src/SyncBridge.Core/Pages -g '*.cs')
$markers = @('A92919492BE0CEF618C506EEE5217C8244986B2988B97D5C6610218EB0A223CE','A851C5A72CE1670E0245A1122D298B009AA4FEE273F59A50F24D69192CD3E285')
$historical = @('docs/sprint-4/Sprint4_Git_Integration_Completion_Report.md','artifacts/test-evidence/sprint-4/batch-b/Sql_Preflight_Output.txt')
$protectedChecks = for($i=0;$i -lt $historical.Count;$i++) { [ordered]@{Path=$historical[$i]; HashMatches=((Get-FileHash -LiteralPath $historical[$i] -Algorithm SHA256).Hash -eq $markers[$i])} }
$architecture = [ordered]@{AcceptedProductionHashChanges=$changed; UnexpectedProduction=$unexpectedProduction; AcceptedA4HashChanges=$a4Changed; PagePersistenceReferences=$pageReferences; MigrationChanges=@(git diff --name-only -- src/SyncBridge.Core.Infrastructure/Persistence/Migrations); DomainChanges=@(git diff --name-only -- src/SyncBridge.Core.Domain); ResolvedPkcs=@($assets.libraries.PSObject.Properties.Name | Where-Object {$_ -like 'System.Security.Cryptography.Pkcs/*'}); ProjectReferences=@($references); ProtectedHashChecks=@($protectedChecks)}
$architecture | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $evidence 'Architecture_Checks.json')
if ($changed.Count -or $unexpectedProduction.Count -or $a4Changed.Count -or $pageReferences.Count -or @($sourceChecks | Where-Object {-not $_.MatchesAcceptedHash}).Count -or @($protectedChecks | Where-Object {-not $_.HashMatches}).Count) { throw 'Unexpected source/authority/architecture/preservation change.' }

$runs = foreach($name in @('Final_Integrated.trx','Final_SQL_Repeat.trx')) {
    [xml]$trx = Get-Content (Join-Path $evidence $name) -Raw
    $results = @($trx.TestRun.Results.UnitTestResult)
    [ordered]@{Artifact=$name; Total=$results.Count; Passed=@($results | Where-Object outcome -eq Passed).Count; Failed=@($results | Where-Object outcome -eq Failed).Count; Skipped=@($results | Where-Object outcome -eq NotExecuted).Count; LiveSql=@($results | Where-Object testName -match 'SqlServer').Count; DurationSeconds=[Math]::Round(([DateTimeOffset]::Parse($trx.TestRun.Times.finish)-[DateTimeOffset]::Parse($trx.TestRun.Times.start)).TotalSeconds,3)}
}
$runs | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'Test_Summary.json')

# Compare private credential values in memory only. Never emit values or inspect protected transcripts.
[xml]$webProject = Get-Content src/SyncBridge.Core/SyncBridge.Core.csproj -Raw
$secretId = $webProject.Project.PropertyGroup.UserSecretsId
$secretPath = Join-Path $env:APPDATA "Microsoft/UserSecrets/$secretId/secrets.json"
$privateValues = @()
if (Test-Path -LiteralPath $secretPath) {
    $secretSettings = Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json
    $privateValues = @($secretSettings.PSObject.Properties | Where-Object { $_.Name -match 'PasswordHash|^DevelopmentIdentity:Passwords:' } | ForEach-Object Value | Where-Object { $_ -is [string] -and $_.Length -gt 0 })
}
$matches = @(); $patterns = @(); $textCount = 0; $binary = @()
Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach($path in $include) {
    $extension = [IO.Path]::GetExtension($path).ToLowerInvariant()
    if ($extension -eq '.jpg') { $binary += $path; continue }
    if ($extension -eq '.docx') {
        $binary += $path
        $zip = [IO.Compression.ZipFile]::OpenRead((Join-Path (Get-Location) $path))
        try { $reader=[IO.StreamReader]::new($zip.GetEntry('word/document.xml').Open()); try{$content=$reader.ReadToEnd()}finally{$reader.Dispose()} } finally{$zip.Dispose()}
    } elseif($extension -in @('.cs','.cshtml','.csproj','.json','.md','.txt','.log','.trx','.ps1','.css')) { $content=[IO.File]::ReadAllText((Join-Path (Get-Location) $path)) }
    else { throw "Unexpected binary/artifact extension: $path" }
    $textCount++
    $decoded = [regex]::Replace($content,'\\u([0-9a-fA-F]{4})',{param($match) [string][char][Convert]::ToInt32($match.Groups[1].Value,16)})
    foreach($value in $privateValues) { if($content.Contains($value) -or $decoded.Contains($value)) { $matches += $path; break } }
    if($content -match '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{50,}|AQAAAA[A-Za-z0-9+/=]{60,}') { $patterns += $path }
}
$screens = Get-Content artifacts/test-evidence/sprint-5/batch-a/A3_Screenshot_Index.json -Raw | ConvertFrom-Json
$changedScreens = @($screens | Where-Object { (Get-FileHash -LiteralPath (Join-Path artifacts/test-evidence/sprint-5/batch-a $_.Artifact) -Algorithm SHA256).Hash -ne $_.SHA256 } | ForEach-Object Artifact)
[ordered]@{CandidateCount=$include.Count; TextAndDocxChecked=$textCount; PrivateCredentialValuesCompared=$privateValues.Count; CredentialMatches=@($matches|Sort-Object -Unique); HighConfidenceSecretPatternMatches=@($patterns|Sort-Object -Unique); IntentionalBinaries=$binary; AcceptedScreenshotCount=$screens.Count; AcceptedScreenshotHashChanges=$changedScreens; ProtectedContentsExcluded=$protected; Limitations=@('Pattern/known-value scanning is not a formal security audit','Screenshot sanitization uses accepted A3 visual evidence plus unchanged hashes; no new browser capture')} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $evidence 'Security_Scan.json')
if($matches.Count -or $patterns.Count -or $changedScreens.Count) { throw 'Security/evidence preservation concern requires review.' }
$gitState = [ordered]@{Branch=(git branch --show-current); HEAD=(git rev-parse HEAD); Upstream=(git rev-parse --abbrev-ref '@{upstream}'); MainDivergence=(git rev-list --left-right --count main...HEAD); UpstreamDivergence=(git rev-list --left-right --count 'origin/feature/wp6-sprint5-razor-pages...HEAD'); StagedFiles=@(git diff --cached --name-only); PendingCategories=@($manifest | Group-Object Category | Select-Object Name,Count); PendingStates=@($manifest|Group-Object GitState|Select-Object Name,Count); ExactProtectedExclusions=$protected; NoGitIntegrationPerformed=$true}
$gitState | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $evidence 'Final_Git_State.json')
Write-Output "Readiness reconciliation: $($include.Count) proposed paths; $textCount text/DOCX checks; $($screens.Count) accepted screenshots unchanged; zero credential matches."
