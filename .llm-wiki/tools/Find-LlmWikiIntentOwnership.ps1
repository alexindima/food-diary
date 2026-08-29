[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Query,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite',
    [string]$SearchFixturePath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$search = if (-not [string]::IsNullOrWhiteSpace($SearchFixturePath)) {
    Get-Content -LiteralPath $SearchFixturePath -Raw | ConvertFrom-Json
} elseif ($CompiledIndexSource -eq 'Sqlite') {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') `
        -Action search -Query $Query -Limit ([Math]::Min(50, [Math]::Max(20, $Limit * 3))) -SkipRefresh -Format Json | ConvertFrom-Json
} else {
    & (Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1') `
        -Query $Query -CompiledIndexSource Json -SkipQueryCache -Limit ([Math]::Min(50, [Math]::Max(20, $Limit * 3))) -Format Json | ConvertFrom-Json
}

function Get-OptionalPropertyValue([object]$InputObject, [string]$Name, [object]$DefaultValue = $null) {
    if ($null -eq $InputObject) { return $DefaultValue }
    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -eq $property -or $null -eq $property.Value) { return $DefaultValue }
    return $property.Value
}

function Resolve-ModuleFromPath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }
    $normalized = $Path.Replace('\', '/')
    if ($normalized -match '^Modules/([^/]+)(?:/|$)') { return $Matches[1] }
    if ($normalized -match '^FoodDiary\.Application\.([^.\/]+)(?:/|$)') { return $Matches[1] }
    if ($normalized -match '^Shared/([^/]+)(?:/|$)') { return $Matches[1] }
    if ($normalized -match '^((?:FoodDiary|MailInbox|MailRelay)[^/]*)(?:/|$)') { return $Matches[1] }
    return $null
}

function ConvertTo-NormalizedOwnershipRecord([object]$Record) {
    $normalized = [ordered]@{}
    foreach ($property in @($Record.PSObject.Properties)) { $normalized[$property.Name] = $property.Value }

    $path = ([string](Get-OptionalPropertyValue $Record 'path' '')).Replace('\', '/')
    $module = [string](Get-OptionalPropertyValue $Record 'module' '')
    if ([string]::IsNullOrWhiteSpace($module)) { $module = Resolve-ModuleFromPath $path }
    if ($module -match '^FoodDiary\.(?:Application|Modules)\.([^.]+)$') { $module = $Matches[1] }

    $normalized['path'] = $path
    $normalized['module'] = $module
    $normalized['score'] = [double](Get-OptionalPropertyValue $Record 'score' 0)
    $normalized['confidence'] = [string](Get-OptionalPropertyValue $Record 'confidence' 'low')
    $normalized['reasons'] = @(Get-OptionalPropertyValue $Record 'reasons' @())
    return [pscustomobject]$normalized
}

function Get-ExplicitModuleOwners([string]$Intent) {
    $inventoryPath = Join-Path $repositoryRoot 'docs/architecture/backend-modules.json'
    if (-not (Test-Path -LiteralPath $inventoryPath -PathType Leaf)) { return @() }

    $inventory = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json
    $normalizedIntent = ($Intent -replace '[^\p{L}\p{Nd}]+', ' ').Trim().ToLowerInvariant()
    $matches = @($inventory.modules.PSObject.Properties | Where-Object {
        $moduleName = $_.Name.ToLowerInvariant()
        $normalizedIntent -eq $moduleName -or $normalizedIntent -match "(^| )$([regex]::Escape($moduleName))( |$)"
    })
    if ($matches.Count -ne 1) { return @() }

    $moduleName = [string]$matches[0].Name
    $mapping = $matches[0].Value.sourceMappings
    $logicalRoot = [string](Get-OptionalPropertyValue $mapping 'logicalRoot' '')
    $applicationProjects = @(Get-OptionalPropertyValue $mapping 'applicationProjects' @())
    $path = if (-not [string]::IsNullOrWhiteSpace($logicalRoot)) {
        $logicalRoot.Replace('\', '/')
    } elseif ($applicationProjects.Count -gt 0) {
        ([string]$applicationProjects[0]).Replace('\', '/')
    } elseif (Test-Path -LiteralPath (Join-Path $repositoryRoot "Modules/$moduleName")) {
        "Modules/$moduleName"
    } else {
        "FoodDiary.Application.$moduleName"
    }
    $guide = "$path/AGENTS.md"
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $guide) -PathType Leaf)) { $guide = 'AGENTS.md' }

    return @([pscustomobject][ordered]@{
        path = $path
        guide = $guide
        module = $moduleName
        score = 1000.0
        confidence = 'high'
        reasons = @('exact backend business-module inventory match')
    })
}

