[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Query,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 30)]
    [int]$Limit = 10,
    [switch]$Compact
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

function Get-SearchTerms {
    param([string]$Text)

    $aliases = @{
        'dietitian' = 'dietologist'; 'nutritionist' = 'dietologist'
        'invitation' = 'invite'; 'inviting' = 'invite'; 'invited' = 'invite'
        'mail' = 'email'
        'url' = 'link'
    }
    # Keep the script Windows PowerShell 5 compatible: non-ASCII aliases are
    # represented as JSON escapes so the file does not depend on a UTF-8 BOM.
    $aliases[(ConvertFrom-Json '"\u0434\u0438\u0435\u0442\u043e\u043b\u043e\u0433"')] = 'dietologist'
    $aliases[(ConvertFrom-Json '"\u0434\u0438\u0435\u0442\u043e\u043b\u043e\u0433\u0430"')] = 'dietologist'
    $aliases[(ConvertFrom-Json '"\u0434\u0438\u0435\u0442\u043e\u043b\u043e\u0433\u0443"')] = 'dietologist'
    $aliases[(ConvertFrom-Json '"\u043f\u0440\u0438\u0433\u043b\u0430\u0441\u0438\u0442\u044c"')] = 'invite'
    $aliases[(ConvertFrom-Json '"\u043f\u0440\u0438\u0433\u043b\u0430\u0448\u0435\u043d\u0438\u0435"')] = 'invite'
    $aliases[(ConvertFrom-Json '"\u043f\u0440\u0438\u0433\u043b\u0430\u0448\u0435\u043d\u0438\u044f"')] = 'invite'
    $aliases[(ConvertFrom-Json '"\u043f\u043e\u0447\u0442\u0430"')] = 'email'
    $aliases[(ConvertFrom-Json '"\u043f\u0438\u0441\u044c\u043c\u043e"')] = 'email'
    $aliases[(ConvertFrom-Json '"\u043f\u0438\u0441\u044c\u043c\u0435"')] = 'email'
    $aliases[(ConvertFrom-Json '"\u0441\u0441\u044b\u043b\u043a\u0430"')] = 'link'
    $aliases[(ConvertFrom-Json '"\u0441\u0441\u044b\u043b\u043a\u0435"')] = 'link'
    $aliases[(ConvertFrom-Json '"\u0441\u0441\u044b\u043b\u043a\u0443"')] = 'link'
    $stopWords = @('a', 'an', 'and', 'for', 'from', 'in', 'of', 'on', 'the', 'to', 'with',
        'bug')

    return @(
        [regex]::Matches($Text.ToLowerInvariant(), '[\p{L}\p{Nd}]+') |
            ForEach-Object {
                $term = $_.Value
                if ($aliases.ContainsKey($term)) { $aliases[$term] } else { $term }
            } |
            Where-Object { $_.Length -ge 3 -and $_ -notin $stopWords } |
            Sort-Object -Unique
    )
}

$queryTerms = @(Get-SearchTerms $Query)

$sourceFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.cs' |
        Where-Object {
            $_.FullName -notmatch '[\\/](obj|bin|\.artifacts|TestResults|Migrations)[\\/]' -and
            $_.Name -notmatch '\.(Designer|g)\.cs$'
        } |
        Sort-Object FullName
)
$sourceDocuments = @(
    foreach ($file in $sourceFiles) {
        [pscustomobject]@{
            file = $file
            content = [System.IO.File]::ReadAllText($file.FullName)
            repositoryPath = ConvertTo-RepositoryPath $file.FullName
            isTest = $file.FullName -match '[\\/]tests[\\/]'
        }
    }
)

