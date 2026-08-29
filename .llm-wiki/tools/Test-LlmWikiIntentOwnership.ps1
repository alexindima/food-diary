[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$tool = Join-Path $PSScriptRoot 'Find-LlmWikiIntentOwnership.ps1'
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ("llm-wiki-ownership-" + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixtureRoot -Force

function Assert-Ownership([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

try {
    $classifiedFixture = Join-Path $fixtureRoot 'classified.json'
    [pscustomobject][ordered]@{
        records = @(
            [pscustomobject][ordered]@{
                path = 'FoodDiary.Application.Fasting/Queries/GetCurrentFasting/GetCurrentFastingQuery.cs'
                score = 100
                confidence = 'high'
                reasons = @('fixture')
            },
            [pscustomobject][ordered]@{
                path = 'Modules/Fasting/Application/Queries/GetFastingHistoryQuery.cs'
                module = 'FoodDiary.Modules.Fasting'
                score = 90
                confidence = 'medium'
                reasons = @('fixture')
            }
        )
        rankingSummary = [pscustomobject][ordered]@{
            confidence = 'high'
            ambiguous = $false
            ambiguityReason = $null
        }
        fingerprint = 'fixture'
        updatedAtUtc = '2026-08-29T00:00:00Z'
        durationMs = 1
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $classifiedFixture -Encoding utf8NoBOM

    $classified = & $tool -Query Fasting -Format Json -SearchFixturePath $classifiedFixture | ConvertFrom-Json
    Assert-Ownership $classified.conclusive 'Ownership should remain conclusive when a SQLite record omits module.'
    Assert-Ownership (@($classified.directModules) -contains 'Fasting') 'Legacy and logical Fasting paths were not classified to the canonical business-module id.'
    Assert-Ownership (@($classified.directModules | Where-Object { $_ -eq 'Fasting' }).Count -eq 1) 'Fasting ownership was duplicated across physical project ids.'
    Assert-Ownership (@($classified.ownershipGuides).Count -eq 1) 'Exact module ownership should suppress lower-authority search owners.'
    Assert-Ownership (@($classified.candidates).Count -eq 2) 'Search candidates should remain available as diagnostic evidence.'

    $unknownFixture = Join-Path $fixtureRoot 'unknown.json'
    [pscustomobject][ordered]@{
        records = @([pscustomobject][ordered]@{
            path = 'misc/unknown.txt'
            score = 1
            confidence = 'low'
        })
        rankingSummary = [pscustomobject][ordered]@{
            confidence = 'low'
            ambiguous = $false
        }
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $unknownFixture -Encoding utf8NoBOM

    $unknown = & $tool -Query Unknown -Format Json -SearchFixturePath $unknownFixture | ConvertFrom-Json
    Assert-Ownership $unknown.abstained 'Low-confidence unclassified ownership should abstain.'
    Assert-Ownership (@($unknown.directModules).Count -eq 0) 'Unclassified paths must not invent module ownership.'
    Assert-Ownership (-not [string]::IsNullOrWhiteSpace([string]$unknown.schemaVersion)) 'Ownership JSON schemaVersion is required.'

    $exact = & $tool -Query Fasting -Format Json -SearchFixturePath $unknownFixture | ConvertFrom-Json
    Assert-Ownership $exact.conclusive 'An exact backend module intent should override ambiguous search ranking.'
    Assert-Ownership (@($exact.directModules) -contains 'Fasting') 'Exact module intent did not resolve the logical Fasting module.'
    Assert-Ownership ($exact.confidence -eq 'high') 'Exact module inventory ownership must report high confidence.'
    Assert-Ownership ($exact.selectionSource -eq 'backend-module-inventory') 'Exact module inventory ownership did not report its selection source.'
    Assert-Ownership (@($exact.ownershipGuides | Where-Object guide -eq 'Modules/Fasting/AGENTS.md').Count -eq 1) 'Exact Fasting intent did not resolve its logical-module guide.'

    Write-Host 'LLM Wiki intent ownership regression passed: optional record fields and logical module paths are normalized.'
} finally {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}
