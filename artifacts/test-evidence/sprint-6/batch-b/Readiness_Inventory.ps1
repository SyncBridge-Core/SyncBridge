param()
$ErrorActionPreference = 'Stop'
# Read-only Git/filesystem inspection. No staging, ref updates or database connections.
$repoRoot = (Resolve-Path '.').Path
$evidence = 'artifacts/test-evidence/sprint-6/batch-b'
$sourcePaths = @(
    'src/SyncBridge.Core/Program.cs',
    'src/SyncBridge.Core/Controllers/ManufacturingController.cs',
    'tests/SyncBridge.Core.Tests/RestApiTests.cs',
    'tests/SyncBridge.Core.Tests/UiApiEquivalenceTests.cs',
    'tests/SyncBridge.Core.Tests/RestBoundaryRegressionTests.cs',
    'tests/SyncBridge.Core.Tests/SecuredServiceFixture.cs',
    'tests/SyncBridge.Core.Tests/SqlServerBatchBIntegrationTests.cs'
)
$generated = @('Git_Include_Paths.txt','Git_Inclusion_Manifest.json','Git_Exclusions.json','Source_Manifest.json',
    'Sensitive_Content_Check.json','Diff_Check.txt','Final_Git_Inventory.txt','Inherited_Preservation.json')
$candidatePaths = @($sourcePaths) + @(
    Get-ChildItem docs/sprint-6,artifacts/test-evidence/sprint-6 -Recurse -File |
        ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1).Replace('\','/') }
) + @($generated | ForEach-Object { "$evidence/$_" })
$candidatePaths = @($candidatePaths | Sort-Object -Unique)
$candidatePaths | Set-Content "$evidence/Git_Include_Paths.txt"
$manifest = foreach ($candidate in $candidatePaths) {
    $tracked = @(git ls-files -- $candidate).Count -gt 0
    [ordered]@{Path=$candidate;Status=$(if($tracked){'tracked modified'}else{'untracked new'});
        Category=$(if($candidate.StartsWith('src/')){'production'}elseif($candidate.StartsWith('tests/')){'test'}elseif($candidate.StartsWith('docs/')){'documentation'}else{'sanitized evidence'});
        ProposedAction='include only after separate Team authorization'}
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content "$evidence/Git_Inclusion_Manifest.json"
$inherited = Get-Content artifacts/test-evidence/sprint-6/batch-a/Inherited_Artifacts.json | ConvertFrom-Json
$preservation = foreach ($original in $inherited) {
    $resolved = (Resolve-Path -LiteralPath $original.Path).Path
    if (!$resolved.StartsWith($repoRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Inherited path outside repository.' }
    $relative = $resolved.Substring($repoRoot.Length + 1).Replace('\','/')
    if ($candidatePaths -contains $relative) { throw 'Protected inherited path entered staging proposal.' }
    [ordered]@{Path=$relative;SHA256=(Get-FileHash -LiteralPath $resolved).Hash;
        MatchesBatchA=((Get-FileHash -LiteralPath $resolved).Hash -eq $original.Hash)}
}
$preservation | ConvertTo-Json | Set-Content "$evidence/Inherited_Preservation.json"
if (@($preservation | Where-Object { !$_.MatchesBatchA }).Count) { throw 'Inherited artifact hash mismatch.' }
@($preservation | ForEach-Object { [ordered]@{Path=$_.Path;Reason='protected inherited original; preserve, exclude'} }) + @(
    [ordered]@{Path='ignored build/user outputs and all private credentials, keys, certificates and databases';Reason='outside approved inclusion inventory'}
) | ConvertTo-Json | Set-Content "$evidence/Git_Exclusions.json"
$sourcePaths | ForEach-Object { [ordered]@{Path=$_;SHA256=(Get-FileHash -LiteralPath $_).Hash} } |
    ConvertTo-Json | Set-Content "$evidence/Source_Manifest.json"
$findings = foreach ($candidate in $candidatePaths) {
    if (Test-Path -LiteralPath $candidate) {
        $matches = Select-String -LiteralPath $candidate -Pattern 'CfDJ8[A-Za-z0-9_-]{60,}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----|(?:PasswordHash|AccountKey|SharedAccessKey)\s*[:=]\s*["''][A-Za-z0-9+/=]{40,}'
        if ($matches) { [ordered]@{Path=$candidate;Count=@($matches).Count} }
    }
}
[ordered]@{ScannedCandidateFiles=$candidatePaths.Count;HighConfidenceFindings=@($findings);
    Bounds='Pattern scan of explicit candidates; fictional test constants permitted; not a formal security audit.'} |
    ConvertTo-Json -Depth 5 | Set-Content "$evidence/Sensitive_Content_Check.json"
if (@($findings).Count) { throw 'Sensitive-pattern candidate requires review.' }
$diffMessages = @(git diff --check 2>&1)
$diffCode = $LASTEXITCODE
@("git diff --check exit: $diffCode") + $diffMessages | Set-Content "$evidence/Diff_Check.txt"
if ($diffCode -ne 0) { throw 'Tracked diff check failed.' }
git status --short --ignored | Set-Content "$evidence/Final_Git_Inventory.txt"
if (@(git diff --cached --name-only).Count) { throw 'Index unexpectedly nonempty.' }
[ordered]@{CandidateFiles=$candidatePaths.Count;InheritedPreserved=@($preservation).Count;StagedFiles=0;DiffCheckExit=$diffCode} | ConvertTo-Json
