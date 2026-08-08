[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Stage,
    [hashtable]$Arguments = @{},
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Test-RelevantPath([string]$Path) {
    $normalized = $Path.Replace('\', '/')
    if ($normalized.StartsWith('.git/') -or $normalized.StartsWith('.artifacts/')) { return $false }
    switch ($Stage) {
        'workspace policy' { return $normalized -match '(^|/)AGENTS\.md$|^\.llm-wiki/(policies/workspace|tools/Get-LlmWikiWorkspacePolicy)' }
        'page contracts' { return $normalized -match '^\.llm-wiki/(?!generated/|reviews/).+\.(md|json|ps1)$' }
        'lint regression' { return $normalized -match '^\.llm-wiki/(?!generated/|reviews/)' }
        'indexes' { return $normalized -notmatch '^\.llm-wiki/reviews/' }
        'adaptive verification' { return $normalized -match '^\.llm-wiki/(tools|policies|workflows|evals)/' }
        'failure knowledge' { return $normalized -match '^\.llm-wiki/(known-failures|tools/Manage-LlmWikiFailures)' }
        'change policy' { return $normalized -notmatch '^\.llm-wiki/reviews/source-impact-reviews\.json$' }
        'source impact' { return $normalized -match '^\.llm-wiki/' }
        default { return $true }
    }
}

$material = [Text.StringBuilder]::new()
$null = $material.AppendLine("stage=$Stage")
$null = $material.AppendLine("head=$((& git -C $repositoryRoot rev-parse HEAD).Trim())")
$canonicalArguments = [ordered]@{}
foreach ($key in @($Arguments.Keys | Sort-Object)) { $canonicalArguments[[string]$key] = $Arguments[$key] }
$null = $material.AppendLine("arguments=$(($canonicalArguments | ConvertTo-Json -Depth 8 -Compress))")
foreach ($line in @(& git -C $repositoryRoot status --porcelain=v1 --untracked-files=all)) {
    $path = ([string]$line).Substring(3).Trim('"')
    if ($path -match ' -> ') { $path = ($path -split ' -> ')[-1] }
    $path = $path.Replace('\', '/')
    if (-not (Test-RelevantPath $path)) { continue }
    $null = $material.AppendLine("path=$path")
    $absolutePath = Join-Path $repositoryRoot $path
    if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
        $null = $material.AppendLine("sha=$((Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256).Hash.ToLowerInvariant())")
    } else {
        $null = $material.AppendLine('state=deleted')
    }
}
$selfHash = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
$null = $material.AppendLine("fingerprinter=$selfHash")
$sha = [Security.Cryptography.SHA256]::Create()
try {
    $fingerprint = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($material.ToString()))) -replace '-', '').ToLowerInvariant()
} finally { $sha.Dispose() }

$result = [pscustomobject][ordered]@{ stage = $Stage; fingerprint = $fingerprint }
if ($Format -eq 'Json') { $result | ConvertTo-Json -Compress } else { $fingerprint }
