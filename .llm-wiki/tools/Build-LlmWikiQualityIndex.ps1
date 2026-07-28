[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$symbolIndexPath = Join-Path $wikiRoot 'generated/csharp-symbol-index.json'
$outputPath = Join-Path $wikiRoot 'generated/quality-index.json'

function ConvertTo-RepositoryPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath($Path).Substring($repositoryRoot.Length + 1).Replace('\', '/')
}

$symbols = (Get-Content -LiteralPath $symbolIndexPath -Raw | ConvertFrom-Json).symbols
$criticalRoles = @('CommandHandler', 'QueryHandler', 'Handler', 'Controller', 'Validator')
$criticalSymbols = @($symbols | Where-Object { $_.role -in $criticalRoles -and $_.kind -ne 'interface' })

$additionalSourceRoots = @(
    Join-Path $repositoryRoot 'FoodDiary.Web.Client/.storybook'
)
$repositoryFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File
    foreach ($additionalSourceRoot in $additionalSourceRoots) {
        if (Test-Path -LiteralPath $additionalSourceRoot -PathType Container) {
            Get-ChildItem -LiteralPath $additionalSourceRoot -Recurse -File
        }
    }
) | Sort-Object { Get-LlmWikiOrdinalSortKey $_.FullName } -Unique

$testFiles = @(
    $repositoryFiles |
        Where-Object {
            $_.FullName -notmatch '[\\/](node_modules|obj|bin|dist|coverage|\.angular|\.artifacts|TestResults)[\\/]' -and
            ($_.FullName -match '[\\/]tests[\\/]' -or $_.Name -match '\.(spec|test)\.ts$') -and
            $_.Extension -in @('.cs', '.ts')
        } |
        Sort-Object { Get-LlmWikiOrdinalSortKey $_.FullName } |
        ForEach-Object {
            [pscustomobject]@{
                path = ConvertTo-RepositoryPath $_.FullName
                content = [System.IO.File]::ReadAllText($_.FullName)
            }
        }
)

$symbolCoverage = [System.Collections.Generic.List[object]]::new()
foreach ($symbol in $criticalSymbols) {
    $references = @(
        $testFiles |
            Where-Object { $_.content.IndexOf($symbol.name, [System.StringComparison]::Ordinal) -ge 0 } |
            Select-Object -ExpandProperty path
    )
    $symbolCoverage.Add([pscustomobject]@{
        name = $symbol.name
        role = $symbol.role
        path = $symbol.path
        line = $symbol.line
        testReferenceCount = $references.Count
        testReferences = $references
    })
}

$productionFiles = @(
    $repositoryFiles |
        Where-Object {
            $_.FullName -notmatch '[\\/](tests|node_modules|obj|bin|dist|coverage|\.angular|\.artifacts|TestResults|Migrations)[\\/]' -and
            ($_.Extension -eq '.cs' -or
             ($_.Extension -eq '.ts' -and $_.FullName -match '[\\/]FoodDiary\.(Web\.Client|Mobile)[\\/]')) -and
            $_.Name -notmatch '\.(Designer|g|spec|test)\.(cs|ts)$'
        } |
        Sort-Object { Get-LlmWikiOrdinalSortKey $_.FullName }
)

$fileMetrics = [System.Collections.Generic.List[object]]::new()
$debtMarkers = [System.Collections.Generic.List[object]]::new()
foreach ($file in $productionFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $path = ConvertTo-RepositoryPath $file.FullName
    $lineCount = @($content -split '\r?\n' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    $decisionCount = [regex]::Matches(
        $content,
        '\b(if|else\s+if|for|foreach|while|switch|case|catch)\b|&&|\|\||\?\?').Count
    $fileCritical = @($symbolCoverage | Where-Object path -eq $path)
    $unreferenced = @($fileCritical | Where-Object testReferenceCount -eq 0).Count
    $score = [Math]::Round(($lineCount / 50.0) + ($decisionCount * 1.5) + ($fileCritical.Count * 2) + ($unreferenced * 5), 2)
    $serializedScore = if ($score -eq [Math]::Truncate($score)) {
        [int]$score
    } else {
        $score
    }
    $fileMetrics.Add([pscustomobject]@{
        path = $path
        nonBlankLines = $lineCount
        decisionPoints = $decisionCount
        criticalSymbols = $fileCritical.Count
        unreferencedCriticalSymbols = $unreferenced
        structuralRiskScore = $serializedScore
    })
    foreach ($match in [regex]::Matches($content, '(?im)(?<marker>TODO|FIXME|HACK|#pragma\s+warning\s+disable|SuppressMessage)\b')) {
        $line = 1 + [regex]::Matches($content.Substring(0, $match.Index), "`n").Count
        $debtMarkers.Add([pscustomobject]@{
            path = $path
            line = $line
            marker = $match.Groups['marker'].Value
        })
    }
}

$unreferencedSymbols = @($symbolCoverage | Where-Object testReferenceCount -eq 0)
$result = [ordered]@{
    schemaVersion = 1
    semantics = [ordered]@{
        testReferenceCoverage = 'A symbol name appears in at least one test source file. This is not execution or line coverage.'
        structuralRiskScore = 'nonBlankLines/50 + decisionPoints*1.5 + criticalSymbols*2 + unreferencedCriticalSymbols*5'
    }
    summary = [ordered]@{
        productionFiles = $fileMetrics.Count
        criticalSymbols = $symbolCoverage.Count
        criticalSymbolsWithTestReferences = @($symbolCoverage | Where-Object testReferenceCount -gt 0).Count
        criticalSymbolsWithoutTestReferences = $unreferencedSymbols.Count
        debtMarkers = $debtMarkers.Count
    }
    hotspots = @(
        $fileMetrics |
            Sort-Object `
                @{ Expression = 'structuralRiskScore'; Descending = $true },
                @{ Expression = { Get-LlmWikiOrdinalSortKey $_.path } } |
            Select-Object -First 100
    )
    files = @($fileMetrics | Sort-Object { Get-LlmWikiOrdinalSortKey $_.path })
    criticalSymbols = @(
        $symbolCoverage |
            Sort-Object {
                Get-LlmWikiOrdinalSortKey "$($_.role)`0$($_.name)`0$($_.path)"
            }
    )
    debtMarkers = @(
        $debtMarkers |
            Sort-Object `
                @{ Expression = { Get-LlmWikiOrdinalSortKey $_.path } },
                line
    )
}
$jsonText = ($result | ConvertTo-Json -Depth 10) + [Environment]::NewLine

if ($Check) {
    if (-not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $jsonText -Depth 10)) {
        Write-Host 'Quality index is stale. Run ./.llm-wiki/wiki.ps1 update.'
        exit 1
    }
    Write-Host "Quality index is current: $($result.summary.criticalSymbols) critical symbols, $($result.summary.criticalSymbolsWithoutTestReferences) without test references."
    exit 0
}

$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $jsonText, $utf8WithoutBom)
Write-Host "Generated .llm-wiki/generated/quality-index.json."
