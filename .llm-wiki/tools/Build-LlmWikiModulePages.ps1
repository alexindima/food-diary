[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$catalogPath = Join-Path $wikiRoot 'generated/repository-catalog.json'
$outputRoot = Join-Path $wikiRoot 'generated/modules'
$generatorPath = '.llm-wiki/tools/Build-LlmWikiModulePages.ps1'

if (-not (Test-Path -LiteralPath $catalogPath)) {
    throw 'Repository catalog is missing. Run Build-LlmWikiCatalog.ps1 first.'
}

function ConvertTo-RepositoryPath {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new($resolvedPath)
    return [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pathUri).ToString())
}

function ConvertTo-Slug {
    param([string]$Name)

    $slug = [regex]::Replace($Name, '([a-z0-9])([A-Z])', '$1-$2')
    return $slug.ToLowerInvariant()
}

function New-FrontMatter {
    param(
        [string]$Id,
        [string[]]$Sources
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('---')
    $lines.Add("id: $Id")
    $lines.Add('kind: module')
    $lines.Add('status: current')
    $lines.Add("generated_by: $generatorPath")
    $lines.Add('sources:')
    foreach ($source in $Sources) {
        $lines.Add("  - $source")
    }
    $lines.Add('---')
    return ,$lines
}

$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
$allModules = [System.Collections.Generic.List[object]]::new()
foreach ($graphModule in $catalog.applicationModules) {
    $allModules.Add([pscustomobject]@{
        name = $graphModule.name
        dependencies = @($graphModule.dependencies)
        origin = 'module-graph'
        project = $null
    })
}
foreach ($extractedModule in $catalog.extractedApplicationModules) {
    if (@($allModules | Where-Object { $_.name -eq $extractedModule.name }).Count -eq 0) {
        $allModules.Add([pscustomobject]@{
            name = $extractedModule.name
            dependencies = @()
            origin = 'extracted-project'
            project = $extractedModule.project
        })
    }
}
$moduleNames = @($allModules | ForEach-Object { $_.name })
$allDirectories = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -Directory |
        Where-Object {
            $_.FullName -notmatch '[\\/](\.git|\.github|\.llm-wiki|docs|node_modules|obj|bin|dist|coverage|\.artifacts|TestResults)[\\/]'
        }
)
$allTestFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.cs' |
        Where-Object {
            $_.FullName -match '[\\/]tests[\\/]' -and
            $_.FullName -notmatch '[\\/](obj|bin|\.artifacts|TestResults)[\\/]'
        }
)

$generatedFiles = [ordered]@{}
$indexLines = New-FrontMatter 'generated.application-modules' @(
    $generatorPath
    '.llm-wiki/generated/repository-catalog.json'
    'docs/architecture/module-dependencies.json'
)
$indexLines.Add('')
$indexLines.Add('# Application Modules')
$indexLines.Add('')
$indexLines.Add('This index is generated from the executable application-module graph and')
$indexLines.Add('repository catalog. Regenerate it instead of editing it manually.')
$indexLines.Add('')
$indexLines.Add('| Module | Dependencies | Consumers | Controllers |')
$indexLines.Add('| --- | ---: | ---: | ---: |')

