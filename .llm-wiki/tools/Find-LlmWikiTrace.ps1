[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Query,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 30)]
    [int]$Limit = 10
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path

function ConvertTo-RepositoryPath {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new($resolvedPath)
    return [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pathUri).ToString())
}

function Get-LineNumber {
    param([string]$Content, [int]$Index)
    return 1 + ([regex]::Matches($Content.Substring(0, [Math]::Max(0, $Index)), "`n")).Count
}

$sourceFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.cs' |
        Where-Object {
            $_.FullName -notmatch '[\\/](obj|bin|\.artifacts|TestResults|Migrations)[\\/]' -and
            $_.Name -notmatch '\.(Designer|g)\.cs$'
        } |
        Sort-Object FullName
)

$handlerCandidates = [System.Collections.Generic.List[object]]::new()
foreach ($file in $sourceFiles) {
    if ($file.FullName -match '[\\/]tests[\\/]') { continue }
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $handlerMatches = [regex]::Matches(
        $content,
        '(?ms)(?:class|sealed\s+class)\s+(?<handler>[A-Za-z_][A-Za-z0-9_]*)\s*(?<ctor>\([^;{]*?\))?\s*:\s*I(?:Command|Query|Request)Handler\s*<\s*(?<request>[A-Za-z_][A-Za-z0-9_]*)')
    foreach ($match in $handlerMatches) {
        $requestName = $match.Groups['request'].Value
        $handlerName = $match.Groups['handler'].Value
        if ($requestName -notlike "*$Query*" -and $handlerName -notlike "*$Query*") { continue }

        $dependencies = [System.Collections.Generic.List[string]]::new()
        $constructorText = $match.Groups['ctor'].Value
        foreach ($dependencyMatch in [regex]::Matches(
            $constructorText,
            '(?m)(?<type>[A-Z][A-Za-z0-9_]*(?:<[^>]+>)?)\s+[A-Za-z_][A-Za-z0-9_]*')) {
            $dependencyType = $dependencyMatch.Groups['type'].Value
            if ($dependencyType -notin @('IRequest', 'ICommand', 'IQuery', 'CancellationToken')) {
                $dependencies.Add($dependencyType)
            }
        }

        $handlerCandidates.Add([pscustomobject]@{
            request = $requestName
            handler = $handlerName
            path = ConvertTo-RepositoryPath $file.FullName
            line = Get-LineNumber $content $match.Index
            dependencies = @($dependencies | Sort-Object -Unique)
        })
    }
}

$results = [System.Collections.Generic.List[object]]::new()
foreach ($candidate in ($handlerCandidates | Select-Object -First $Limit)) {
    $requestPattern = "\b$([regex]::Escape($candidate.request))\b"
    $requestDefinition = $null
    $implementations = [System.Collections.Generic.List[object]]::new()
    $presentation = [System.Collections.Generic.List[object]]::new()
    $tests = [System.Collections.Generic.List[object]]::new()

    foreach ($file in $sourceFiles) {
        $content = [System.IO.File]::ReadAllText($file.FullName)
        $repositoryPath = ConvertTo-RepositoryPath $file.FullName

        if ($null -eq $requestDefinition -and
            $content -match "(?:record|class)\s+$([regex]::Escape($candidate.request))\b") {
            $definitionMatch = [regex]::Match(
                $content,
                "(?:record|class)\s+$([regex]::Escape($candidate.request))\b")
            $requestDefinition = [pscustomobject]@{
                path = $repositoryPath
                line = Get-LineNumber $content $definitionMatch.Index
            }
        }

        foreach ($dependency in $candidate.dependencies) {
            if ($file.FullName -match '[\\/]tests[\\/]') { continue }
            $plainDependency = $dependency -replace '<.*$', ''
            $implementationMatch = [regex]::Match(
                $content,
                "(?:class|record)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)[^;{]*:\s*[^`r`n{]*\b$([regex]::Escape($plainDependency))\b")
            if ($implementationMatch.Success) {
                $implementations.Add([pscustomobject]@{
                    contract = $dependency
                    implementation = $implementationMatch.Groups['name'].Value
                    path = $repositoryPath
                    line = Get-LineNumber $content $implementationMatch.Index
                })
            }
        }

        if ($file.FullName -match '[\\/]FoodDiary\.Presentation\.Api[\\/]' -and
            ($content -match $requestPattern -or
             ($repositoryPath -match '/Features/' -and
              $repositoryPath -match "/$([regex]::Escape(($candidate.request -replace '(Command|Query|Request)$', '')))[^/]*"))) {
            $presentation.Add([pscustomobject]@{
                path = $repositoryPath
                confidence = if ($content -match $requestPattern) { 'direct' } else { 'heuristic' }
            })
        }

        if ($file.FullName -match '[\\/]tests[\\/]' -and
            ($content -match $requestPattern -or $content -match "\b$([regex]::Escape($candidate.handler))\b")) {
            $tests.Add([pscustomobject]@{ path = $repositoryPath })
        }
    }

    $results.Add([pscustomobject]@{
        request = $candidate.request
        requestDefinition = $requestDefinition
        handler = [pscustomobject]@{
            name = $candidate.handler
            path = $candidate.path
            line = $candidate.line
        }
        dependencies = $candidate.dependencies
        implementations = @($implementations | Sort-Object contract, implementation, path -Unique)
        presentation = @($presentation | Sort-Object path -Unique)
        tests = @($tests | Sort-Object path -Unique)
    })
}

if ($Format -eq 'Json') {
    @($results) | ConvertTo-Json -Depth 8
    exit 0
}

if ($results.Count -eq 0) {
    Write-Host "No request handlers matched '$Query'."
    exit 1
}

foreach ($result in $results) {
    Write-Host "$($result.request) -> $($result.handler.name)"
    if ($null -ne $result.requestDefinition) {
        Write-Host "  Request: $($result.requestDefinition.path):$($result.requestDefinition.line)"
    }
    Write-Host "  Handler: $($result.handler.path):$($result.handler.line)"
    if ($result.dependencies.Count -gt 0) {
        Write-Host "  Dependencies: $($result.dependencies -join ', ')"
    }
    foreach ($implementation in $result.implementations) {
        Write-Host "  Implementation: $($implementation.contract) -> $($implementation.implementation) ($($implementation.path):$($implementation.line))"
    }
    foreach ($entry in $result.presentation) {
        Write-Host "  Presentation [$($entry.confidence)]: $($entry.path)"
    }
    foreach ($test in $result.tests) {
        Write-Host "  Test: $($test.path)"
    }
    Write-Host ''
}