$handlerCandidates = [System.Collections.Generic.List[object]]::new()
foreach ($document in $sourceDocuments) {
    if ($document.isTest) { continue }
    $file = $document.file
    $content = $document.content
    $handlerMatches = [regex]::Matches(
        $content,
        '(?ms)(?:class|sealed\s+class)\s+(?<handler>[A-Za-z_][A-Za-z0-9_]*)\s*(?<ctor>\([^;{]*?\))?\s*:\s*I(?:Command|Query|Request)Handler\s*<\s*(?<request>[A-Za-z_][A-Za-z0-9_]*)')
    foreach ($match in $handlerMatches) {
        $requestName = $match.Groups['request'].Value
        $handlerName = $match.Groups['handler'].Value
        $repositoryPath = $document.repositoryPath
        $searchableText = "$requestName $handlerName $repositoryPath $content".ToLowerInvariant()
        $matchedTerms = @($queryTerms | Where-Object { $searchableText.Contains($_) })
        $exactNameMatch = $requestName -like "*$Query*" -or $handlerName -like "*$Query*"
        if (-not $exactNameMatch -and $matchedTerms.Count -eq 0) { continue }
        $score = ($matchedTerms.Count * 10)
        if ($exactNameMatch) { $score += 100 }
        foreach ($term in $matchedTerms) {
            if ($requestName.ToLowerInvariant().Contains($term)) { $score += 8 }
            if ($handlerName.ToLowerInvariant().Contains($term)) { $score += 6 }
            if ($repositoryPath.ToLowerInvariant().Contains($term)) { $score += 3 }
        }

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
            path = $repositoryPath
            line = Get-LineNumber $content $match.Index
            dependencies = @($dependencies | Sort-Object -Unique)
            score = $score
            matchedTerms = $matchedTerms
        })
    }
}

$results = [System.Collections.Generic.List[object]]::new()
foreach ($candidate in ($handlerCandidates | Sort-Object @{ Expression = 'score'; Descending = $true }, request, path | Select-Object -First $Limit)) {
    $requestPattern = "\b$([regex]::Escape($candidate.request))\b"
    $requestDefinition = $null
    $implementations = [System.Collections.Generic.List[object]]::new()
    $presentation = [System.Collections.Generic.List[object]]::new()
    $tests = [System.Collections.Generic.List[object]]::new()

    foreach ($document in $sourceDocuments) {
        $file = $document.file
        $content = $document.content
        $repositoryPath = $document.repositoryPath

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
            if ($document.isTest) { continue }
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

        if ($document.isTest -and
            ($content -match $requestPattern -or $content -match "\b$([regex]::Escape($candidate.handler))\b")) {
            $tests.Add([pscustomobject]@{ path = $repositoryPath })
        }
    }

    $results.Add([pscustomobject]@{
        request = $candidate.request
        match = [pscustomobject]@{
            score = $candidate.score
            queryTerms = $queryTerms
            matchedTerms = $candidate.matchedTerms
        }
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
    exit 0
}

$displayResults = if ($Compact) { @($results | Select-Object -First 1) } else { @($results) }
foreach ($result in $displayResults) {
    Write-Host "$($result.request) -> $($result.handler.name)"
    if ($null -ne $result.requestDefinition) {
        Write-Host "  Request: $($result.requestDefinition.path):$($result.requestDefinition.line)"
    }
    Write-Host "  Handler: $($result.handler.path):$($result.handler.line)"
    if ($result.dependencies.Count -gt 0) {
        Write-Host "  Dependencies: $($result.dependencies -join ', ')"
    }
    foreach ($implementation in @($result.implementations | Select-Object -First $(if ($Compact) { 5 } else { 1000 }))) {
        Write-Host "  Implementation: $($implementation.contract) -> $($implementation.implementation) ($($implementation.path):$($implementation.line))"
    }
    foreach ($entry in @($result.presentation | Select-Object -First $(if ($Compact) { 5 } else { 1000 }))) {
        Write-Host "  Presentation [$($entry.confidence)]: $($entry.path)"
    }
    foreach ($test in @($result.tests | Select-Object -First $(if ($Compact) { 5 } else { 1000 }))) {
        Write-Host "  Test: $($test.path)"
    }
    if ($Compact) { Write-Host '  Compact trace: use -FullTrace for every match and consumer.' }
    Write-Host ''
}