foreach ($module in @($allModules | Sort-Object { Get-LlmWikiOrdinalSortKey $_.name })) {
    $moduleName = [string]$module.name
    $slug = ConvertTo-Slug $moduleName
    $relativeOutputPath = ".llm-wiki/generated/modules/$slug.md"
    $dependencies = @($module.dependencies | Sort-Object { Get-LlmWikiOrdinalSortKey $_ })
    $consumers = @(
        $allModules |
            Where-Object { @($_.dependencies) -contains $moduleName } |
            ForEach-Object { $_.name } |
            Sort-Object { Get-LlmWikiOrdinalSortKey $_ }
    )
    $controllers = @(
        $catalog.http.controllers |
            Where-Object {
                $_.path -match "/Features/$([regex]::Escape($moduleName))/" -or
                $_.name -like "$moduleName*Controller"
            } |
            Sort-Object { Get-LlmWikiOrdinalSortKey $_.path }
    )
    $sourceDirectories = @(
        $allDirectories |
            Where-Object { $_.Name -eq $moduleName } |
            ForEach-Object { ConvertTo-RepositoryPath $_.FullName } |
            Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique
    )
    $tests = @(
        $allTestFiles |
            Where-Object {
                (ConvertTo-RepositoryPath $_.FullName) -match "/$([regex]::Escape($moduleName))(/|[^/]*Tests?\.cs$)"
            } |
            ForEach-Object { ConvertTo-RepositoryPath $_.FullName } |
            Sort-Object { Get-LlmWikiOrdinalSortKey $_ } |
            Select-Object -First 30
    )

    $lines = New-FrontMatter "generated.module.$slug" @(
        $generatorPath
        '.llm-wiki/generated/repository-catalog.json'
        'docs/architecture/module-dependencies.json'
    )
    $lines.Add('')
    $lines.Add("# $moduleName")
    $lines.Add('')
    $lines.Add('## Graph')
    $lines.Add('')
    $lines.Add("- Origin: $($module.origin)")
    if (-not [string]::IsNullOrWhiteSpace($module.project)) {
        $lines.Add(('- Extracted project: `{0}`' -f $module.project))
    }
    $lines.Add("- Dependencies: $(if ($dependencies.Count) { $dependencies -join ', ' } else { 'none' })")
    $lines.Add("- Consumers: $(if ($consumers.Count) { $consumers -join ', ' } else { 'none' })")
    $lines.Add('')
    $lines.Add('## Source Areas')
    $lines.Add('')
    if ($sourceDirectories.Count -eq 0) {
        $lines.Add('- No exact module-named source directory was found.')
    } else {
        foreach ($sourceDirectory in $sourceDirectories) {
            $lines.Add(('- `{0}`' -f $sourceDirectory))
        }
    }
    $lines.Add('')
    $lines.Add('## HTTP Surface')
    $lines.Add('')
    if ($controllers.Count -eq 0) {
        $lines.Add('No literal attribute-routed controller was associated with this module.')
    } else {
        foreach ($controller in $controllers) {
            $lines.Add("### $($controller.name)")
            $lines.Add('')
            $lines.Add(('Source: `{0}`' -f $controller.path))
            $lines.Add('')
            foreach ($endpoint in $controller.endpoints) {
                $lines.Add(('- `{0} {1}`' -f $endpoint.verb, $endpoint.route))
            }
            $lines.Add('')
        }
    }
    $lines.Add('## Focused Tests')
    $lines.Add('')
    if ($tests.Count -eq 0) {
        $lines.Add('No test file with an exact module path/name match was found.')
    } else {
        foreach ($test in $tests) {
            $lines.Add(('- `{0}`' -f $test))
        }
    }
    $lines.Add('')
    $lines.Add('## Working Rule')
    $lines.Add('')
    $lines.Add('Use this page for discovery only. Read the nearest scoped `AGENTS.md` and')
    $lines.Add('verify behavior in source code, tests, and API contract snapshots before')
    $lines.Add('changing the module.')

    $generatedFiles[$relativeOutputPath] = ($lines -join [Environment]::NewLine) + [Environment]::NewLine
    $indexLines.Add("| [$moduleName]($slug.md) | $($dependencies.Count) | $($consumers.Count) | $($controllers.Count) |")
}

$indexRelativePath = '.llm-wiki/generated/modules/index.md'
$generatedFiles[$indexRelativePath] = ($indexLines -join [Environment]::NewLine) + [Environment]::NewLine

if ($Check) {
    $errors = [System.Collections.Generic.List[string]]::new()
    foreach ($entry in $generatedFiles.GetEnumerator()) {
        $absolutePath = Join-Path $repositoryRoot $entry.Key
        if (-not (Test-Path -LiteralPath $absolutePath)) {
            $errors.Add("Missing generated module page: $($entry.Key)")
            continue
        }
        if (-not (Test-LlmWikiTextEquivalent -ActualPath $absolutePath -ExpectedText $entry.Value)) {
            $errors.Add("Stale generated module page: $($entry.Key)")
        }
    }

    if (Test-Path -LiteralPath $outputRoot) {
        $actualGeneratedPaths = @(
            Get-ChildItem -LiteralPath $outputRoot -File -Filter '*.md' |
                ForEach-Object { ConvertTo-RepositoryPath $_.FullName }
        )
        foreach ($actualPath in $actualGeneratedPaths) {
            if (-not $generatedFiles.Contains($actualPath)) {
                $errors.Add("Unexpected generated module page: $actualPath")
            }
        }
    }

    if ($errors.Count -gt 0) {
        Write-Host "Generated application-module pages are stale ($($errors.Count) error(s)):"
        foreach ($errorMessage in $errors) {
            Write-Host " - $errorMessage"
        }
        Write-Host 'Regenerate with ./.llm-wiki/tools/Build-LlmWikiModulePages.ps1'
        exit 1
    }

    Write-Host "Generated application-module pages are current: $($moduleNames.Count) modules."
    return
}

if (-not (Test-Path -LiteralPath $outputRoot)) {
    New-Item -ItemType Directory -Path $outputRoot | Out-Null
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
foreach ($entry in $generatedFiles.GetEnumerator()) {
    $absolutePath = Join-Path $repositoryRoot $entry.Key
    [System.IO.File]::WriteAllText($absolutePath, $entry.Value, $utf8WithoutBom)
}

Get-ChildItem -LiteralPath $outputRoot -File -Filter '*.md' |
    Where-Object { -not $generatedFiles.Contains((ConvertTo-RepositoryPath $_.FullName)) } |
    Remove-Item -Force

Write-Host "Generated $($moduleNames.Count) application-module pages and index."
