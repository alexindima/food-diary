[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$outputPath = Join-Path $wikiRoot 'generated/domain-data-index.json'

function ConvertTo-RepositoryPath([string]$Path) {
    [System.IO.Path]::GetFullPath($Path).Substring($repositoryRoot.Length + 1).Replace('\', '/')
}

function Get-Area([string]$Path) {
    if ($Path -match '^MailInbox/') { return 'MailInbox' }
    if ($Path -match '^MailRelay/') { return 'MailRelay' }
    return 'FoodDiary'
}

$domainRoots = @(
    'FoodDiary.Domain',
    'MailInbox/FoodDiary.MailInbox.Domain',
    'MailRelay/FoodDiary.MailRelay.Domain',
    'Shared/FoodDiary.Domain.Primitives'
)
$domainTypes = [System.Collections.Generic.List[object]]::new()
$invariants = [System.Collections.Generic.List[object]]::new()
foreach ($root in $domainRoots) {
    $absoluteRoot = Join-Path $repositoryRoot $root
    if (-not (Test-Path -LiteralPath $absoluteRoot)) { continue }
    foreach ($file in Get-ChildItem -LiteralPath $absoluteRoot -Recurse -File -Filter '*.cs' | Sort-Object FullName) {
        $content = [System.IO.File]::ReadAllText($file.FullName)
        $path = ConvertTo-RepositoryPath $file.FullName
        foreach ($match in [regex]::Matches($content, '(?m)^\s*public\s+(?:(?:sealed|abstract|readonly)\s+)*(?<kind>class|record(?:\s+struct)?|struct|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')) {
            $kind = $match.Groups['kind'].Value
            $role = if ($path -match '/Entities/') { 'entity' } elseif ($path -match '/ValueObjects/') { 'value-object' } elseif ($kind -eq 'enum') { 'enum' } else { 'domain-type' }
            $domainTypes.Add([pscustomobject]@{
                area = Get-Area $path
                name = $match.Groups['name'].Value
                role = $role
                path = $path
                line = 1 + [regex]::Matches($content.Substring(0, $match.Index), "`n").Count
            })
        }
        foreach ($match in [regex]::Matches($content, '(?m)(?:throw\s+new\s+(?<exception>[A-Za-z_][A-Za-z0-9_.<>]*)|(?<guard>Argument(?:Null)?Exception\.ThrowIf[A-Za-z]+)\s*\()[^;\r\n]*(?:["''](?<message>[^"'']{4,})["''])?')) {
            $message = $match.Groups['message'].Value
            if ([string]::IsNullOrWhiteSpace($message)) { $message = $match.Value.Trim() }
            $invariants.Add([pscustomobject]@{
                area = Get-Area $path
                type = if ($match.Groups['exception'].Success) { $match.Groups['exception'].Value } else { $match.Groups['guard'].Value }
                message = $message
                path = $path
                line = 1 + [regex]::Matches($content.Substring(0, $match.Index), "`n").Count
            })
        }
    }
}

$persistence = [System.Collections.Generic.List[object]]::new()
$configurationFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*Configuration.cs' |
        Where-Object {
            $_.FullName -notmatch '[\\/](Migrations|node_modules|bin|obj)[\\/]' -and
            [System.IO.File]::ReadAllText($_.FullName) -match 'IEntityTypeConfiguration|EntityTypeBuilder'
        } |
        Sort-Object FullName
)
foreach ($file in $configurationFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $path = ConvertTo-RepositoryPath $file.FullName
    $entityMatch = [regex]::Match($content, '(?:IEntityTypeConfiguration|EntityTypeBuilder)<(?<entity>[A-Za-z_][A-Za-z0-9_.]*)>')
    if (-not $entityMatch.Success) { continue }
    $tableMatch = [regex]::Match($content, '\.ToTable\(\s*["''](?<table>[^"'']+)["'']')
    $indexes = @(
        [regex]::Matches($content, '\.HasIndex\(\s*(?<expression>[^\r\n;]+)') |
            ForEach-Object { $_.Groups['expression'].Value.Trim() } |
            Sort-Object -Unique
    )
    $relationships = @(
        [regex]::Matches($content, '\.(?<kind>HasOne|HasMany)\s*(?:<(?<target>[^>]+)>)?\s*\(\s*(?<expression>[^\r\n;]*)') |
            ForEach-Object {
                [pscustomobject]@{
                    kind = $_.Groups['kind'].Value
                    target = $_.Groups['target'].Value.Trim()
                    expression = $_.Groups['expression'].Value.Trim()
                }
            }
    )
    $persistence.Add([pscustomobject]@{
        area = Get-Area $path
        entity = $entityMatch.Groups['entity'].Value
        table = if ($tableMatch.Success) { $tableMatch.Groups['table'].Value } else { $null }
        indexes = $indexes
        relationships = $relationships
        path = $path
    })
}

$result = [ordered]@{
    schemaVersion = 1
    summary = [ordered]@{
        domainTypes = $domainTypes.Count
        entities = @($domainTypes | Where-Object role -eq 'entity').Count
        valueObjects = @($domainTypes | Where-Object role -eq 'value-object').Count
        invariants = $invariants.Count
        persistenceMappings = $persistence.Count
        indexes = @($persistence.indexes).Count
        relationships = @($persistence.relationships).Count
    }
    domainTypes = @($domainTypes | Sort-Object area, role, name, path)
    invariants = @($invariants | Sort-Object area, path, line)
    persistenceMappings = @($persistence | Sort-Object area, entity, path)
}
$jsonText = ($result | ConvertTo-Json -Depth 10) + [Environment]::NewLine
if ($Check) {
    if (-not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $jsonText -Depth 10)) {
        Write-Host 'Domain/data index is stale. Run ./.llm-wiki/wiki.ps1 update.'
        exit 1
    }
    Write-Host "Domain/data index is current: $($result.summary.domainTypes) domain types, $($result.summary.persistenceMappings) mappings."
    exit 0
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $jsonText, $utf8WithoutBom)
Write-Host 'Generated .llm-wiki/generated/domain-data-index.json.'
