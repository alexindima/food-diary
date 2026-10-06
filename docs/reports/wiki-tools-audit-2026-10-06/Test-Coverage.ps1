[CmdletBinding()]
param([switch]$FailOnIncomplete)
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$inventory = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inventory.json') -Raw | ConvertFrom-Json
$ledger = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'reviews.json') -Raw | ConvertFrom-Json
$paths = @(& git -C $repositoryRoot ls-files --cached --others --exclude-standard -- $inventory.scopeRoots |
    Where-Object { [IO.Path]::GetExtension($_) -in @('.ps1','.cs','.csproj','.mjs','.cmd','.yml') } | Sort-Object -Unique)
if ($LASTEXITCODE -ne 0) { throw 'Unable to enumerate current audit sources.' }
$registeredPaths = @($inventory.files.path | Sort-Object -Unique)
if (@(Compare-Object $paths $registeredPaths).Count -ne 0 -or $registeredPaths.Count -ne $inventory.files.Count) {
    throw 'Inventory scope is stale or contains duplicate entries; rebuild it before claiming coverage.'
}
$reviewed = @{}
foreach ($review in $ledger.reviews) {
    if ($reviewed.ContainsKey($review.path) -or $review.path -notin $registeredPaths) { throw "Invalid or duplicate review path: $($review.path)" }
    foreach ($property in @('performance','reliability','sourceSha256','coverage')) {
        if ([string]::IsNullOrWhiteSpace([string]$review.$property)) { throw "Missing $property for $($review.path)" }
    }
    if ($review.coverage -cne 'full-source-read') { throw "Review does not cover the complete source: $($review.path)" }
    $content = [IO.File]::ReadAllText((Join-Path $repositoryRoot $review.path)).Replace("`r`n", "`n").Replace("`r", "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $currentHash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($content))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
    if ($currentHash -cne $review.sourceSha256) { throw "Source changed after review: $($review.path)" }
    $reviewed[$review.path] = $true
}
$pending = @($registeredPaths | Where-Object { -not $reviewed.ContainsKey($_) })
Write-Output ([pscustomobject]@{total=$registeredPaths.Count; reviewed=$reviewed.Count; pending=$pending.Count; complete=($pending.Count -eq 0)} | ConvertTo-Json -Compress)
if ($FailOnIncomplete -and $pending.Count -gt 0) { throw "Audit incomplete: $($pending.Count) source files still need complete code review." }
