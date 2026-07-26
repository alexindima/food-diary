[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$frontendRoot = Join-Path $repositoryRoot 'FoodDiary.Web.Client'
$outputPath = Join-Path $wikiRoot 'generated/frontend-index.json'

function ConvertTo-RepositoryPath {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new($resolvedPath)
    return [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pathUri).ToString())
}

function Get-FrontendRole {
    param(
        [string]$Name,
        [string]$Path,
        [string]$Content
    )

    if ($Content -match '@Component\s*\(') { return 'Component' }
    if ($Content -match '@Directive\s*\(') { return 'Directive' }
    if ($Content -match '@Pipe\s*\(') { return 'Pipe' }
    if ($Name -match 'Resolver$') { return 'Resolver' }
    if ($Name -match 'Guard$') { return 'Guard' }
    if ($Name -match 'Facade$') { return 'Facade' }
    if ($Name -match '(Api|Client)$') { return 'ApiClient' }
    if ($Name -match 'Service$' -or $Content -match '@(?:Injectable|Service)\s*\(') { return 'Service' }
    if ($Path -match '\.routes\.ts$') { return 'Routes' }
    if ($Name -match 'Model$') { return 'Model' }
    return 'Other'
}

function Get-JsonPropertyCount {
    param([string]$Content)

    return [regex]::Matches($Content, '"(?:\\.|[^"\\])*"\s*:').Count
}

$typescriptFiles = @(
    Get-ChildItem -LiteralPath $frontendRoot -Recurse -File -Filter '*.ts' |
        Where-Object {
            $_.FullName -notmatch '[\\/](node_modules|dist|coverage|\.angular)[\\/]'
        } |
        Sort-Object FullName
)

$symbols = [System.Collections.Generic.List[object]]::new()
$routes = [System.Collections.Generic.List[object]]::new()
$classPattern = '(?m)^\s*export\s+(?:default\s+)?(?:abstract\s+)?class\s+(?<name>[A-Za-z_]\w*)'
$routePattern = '(?m)\bpath\s*:\s*[''"](?<path>[^''"]*)[''"]'
foreach ($typescriptFile in $typescriptFiles) {
    $content = Get-Content -LiteralPath $typescriptFile.FullName -Raw
    $path = ConvertTo-RepositoryPath $typescriptFile.FullName
    $selectorMatch = [regex]::Match($content, 'selector\s*:\s*[''"](?<selector>[^''"]+)[''"]')
    foreach ($match in [regex]::Matches($content, $classPattern)) {
        $name = $match.Groups['name'].Value
        $line = ($content.Substring(0, $match.Index) -split "`n").Count
        $symbols.Add([pscustomobject][ordered]@{
            name = $name
            role = Get-FrontendRole $name $path $content
            selector = if ($selectorMatch.Success) { $selectorMatch.Groups['selector'].Value } else { $null }
            path = $path
            line = $line
        })
    }
    if ($path -match '\.routes\.ts$|app\.routes\.ts$') {
        foreach ($routeMatch in [regex]::Matches($content, $routePattern)) {
            $line = ($content.Substring(0, $routeMatch.Index) -split "`n").Count
            $routes.Add([pscustomobject][ordered]@{
                path = $routeMatch.Groups['path'].Value
                source = $path
                line = $line
            })
        }
    }
}

$featureMap = [ordered]@{}
foreach ($symbol in $symbols) {
    $featureMatch = [regex]::Match(
        $symbol.path,
        '^FoodDiary\.Web\.Client/(?<area>src/app|projects/fooddiary-admin/src/app)/features/(?<feature>[^/]+)/'
    )
    if (-not $featureMatch.Success) {
        continue
    }
    $area = if ($featureMatch.Groups['area'].Value.StartsWith('projects/')) { 'admin' } else { 'client' }
    $featureName = $featureMatch.Groups['feature'].Value
    $key = "$area/$featureName"
    if (-not $featureMap.Contains($key)) {
        $featureMap[$key] = [pscustomobject][ordered]@{
            area = $area
            name = $featureName
            root = "FoodDiary.Web.Client/$($featureMatch.Groups['area'].Value)/features/$featureName"
            symbols = [System.Collections.Generic.List[string]]::new()
            routes = [System.Collections.Generic.List[string]]::new()
            tests = [System.Collections.Generic.List[string]]::new()
        }
    }
    $featureMap[$key].symbols.Add($symbol.name)
}
foreach ($route in $routes) {
    foreach ($feature in $featureMap.Values) {
        if ($route.source.StartsWith("$($feature.root)/")) {
            $feature.routes.Add($route.path)
        }
    }
}
$specFiles = @($typescriptFiles | Where-Object { $_.Name -like '*.spec.ts' })
foreach ($specFile in $specFiles) {
    $path = ConvertTo-RepositoryPath $specFile.FullName
    foreach ($feature in $featureMap.Values) {
        if ($path.StartsWith("$($feature.root)/")) {
            $feature.tests.Add($path)
        }
    }
}
$features = @(
    $featureMap.Values |
        ForEach-Object {
            [ordered]@{
                area = $_.area
                name = $_.name
                root = $_.root
                symbols = @($_.symbols | Sort-Object -Unique)
                routes = @($_.routes | Sort-Object -Unique)
                tests = @($_.tests | Sort-Object -Unique)
            }
        } |
        Sort-Object area, name
)

