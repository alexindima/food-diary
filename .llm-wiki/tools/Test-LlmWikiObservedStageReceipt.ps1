[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Join-Path ([IO.Path]::GetTempPath()) "llm-wiki-observed-stage-$PID"
$null = New-Item -ItemType Directory -Path $root -Force
try {
    $tool = Join-Path $root 'pass.ps1'
    $argumentsPath = Join-Path $root 'arguments.json'
    $resultPath = Join-Path $root 'result.json'
    $receiptPath = Join-Path $root 'stage.passed'
    [IO.File]::WriteAllText($tool, "param([string]`$Value) if (`$Value -ne 'ok') { throw 'bad value' }", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($argumentsPath, '{"Value":"ok"}', [Text.UTF8Encoding]::new($false))
    & (Join-Path $PSScriptRoot 'Invoke-LlmWikiObservedStage.ps1') -ToolPath $tool -ArgumentsPath $argumentsPath -StageName sample -ResultPath $resultPath -Fingerprint abc -PassedReceiptPath $receiptPath
    $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
    if ($result.status -ne 'passed' -or $result.fingerprint -ne 'abc' -or -not (Test-Path -LiteralPath $receiptPath)) {
        throw 'Observed stage did not persist its own durable success receipt.'
    }
    Write-Host 'LLM Wiki observed-stage receipt regression passed.'
} finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
