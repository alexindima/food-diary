[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('catalog', 'symbols', 'frontend', 'frontend-contract', 'backend-contract', 'architecture-health', 'domain-data', 'configuration', 'quality', 'runtime', 'sensitive-data', 'modules')]
    [string]$Index,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$generatedRoot = Join-Path $wikiRoot 'generated'
$sourceNames = @{
    catalog = 'repository-catalog.json'
    symbols = 'csharp-symbol-index.json'
    frontend = 'frontend-index.json'
    'frontend-contract' = 'frontend-contract-index.json'
    'backend-contract' = 'backend-contract-index.json'
    'architecture-health' = 'architecture-health-index.json'
    'domain-data' = 'domain-data-index.json'
    configuration = 'configuration-index.json'
    quality = 'quality-index.json'
    runtime = 'runtime-topology.json'
    'sensitive-data' = 'sensitive-data-index.json'
}

function ConvertTo-RepositoryPath([string]$Path) {
    return [IO.Path]::GetRelativePath((Resolve-Path (Join-Path $wikiRoot '..')).Path, $Path).Replace('\', '/')
}

function Get-CollectionSections([object]$Value, [string]$Prefix, [int]$Depth) {
    $sections = [Collections.Generic.List[object]]::new()
    if ($null -eq $Value -or $Depth -gt 2) { return @($sections) }
    foreach ($property in $Value.PSObject.Properties) {
        $name = if ([string]::IsNullOrWhiteSpace($Prefix)) { $property.Name } else { "$Prefix.$($property.Name)" }
        $propertyValue = $property.Value
        if ($propertyValue -is [Array]) {
            $items = @($propertyValue)
            $sections.Add([pscustomobject][ordered]@{
                name = $name
                count = $items.Count
                items = @($items | Select-Object -First $Limit)
            })
        } elseif ($null -ne $propertyValue -and $propertyValue -isnot [string] -and @($propertyValue.PSObject.Properties).Count -gt 0) {
            foreach ($section in @(Get-CollectionSections $propertyValue $name ($Depth + 1))) { $sections.Add($section) }
        }
    }
    return @($sections)
}

if ($Index -eq 'modules') {
    $moduleRoot = Join-Path $generatedRoot 'modules'
    if (-not (Test-Path -LiteralPath $moduleRoot -PathType Container)) { throw 'Compiled module pages are missing. Run wiki.ps1 update.' }
    $files = @(Get-ChildItem -LiteralPath $moduleRoot -File | Sort-Object Name)
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        index = $Index
        sourcePath = ConvertTo-RepositoryPath $moduleRoot
        readOnly = $true
        freshness = 'not-verified; use -Check or wiki.ps1 verify'
        count = $files.Count
        items = @($files | Select-Object -First $Limit | ForEach-Object { ConvertTo-RepositoryPath $_.FullName })
    }
} else {
    $sourcePath = Join-Path $generatedRoot $sourceNames[$Index]
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { throw "Compiled index '$Index' is missing. Run wiki.ps1 update." }
    $data = Get-Content -LiteralPath $sourcePath -Raw | ConvertFrom-Json
    $summaryProperty = $data.PSObject.Properties['summary']
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        index = $Index
        sourcePath = ConvertTo-RepositoryPath $sourcePath
        readOnly = $true
        freshness = 'not-verified; use -Check or wiki.ps1 verify'
        generatedAtUtc = $(if ($data.PSObject.Properties['generatedAtUtc']) { $data.generatedAtUtc } else { $null })
        summary = $(if ($null -ne $summaryProperty) { $summaryProperty.Value } else { $null })
        sections = @(Get-CollectionSections $data '' 0)
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 20; return }
Write-Host "Compiled index '$Index' (read-only): $($result.sourcePath)"
Write-Host "Freshness: $($result.freshness)"
if ($Index -eq 'modules') {
    foreach ($item in @($result.items)) { Write-Host " - $item" }
} else {
    foreach ($section in @($result.sections)) { Write-Host " - $($section.name): $($section.count) item(s), showing $(@($section.items).Count)" }
}
