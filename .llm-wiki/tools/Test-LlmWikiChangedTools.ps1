[CmdletBinding()]
param(
    [string[]]$ChangedPath = @()
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = Split-Path -Parent $wikiRoot
$toolPaths = @(
    $ChangedPath |
        Where-Object { $_ -match '^\.llm-wiki/(?:tools/.+\.ps1|wiki\.ps1)$' } |
        Sort-Object -Unique
)
$issues = [Collections.Generic.List[string]]::new()
foreach ($relativePath in $toolPaths) {
    $absolutePath = Join-Path $repositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        $issues.Add("Changed Wiki tool does not exist: $relativePath")
        continue
    }
    $tokens = $null
    $parseErrors = $null
    $null = [Management.Automation.Language.Parser]::ParseFile($absolutePath, [ref]$tokens, [ref]$parseErrors)
    foreach ($parseError in @($parseErrors)) {
        $issues.Add("${relativePath}:$($parseError.Extent.StartLineNumber): $($parseError.Message)")
    }
}
if ($issues.Count -gt 0) {
    throw "Changed Wiki tool contract failed:`n - $($issues -join "`n - ")"
}
Write-Host "Changed Wiki tool contract passed: $($toolPaths.Count) PowerShell file(s) parsed."
