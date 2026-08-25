[CmdletBinding()]
param(
    [string]$DraftPath = '.artifacts/llm-wiki/evals/context-search-unseen-draft.json',
    [string]$OutputPath = '.artifacts/llm-wiki/evals/context-search-unseen-frozen.json',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
function Resolve-RepoPath([string]$Path) {
    if ([IO.Path]::IsPathRooted($Path)) { return [IO.Path]::GetFullPath($Path) }
    return [IO.Path]::GetFullPath((Join-Path $repositoryRoot $Path))
}

$draftFullPath = Resolve-RepoPath $DraftPath
$outputFullPath = Resolve-RepoPath $OutputPath
if ((Test-Path -LiteralPath $outputFullPath) -and -not $Force) {
    throw "Frozen corpus already exists at '$outputFullPath'. Use -Force to replace it intentionally."
}
$draft = [IO.File]::ReadAllText($draftFullPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
if ($draft.status -ne 'draft-unseen-not-executable') { throw "Unexpected draft status '$($draft.status)'." }

$stopWords = @('food','diary','src','app','lib','features','feature','projects','tests','test','application','infrastructure','presentation','domain','tools','llm','wiki','cs','ts','ps1','models','services','configurations','entities','value','objects','pages')
$aliases = @{
    ai='artificial intelligence'; jwt='token authentication'; smtp='mail transport'; mx='mail exchange'
    api='http endpoint'; aws='cloud notification'; ses='email provider'; sns='webhook notification'
    id='identifier'; ids='identifiers'; ui='interface'; wht='waist height ratio'; usda='nutrition database'
    pdf='document export'; redis='idempotency cache'; rabbit='message broker'; mq='message queue'; spec='tests'
    fitbit='wearable provider'; yoo='payment provider'; kasa='checkout payment'
}
$cohortPrefixes = @{
    'application-api'='implementation contract for'
    'behavior-to-test'='automated checks for'
    'domain-invariants'='domain rule for'
    'frontend'='client interface for'
    'integrations-persistence'='storage or provider implementation for'
    'wiki-tooling'='developer knowledge tooling for'
}

$cases = foreach ($case in @($draft.cases)) {
    $target = [string]$case.expectedPaths[0]
    $leaf = [IO.Path]::GetFileNameWithoutExtension($target)
    $segments = [regex]::Matches($leaf, '[A-Z]+(?=[A-Z][a-z]|\d|$)|[A-Z]?[a-z]+|\d+') | ForEach-Object Value
    $terms = foreach ($segment in $segments) {
        $lower = $segment.ToLowerInvariant()
        if ($lower -in $stopWords) { continue }
        if ($aliases.ContainsKey($lower)) { $aliases[$lower] } else { $lower }
    }
    $parent = Split-Path (Split-Path $target -Parent) -Leaf
    $parentTerms = [regex]::Matches($parent, '[A-Z]+(?=[A-Z][a-z]|\d|$)|[A-Z]?[a-z]+|\d+') |
        ForEach-Object Value | ForEach-Object { $_.ToLowerInvariant() } |
        Where-Object { $_ -notin $stopWords -and $_ -notin $terms }
    $semanticTerms = @(@($parentTerms) + @($terms) | Select-Object -Unique)
    if ($semanticTerms.Count -eq 0) { throw "Could not derive authoring terms for '$target'." }
    $query = "$($cohortPrefixes[[string]$case.cohort]) $($semanticTerms -join ' ')"
    [pscustomobject][ordered]@{
        id = [string]$case.id
        cohort = [string]$case.cohort
        query = $query
        changeType = [string]$case.changeType
        expectedPaths = @($case.expectedPaths)
    }
}

$duplicateQueries = @($cases | Group-Object query | Where-Object Count -gt 1)
if ($duplicateQueries.Count -gt 0) { throw "Authoring produced $($duplicateQueries.Count) duplicate query group(s)." }
$payload = [pscustomobject][ordered]@{
    schemaVersion = 1
    status = 'frozen-unseen-blind-to-results'
    frozenAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    methodology = 'Queries were deterministically authored from target semantics and cohort intent without invoking context search or reading ranking policy. Evaluation must occur only after this file is frozen.'
    independence = 'blind-to-results; target-aware; not independently human-authored'
    sourceDraftSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($draftFullPath))).ToLowerInvariant()
    cases = @($cases)
}
$json = $payload | ConvertTo-Json -Depth 8
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $outputFullPath) -Force
[IO.File]::WriteAllText($outputFullPath, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
$outputHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($outputFullPath))).ToLowerInvariant()
Write-Host "Frozen unseen context corpus: cases=$($cases.Count), sha256=$outputHash, path=$outputFullPath"
