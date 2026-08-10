[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGeneratedArtifacts.ps1')
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("llm-wiki-generated-" + [guid]::NewGuid().ToString('N'))
$generated = Join-Path $tempRoot 'generated'
$backup = Join-Path $tempRoot 'backup'
try {
    $null = New-Item -ItemType Directory -Path $generated, $backup -Force
    [IO.File]::WriteAllText((Join-Path $backup 'same.json'), "{`n  `"value`": 1`n}", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $generated 'same.json'), '{"value":1}', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $backup 'changed.json'), '{"value":1}', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $generated 'changed.json'), '{"value":2}', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $backup 'same.md'), "one`r`ntwo`r`n", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $generated 'same.md'), "one`ntwo`n", [Text.UTF8Encoding]::new($false))
    $restored = @(Restore-LlmWikiSemanticNoOpArtifacts -GeneratedRoot $generated -BackupRoot $backup)
    if (@($restored).Count -ne 2 -or $restored -notcontains 'same.json' -or $restored -notcontains 'same.md') {
        throw "Unexpected semantic no-op set: $($restored -join ', ')"
    }
    if ([IO.File]::ReadAllText((Join-Path $generated 'changed.json')) -cne '{"value":2}') {
        throw 'Meaningful generated JSON was incorrectly restored.'
    }
    Write-Host 'LLM Wiki generated-artifact regression passed: formatting-only rewrites are suppressed and model changes remain.'
} finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
