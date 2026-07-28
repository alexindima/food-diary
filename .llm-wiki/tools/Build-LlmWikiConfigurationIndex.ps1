[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$outputPath = Join-Path $wikiRoot 'generated/configuration-index.json'

function ConvertTo-RepositoryPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath($Path).Substring($repositoryRoot.Length + 1).Replace('\', '/')
}

function Add-JsonKeys {
    param($Value, [string]$Prefix, [System.Collections.Generic.List[string]]$Keys)
    if ($null -eq $Value) { return }
    foreach ($property in $Value.PSObject.Properties) {
        $key = if ($Prefix) { "$Prefix`:$($property.Name)" } else { $property.Name }
        if ($property.Value -is [System.Management.Automation.PSCustomObject]) {
            Add-JsonKeys $property.Value $key $Keys
        } else {
            $Keys.Add($key)
        }
    }
}

$optionTypes = [System.Collections.Generic.List[object]]::new()
foreach ($file in Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin|tests|TestResults)[\\/]' }) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    foreach ($match in [regex]::Matches(
        $content,
        '(?ms)(?:class|record)\s+(?<type>[A-Za-z_][A-Za-z0-9_]*Options)\b(?<body>.*?)(?=\n(?:public|internal)\s+(?:sealed\s+)?(?:class|record)|\z)')) {
        $body = $match.Groups['body'].Value
        $sectionMatch = [regex]::Match($body, 'SectionName\s*=\s*"(?<section>[^"]+)"')
        $properties = @(
            [regex]::Matches($body, 'public\s+(?:required\s+)?[A-Za-z0-9_?<>,.\[\]\s]+\s+(?<name>[A-Z][A-Za-z0-9_]*)\s*\{') |
                ForEach-Object { $_.Groups['name'].Value } |
                Sort-Object -Unique
        )
        if (-not $sectionMatch.Success -and $properties.Count -eq 0) { continue }
        $optionTypes.Add([pscustomobject]@{
            type = $match.Groups['type'].Value
            section = if ($sectionMatch.Success) { $sectionMatch.Groups['section'].Value } else { $null }
            properties = $properties
            path = ConvertTo-RepositoryPath $file.FullName
        })
    }
}

$configurationFiles = [System.Collections.Generic.List[object]]::new()
foreach ($file in Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter 'appsettings*.json' |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin|TestResults)[\\/]' }) {
    try {
        $json = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        $keys = [System.Collections.Generic.List[string]]::new()
        Add-JsonKeys $json '' $keys
        $configurationFiles.Add([pscustomobject]@{
            path = ConvertTo-RepositoryPath $file.FullName
            keys = @($keys | Sort-Object -Unique)
        })
    } catch {
        throw "Invalid configuration JSON: $(ConvertTo-RepositoryPath $file.FullName)"
    }
}

$environmentFiles = [System.Collections.Generic.List[object]]::new()
foreach ($file in Get-ChildItem -LiteralPath $repositoryRoot -File -Filter '*.env.example') {
    $names = @(
        Get-Content -LiteralPath $file.FullName |
            Where-Object { $_ -match '^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=' } |
            ForEach-Object { ([regex]::Match($_, '^\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=')).Groups['name'].Value } |
            Sort-Object -Unique
    )
    $environmentFiles.Add([pscustomobject]@{
        path = ConvertTo-RepositoryPath $file.FullName
        variables = $names
    })
}

$result = [ordered]@{
    schemaVersion = 1
    summary = [ordered]@{
        optionTypes = $optionTypes.Count
        configurationFiles = $configurationFiles.Count
        configurationKeys = @($configurationFiles.keys | Sort-Object -Unique).Count
        environmentVariables = @($environmentFiles.variables | Sort-Object -Unique).Count
    }
    optionTypes = @($optionTypes | Sort-Object type, path)
    configurationFiles = @($configurationFiles | Sort-Object path)
    environmentFiles = @($environmentFiles | Sort-Object path)
}
$jsonText = ($result | ConvertTo-Json -Depth 10) + [Environment]::NewLine

if ($Check) {
    if (-not (Test-Path -LiteralPath $outputPath) -or (Get-Content -LiteralPath $outputPath -Raw) -ne $jsonText) {
        Write-Host 'Configuration index is stale. Run ./.llm-wiki/wiki.ps1 update.'
        exit 1
    }
    Write-Host "Configuration index is current: $($result.summary.optionTypes) option types, $($result.summary.configurationKeys) keys."
    exit 0
}

$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $jsonText, $utf8WithoutBom)
Write-Host "Generated .llm-wiki/generated/configuration-index.json."