$rawRecords = if ($CompiledIndexSource -eq 'Sqlite') { @($search.records) } else { @($search.candidates) }
$records = @($rawRecords | ForEach-Object { ConvertTo-NormalizedOwnershipRecord $_ })
$explicitOwners = @(Get-ExplicitModuleOwners $Query)
$ranking = if ($CompiledIndexSource -eq 'Sqlite') { $search.rankingSummary } else { $null }
$confidence = if ($explicitOwners.Count -eq 1) {
    'high'
} elseif ($CompiledIndexSource -eq 'Sqlite') {
    if ($null -eq $ranking) { 'low' } else { [string](Get-OptionalPropertyValue $ranking 'confidence' 'low') }
} else { [string](Get-OptionalPropertyValue $search 'confidence' 'low') }
$ambiguous = if ($CompiledIndexSource -eq 'Sqlite') {
    if ($null -eq $ranking) { $true } else { [bool](Get-OptionalPropertyValue $ranking 'ambiguous' $true) }
} else { -not [bool](Get-OptionalPropertyValue $search 'conclusive' $false) }
$conclusive = $explicitOwners.Count -eq 1 -or ($records.Count -gt 0 -and $confidence -in @('high', 'medium') -and -not $ambiguous)
$selected = if ($explicitOwners.Count -eq 1) {
    @()
} elseif ($conclusive) {
    @($records | Where-Object { [string]$_.confidence -in @('high', 'medium') } | Select-Object -First $Limit)
} else { @() }

$owners = [Collections.Generic.List[object]]::new()
foreach ($owner in $explicitOwners) { $owners.Add($owner) }
foreach ($record in $selected) {
    $path = ([string]$record.path).Replace('\', '/')
    $segments = $path -split '/'
    $guide = 'AGENTS.md'
    for ($index = $segments.Count - 1; $index -ge 1; $index--) {
        $candidate = (($segments[0..($index - 1)] -join '/') + '/AGENTS.md')
        if (Test-Path -LiteralPath (Join-Path $repositoryRoot $candidate) -PathType Leaf) { $guide = $candidate; break }
    }
    $owners.Add([pscustomobject][ordered]@{
        path = $path
        guide = $guide
        module = [string](Get-OptionalPropertyValue $record 'module' '')
        score = [double](Get-OptionalPropertyValue $record 'score' 0)
        confidence = [string](Get-OptionalPropertyValue $record 'confidence' 'low')
        reasons = @(Get-OptionalPropertyValue $record 'reasons' @())
    })
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    query = $Query
    selectionSource = $(if ($explicitOwners.Count -eq 1) { 'backend-module-inventory' } elseif ($conclusive) { 'compiled-index' } else { 'abstained' })
    confidence = $confidence
    conclusive = $conclusive
    abstained = -not $conclusive
    abstentionReason = $(if ($conclusive) { $null } elseif ($records.Count -eq 0) { 'no-indexed-candidates' } elseif ($ambiguous) { $(if ($CompiledIndexSource -eq 'Sqlite') { [string](Get-OptionalPropertyValue $ranking 'ambiguityReason' 'ambiguous-candidates') } else { [string](Get-OptionalPropertyValue $search 'ambiguityReason' 'ambiguous-candidates') }) } else { 'low-confidence' })
    directModules = @($owners | ForEach-Object { $_.module } | Where-Object { $_ } | Sort-Object -Unique)
    transitivelyImpactedModules = @()
    downstreamModules = @()
    ownershipGuides = @($owners)
    candidates = @($records | Select-Object -First $Limit)
    index = $(if ($CompiledIndexSource -eq 'Sqlite') {
        [pscustomobject][ordered]@{
            source = 'sqlite'
            fingerprint = Get-OptionalPropertyValue $search 'fingerprint'
            updatedAtUtc = Get-OptionalPropertyValue $search 'updatedAtUtc'
            durationMs = Get-OptionalPropertyValue $search 'durationMs' 0
        }
    } else {
        [pscustomobject][ordered]@{ source = 'json-baseline'; compiledIndex = $search.compiledIndex }
    })
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; return }
Write-Host "Intent ownership: confidence=$confidence; conclusive=$conclusive; candidates=$($records.Count)."
if (-not $conclusive) { Write-Host "Abstained: $($result.abstentionReason). Narrow the intent or provide -ChangedPath."; return }
foreach ($owner in $owners) { Write-Host " - $($owner.guide): $($owner.path) [$($owner.confidence)]" }
