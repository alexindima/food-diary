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

$cases = foreach ($case in @($draft.cases)) {
    $query = [string]$case.query
    if ([string]::IsNullOrWhiteSpace($query) -or $query -eq '<independent-author-query-required>') {
        throw "Case '$($case.id)' still requires an independently authored query before freezing."
    }
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
    status = 'frozen-independent-query-corpus'
    frozenAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    methodology = 'Targets were selected before authoring. Every query was supplied in the draft before freeze; this tool never derives query text from target paths. Evaluation must occur only after this file is frozen.'
    independence = 'caller-authored; freeze tool does not claim that the author was blind to ranking results'
    sourceDraftSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($draftFullPath))).ToLowerInvariant()
    cases = @($cases)
}
$json = $payload | ConvertTo-Json -Depth 8
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $outputFullPath) -Force
[IO.File]::WriteAllText($outputFullPath, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
$outputHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($outputFullPath))).ToLowerInvariant()
Write-Host "Frozen unseen context corpus: cases=$($cases.Count), sha256=$outputHash, path=$outputFullPath"
