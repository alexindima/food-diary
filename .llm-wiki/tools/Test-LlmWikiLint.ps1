[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$lintTool = Join-Path $PSScriptRoot 'Test-LlmWiki.ps1'
$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) "food-diary-llm-wiki-lint-$([guid]::NewGuid().ToString('N'))"
$fixtureWiki = Join-Path $fixtureRoot '.llm-wiki'
$fixtureTools = Join-Path $fixtureWiki 'tools'
$powerShellExecutable = (Get-Process -Id $PID).Path

function Assert-Lint {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw "LLM Wiki lint regression failed: $Message"
    }
}

function Invoke-FixtureLint {
    $output = & $powerShellExecutable -NoProfile -File $lintTool `
        -WikiRoot $fixtureWiki `
        -RepositoryRoot $fixtureRoot `
        -Format Json
    $exitCode = $LASTEXITCODE
    return [pscustomobject]@{
        exitCode = $exitCode
        result = ($output -join "`n" | ConvertFrom-Json)
    }
}

try {
    $null = New-Item -ItemType Directory -Path $fixtureTools -Force
    Set-Content -LiteralPath (Join-Path $fixtureRoot 'source.txt') -Value 'source'
    Set-Content -LiteralPath (Join-Path $fixtureTools 'Build-Fixture.ps1') -Value '# deterministic fixture generator'

    @'
---
id: fixture.valid
kind: system
status: current
sources:
  - source.txt
---
# Valid heading

[Self](#valid-heading)
'@ | Set-Content -LiteralPath (Join-Path $fixtureWiki 'valid.md')

    $valid = Invoke-FixtureLint
    Assert-Lint ($valid.exitCode -eq 0) 'a valid page was rejected.'
    Assert-Lint ($valid.result.valid -eq $true) 'valid JSON result was not marked valid.'

    @'
---
id: fixture.invalid
id: fixture.duplicate
kind: unsupported
status: current
unknown_field: value
sources:
  - ../outside.txt
---
# Existing

[Broken path](missing.md)
[Broken anchor](#missing)
ghp_123456789012345678901234567890
'@ | Set-Content -LiteralPath (Join-Path $fixtureWiki 'invalid.md')

    $invalid = Invoke-FixtureLint
    $codes = @($invalid.result.diagnostics.code)
    Assert-Lint ($invalid.exitCode -eq 1) 'an invalid page did not fail lint.'
    foreach ($expectedCode in @('WIKI005', 'WIKI006', 'WIKI009', 'WIKI015', 'WIKI017', 'WIKI018', 'WIKI019')) {
        Assert-Lint ($expectedCode -in $codes) "expected diagnostic $expectedCode was not emitted."
    }

    Write-Host 'LLM Wiki lint regression passed.'
    $global:LASTEXITCODE = 0
} finally {
    if (Test-Path -LiteralPath $fixtureRoot) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}
