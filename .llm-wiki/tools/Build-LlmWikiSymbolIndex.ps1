[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$outputPath = Join-Path $wikiRoot 'generated/csharp-symbol-index.json'

function ConvertTo-RepositoryPath {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new($resolvedPath)
    return [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pathUri).ToString())
}

function Get-SymbolRole {
    param(
        [string]$Name,
        [string]$Path,
        [string]$Kind
    )

    $nameWithoutInterfacePrefix = if ($Kind -eq 'interface' -and $Name.StartsWith('I')) {
        $Name.Substring(1)
    } else {
        $Name
    }
    $roles = @(
        'CommandHandler'
        'QueryHandler'
        'Handler'
        'Validator'
        'ReadModelRepository'
        'LookupRepository'
        'ReadRepository'
        'WriteRepository'
        'Repository'
        'Controller'
        'Service'
        'Factory'
        'Mapper'
        'Mapping'
        'Options'
        'Configuration'
        'Entity'
        'ValueObject'
        'Event'
        'Command'
        'Query'
        'Request'
        'Response'
    )
    foreach ($role in $roles) {
        if ($nameWithoutInterfacePrefix.EndsWith($role, [System.StringComparison]::Ordinal)) {
            return $role
        }
    }
    if ($Path -match '/Entities/') {
        return 'Entity'
    }
    if ($Path -match '/ValueObjects?/' -or $Path -match '/StronglyTypedIds?') {
        return 'ValueObject'
    }
    return 'Other'
}

$sourceFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.cs' |
        Where-Object {
            $_.FullName -notmatch '[\\/](tests|node_modules|obj|bin|\.artifacts|TestResults|Migrations)[\\/]' -and
            $_.Name -notmatch '\.(Designer|g)\.cs$' -and
            $_.Name -notmatch 'ModelSnapshot\.cs$'
        } |
        Sort-Object FullName
)

$typePattern = '(?m)^\s*(?:public|internal)\s+(?:(?:sealed|abstract|static|partial|readonly)\s+)*(?<kind>class|interface|record(?:\s+struct)?|struct|enum)\s+(?<name>[A-Za-z_]\w*)'
$symbols = [System.Collections.Generic.List[object]]::new()
foreach ($sourceFile in $sourceFiles) {
    $content = Get-Content -LiteralPath $sourceFile.FullName -Raw
    $path = ConvertTo-RepositoryPath $sourceFile.FullName
    foreach ($match in [regex]::Matches($content, $typePattern)) {
        $kind = $match.Groups['kind'].Value
        $name = $match.Groups['name'].Value
        $line = ($content.Substring(0, $match.Index) -split "`n").Count
        $symbols.Add([pscustomobject][ordered]@{
            name = $name
            kind = $kind
            role = Get-SymbolRole $name $path $kind
            path = $path
            line = $line
        })
    }
}

$symbolNames = @{}
foreach ($symbol in $symbols) {
    if (-not $symbolNames.ContainsKey($symbol.name)) {
        $symbolNames[$symbol.name] = [System.Collections.Generic.List[object]]::new()
    }
    $symbolNames[$symbol.name].Add($symbol)
}

$interfaceImplementations = [System.Collections.Generic.List[object]]::new()
foreach ($symbol in $symbols | Where-Object { $_.kind -eq 'interface' -and $_.name -match '^I[A-Z]' }) {
    $implementationName = $symbol.name.Substring(1)
    if ($symbolNames.ContainsKey($implementationName)) {
        foreach ($implementation in $symbolNames[$implementationName]) {
            if ($implementation.kind -eq 'interface') {
                continue
            }
            $interfaceImplementations.Add([pscustomobject][ordered]@{
                interface = $symbol.name
                interfacePath = $symbol.path
                implementation = $implementation.name
                implementationPath = $implementation.path
                confidence = 'naming-convention'
            })
        }
    }
}

$registrationFiles = @(
    $sourceFiles | Where-Object {
        $_.Name -like '*DependencyInjection*.cs' -or
        $_.Name -eq 'Program.cs' -or
        $_.Name -like '*ServiceCollectionExtensions.cs'
    }
)
$registrations = [System.Collections.Generic.List[object]]::new()
$registrationPattern = '(?:Try)?Add(?<lifetime>Scoped|Transient|Singleton)\s*<\s*(?<service>[\w.]+)\s*,\s*(?<implementation>[\w.]+)\s*>'
foreach ($registrationFile in $registrationFiles) {
    $content = Get-Content -LiteralPath $registrationFile.FullName -Raw
    $path = ConvertTo-RepositoryPath $registrationFile.FullName
    foreach ($match in [regex]::Matches($content, $registrationPattern)) {
        $line = ($content.Substring(0, $match.Index) -split "`n").Count
        $registrations.Add([pscustomobject][ordered]@{
            service = $match.Groups['service'].Value
            implementation = $match.Groups['implementation'].Value
            lifetime = $match.Groups['lifetime'].Value
            path = $path
            line = $line
        })
    }
}

$roleCounts = [ordered]@{}
foreach ($roleGroup in @($symbols | Group-Object role | Sort-Object Name)) {
    $roleCounts[$roleGroup.Name] = $roleGroup.Count
}
$kindCounts = [ordered]@{}
foreach ($kindGroup in @($symbols | Group-Object kind | Sort-Object Name)) {
    $kindCounts[$kindGroup.Name] = $kindGroup.Count
}

$index = [ordered]@{
    schemaVersion = 1
    generator = '.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1'
    extraction = [ordered]@{
        scope = 'Production C# source excluding tests, migrations, generated files, obj, and bin'
        symbols = 'Public and internal top-level type declarations'
        interfaceImplementations = 'IName to Name naming convention'
        registrations = 'Literal AddScoped/AddTransient/AddSingleton service-to-implementation registrations'
    }
    summary = [ordered]@{
        sourceFiles = $sourceFiles.Count
        symbols = $symbols.Count
        interfaceImplementationLinks = $interfaceImplementations.Count
        dependencyInjectionRegistrations = $registrations.Count
        kinds = $kindCounts
        roles = $roleCounts
    }
    symbols = @($symbols | Sort-Object path, line, name)
    interfaceImplementations = @($interfaceImplementations | Sort-Object interface, implementationPath)
    dependencyInjectionRegistrations = @($registrations | Sort-Object path, line, service)
}

$json = $index | ConvertTo-Json -Depth 12
$expectedContent = $json + [Environment]::NewLine

if ($Check) {
    if (-not (Test-Path -LiteralPath $outputPath)) {
        Write-Host 'C# symbol index is missing. Run Build-LlmWikiSymbolIndex.ps1.'
        exit 1
    }
    if (-not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $expectedContent -Depth 12)) {
        Write-Host 'C# symbol index is stale. Regenerate it with:'
        Write-Host '  ./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1'
        exit 1
    }
    Write-Host "C# symbol index is current: $($symbols.Count) symbols, $($registrations.Count) DI registrations."
    return
}

$outputDirectory = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $expectedContent, $utf8WithoutBom)
Write-Host "Generated $(ConvertTo-RepositoryPath $outputPath)."