$localeRoot = Join-Path $frontendRoot 'assets/i18n'
$localeFiles = [System.Collections.Generic.List[object]]::new()
$localeNames = @(
    Get-ChildItem -LiteralPath (Join-Path $localeRoot 'en') -File -Filter '*.json' |
        ForEach-Object { $_.Name }
    Get-ChildItem -LiteralPath (Join-Path $localeRoot 'ru') -File -Filter '*.json' |
        ForEach-Object { $_.Name }
) | Sort-Object -Unique
foreach ($localeName in $localeNames) {
    $enPath = Join-Path $localeRoot "en/$localeName"
    $ruPath = Join-Path $localeRoot "ru/$localeName"
    $enExists = Test-Path -LiteralPath $enPath
    $ruExists = Test-Path -LiteralPath $ruPath
    $enKeys = if ($enExists) {
        Get-JsonPropertyCount (Get-Content -LiteralPath $enPath -Raw)
    } else { 0 }
    $ruKeys = if ($ruExists) {
        Get-JsonPropertyCount (Get-Content -LiteralPath $ruPath -Raw)
    } else { 0 }
    $localeFiles.Add([pscustomobject][ordered]@{
        name = $localeName
        englishExists = $enExists
        russianExists = $ruExists
        englishProperties = $enKeys
        russianProperties = $ruKeys
        countsMatch = $enExists -and $ruExists -and $enKeys -eq $ruKeys
    })
}

$roleCounts = [ordered]@{}
foreach ($roleGroup in @($symbols | Group-Object role | Sort-Object Name)) {
    $roleCounts[$roleGroup.Name] = $roleGroup.Count
}
$index = [ordered]@{
    schemaVersion = 1
    generator = '.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1'
    extraction = [ordered]@{
        scope = 'FoodDiary.Web.Client TypeScript excluding build, dependency, and coverage output'
        symbols = 'Exported classes with Angular/file-name role inference'
        routes = 'Literal path properties in route files'
        localization = 'English/Russian locale file presence and recursive JSON property counts'
    }
    summary = [ordered]@{
        typescriptFiles = $typescriptFiles.Count
        symbols = $symbols.Count
        routes = $routes.Count
        features = $features.Count
        specs = $specFiles.Count
        localeFiles = $localeFiles.Count
        roles = $roleCounts
    }
    features = $features
    symbols = @($symbols | Sort-Object path, line, name)
    routes = @($routes | Sort-Object source, line)
    localization = @($localeFiles | Sort-Object name)
}

$json = $index | ConvertTo-Json -Depth 15
$expectedContent = $json + [Environment]::NewLine
if ($Check) {
    if (-not (Test-Path -LiteralPath $outputPath)) {
        Write-Host 'Frontend index is missing. Run Build-LlmWikiFrontendIndex.ps1.'
        exit 1
    }
    if ([System.IO.File]::ReadAllText($outputPath) -cne $expectedContent) {
        Write-Host 'Frontend index is stale. Regenerate it with:'
        Write-Host '  ./.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1'
        exit 1
    }
    Write-Host "Frontend index is current: $($features.Count) features, $($symbols.Count) symbols, $($routes.Count) routes."
    return
}

$outputDirectory = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $expectedContent, $utf8WithoutBom)
Write-Host "Generated $(ConvertTo-RepositoryPath $outputPath)."
