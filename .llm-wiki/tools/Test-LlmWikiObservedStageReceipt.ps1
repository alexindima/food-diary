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
    # Run the actual facade stage function against owned child tools. Retention
    # noise is collapsed, but every record attempt and other failure remains visible.
    $toolsRoot = Join-Path $root '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $toolsRoot -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Invoke-LlmWikiObservedStage.ps1') -Destination $toolsRoot
    [IO.File]::WriteAllText((Join-Path $toolsRoot 'pass.ps1'), '# passing child')
    [IO.File]::WriteAllText((Join-Path $toolsRoot 'fail.ps1'), "throw 'fixture child failed'")
    [IO.File]::WriteAllText((Join-Path $toolsRoot 'Get-LlmWikiVerificationStageFingerprint.ps1'), "param([string]`$Stage, [hashtable]`$Arguments) 'fixture-fingerprint'")
    $telemetryPath = Join-Path $toolsRoot 'Manage-LlmWikiVerificationTelemetry.ps1'
    $stub = @'
[CmdletBinding()]
param([string]$Action, [string]$WorkspacePath, [string]$CheckId, [string]$Status, [double]$DurationSeconds, [string]$Command, [string]$InputFingerprint, [string]$Format)
[IO.File]::AppendAllText((Join-Path $PSScriptRoot 'attempts.txt'), "record`n")
$record = [Management.Automation.ErrorRecord]::new([InvalidOperationException]::new('fixture retention reached'), 'LlmWikiTelemetryRetentionReached', [Management.Automation.ErrorCategory]::LimitsExceeded, $null)
$PSCmdlet.ThrowTerminatingError($record)
'@
    [IO.File]::WriteAllText($telemetryPath, $stub)
    $facade = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../wiki.ps1'))
    $ast = [Management.Automation.Language.Parser]::ParseInput($facade, [ref]$null, [ref]$null)
    $stageFunction = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-ObservedWikiStage' }, $true)
    . ([scriptblock]::Create($stageFunction.Extent.Text))
    $script:verifyStageOrdinal = 0
    $script:verifyStageTotal = 5
    $script:verifyRunId = 'fixture'
    $script:verifyStageExpectedSeconds = @{}
    $script:verifyTelemetryRetentionWarned = $false
    $ResumePassedStages = $false
    $requestedVerifyRunRoot = Join-Path $root '.artifacts/llm-wiki/verify-runs/fixture'
    $warnings = @(
        Invoke-ObservedWikiStage 'first' 'pass.ps1' @{} 30 'fixture' 3>&1
        Invoke-ObservedWikiStage 'second' 'pass.ps1' @{} 30 'fixture' 3>&1
    ) | Where-Object { $_ -is [Management.Automation.WarningRecord] }
    if (@($warnings).Count -ne 1) { throw 'Repeated telemetry retention warnings were not collapsed within one run.' }
    [IO.File]::WriteAllText($telemetryPath, $stub.Replace('$PSCmdlet.ThrowTerminatingError($record)', "throw 'fixture registry unavailable'"))
    $warnings = @(Invoke-ObservedWikiStage 'third' 'pass.ps1' @{} 30 'fixture' 3>&1) | Where-Object { $_ -is [Management.Automation.WarningRecord] }
    if (@($warnings).Count -ne 1 -or $warnings[0].Message -notlike '*registry unavailable*') { throw 'A distinct telemetry failure was suppressed.' }
    [IO.File]::WriteAllText($telemetryPath, $stub)
    $script:verifyTelemetryRetentionWarned = $false
    $warnings = @(Invoke-ObservedWikiStage 'next-run' 'pass.ps1' @{} 30 'fixture' 3>&1) | Where-Object { $_ -is [Management.Automation.WarningRecord] }
    if (@($warnings).Count -ne 1) { throw 'A new verify run did not report retention.' }
    $failed = $false
    try { Invoke-ObservedWikiStage 'failed' 'fail.ps1' @{} 30 'fixture' 3>&1 | Out-Null }
    catch { $failed = $_.Exception.Message -like '*Wiki verify stage failed*' }
    if (-not $failed) { throw 'Telemetry handling concealed a child stage failure.' }
    if (@([IO.File]::ReadAllLines((Join-Path $toolsRoot 'attempts.txt'))).Count -ne 5) { throw 'Collapsed warnings skipped telemetry record attempts.' }
    Write-Host 'LLM Wiki observed-stage receipt regression passed.'
} finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
