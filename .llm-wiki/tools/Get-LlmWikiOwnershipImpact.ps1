[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [object]$DiffInput,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$graphPath = Join-Path $repositoryRoot 'docs/architecture/module-dependencies.json'

$diffArguments = @{ BaseRef = $BaseRef; Format = 'Json'; Limit = 20 }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $diffArguments.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $diffArguments.ChangedPath = $ChangedPath }
$diff = if ($null -ne $DiffInput) { $DiffInput } else {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1') @diffArguments | ConvertFrom-Json
}
$graph = Get-Content -LiteralPath $graphPath -Raw | ConvertFrom-Json

$directModules = @($diff.modules | ForEach-Object name | Where-Object { $_ -and $graph.modules.PSObject.Properties.Name -contains $_ } | Sort-Object -Unique)
$reverse = @{}
foreach ($moduleProperty in $graph.modules.PSObject.Properties) {
    foreach ($dependency in @($moduleProperty.Value)) {
        if (-not $reverse.ContainsKey($dependency)) {
            $reverse[$dependency] = [System.Collections.Generic.List[string]]::new()
        }
        $reverse[$dependency].Add($moduleProperty.Name)
    }
}

$impacted = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$queue = [System.Collections.Generic.Queue[string]]::new()
foreach ($module in $directModules) {
    $null = $impacted.Add($module)
    $queue.Enqueue($module)
}
while ($queue.Count -gt 0) {
    $current = $queue.Dequeue()
    if (-not $reverse.ContainsKey($current)) { continue }
    foreach ($consumer in $reverse[$current]) {
        if ($impacted.Add($consumer)) { $queue.Enqueue($consumer) }
    }
}

$owners = [System.Collections.Generic.List[object]]::new()
foreach ($path in @($diff.changedPaths)) {
    $segments = $path -split '/'
    $ownerPath = $null
    for ($i = $segments.Count - 1; $i -ge 0; $i--) {
        $candidate = if ($i -eq 0) { 'AGENTS.md' } else { (($segments[0..($i - 1)] -join '/') + '/AGENTS.md') }
        if (Test-Path -LiteralPath (Join-Path $repositoryRoot $candidate)) {
            $ownerPath = $candidate
            break
        }
    }
    if ($null -eq $ownerPath) { $ownerPath = 'AGENTS.md' }
    $owners.Add([pscustomobject]@{ path = $path; guide = $ownerPath })
}

$result = [pscustomobject]@{
    changedPaths = @($diff.changedPaths)
    directModules = $directModules
    transitivelyImpactedModules = @($impacted | Sort-Object)
    downstreamModules = @($impacted | Where-Object { $_ -notin $directModules } | Sort-Object)
    ownershipGuides = @($owners | Sort-Object path)
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 7
    exit 0
}

Write-Host "Direct modules: $($result.directModules -join ', ')"
Write-Host "Downstream modules: $($result.downstreamModules -join ', ')"
Write-Host 'Scoped guides:'
foreach ($group in $result.ownershipGuides | Group-Object guide) {
    Write-Host " - $($group.Name): $($group.Count) changed path(s)"
}
