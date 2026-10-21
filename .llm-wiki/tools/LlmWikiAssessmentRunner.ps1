. (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')

function Invoke-LlmWikiAssessment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ToolName,
        [Parameter(Mandatory)][hashtable]$Arguments,
        [string]$Label = $ToolName,
        [ValidateRange(1, 60)][int]$HeartbeatSeconds = 10,
        [ValidateRange(1, 7200)][int]$TimeoutSeconds = 1800
    )
    $repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
    if ($ToolName -notmatch '^[A-Za-z0-9-]+\.ps1$') { throw 'Assessment tool must be a Wiki tool filename.' }
    $toolPath = Join-Path $PSScriptRoot $ToolName
    $callArguments = @{} + $Arguments
    $callArguments.Format = 'Json'
    $argumentIdentity = [ordered]@{}
    foreach ($key in @($callArguments.Keys | Sort-Object)) { $argumentIdentity[$key] = $callArguments[$key] }
    $action = [string]$callArguments.Action
    # No mutating command or application test execution is replayed here.
    $cacheable = $ToolName -in @('Manage-LlmWikiTaskWorkspace.ps1', 'Manage-LlmWikiRequirementModel.ps1', 'Manage-LlmWikiPlanConformance.ps1', 'Manage-LlmWikiProofOfChange.ps1', 'Manage-LlmWikiImpactSimulation.ps1', 'Manage-LlmWikiFailurePrediction.ps1', 'Manage-LlmWikiVerificationCost.ps1', 'Manage-LlmWikiChangeCritique.ps1', 'Manage-LlmWikiConfidenceLedger.ps1') -and $action -in @('assess', 'verify', 'status') -and -not $callArguments.ContainsKey('AsOfUtc') -and -not $callArguments.ContainsKey('PacketInput')
    $entry = $null
    $dependencies = @()
    if ($cacheable) {
        $workspace = [string]$callArguments.WorkspacePath
        if ([IO.Path]::IsPathRooted($workspace) -or $workspace.Replace('\', '/') -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') { throw 'Assessment cache requires an owned task workspace.' }
        # Include ignored task evidence, including the bytes of execution logs.
        # Git's content snapshot supplies product, policy, tool and index inputs.
        $unconsumedOutputs = @()
        if ($ToolName -ne 'Manage-LlmWikiTaskWorkspace.ps1') {
            if ($ToolName -ne 'Manage-LlmWikiChangeCritique.ps1' -or $action -eq 'assess') { $unconsumedOutputs += 'change-critique.json' }
            if ($ToolName -ne 'Manage-LlmWikiChangeCritique.ps1' -and ($ToolName -ne 'Manage-LlmWikiConfidenceLedger.ps1' -or $action -eq 'assess')) { $unconsumedOutputs += 'confidence-ledger.json' }
        }
        $dependencies = @(Get-ChildItem -LiteralPath (Join-Path $repository $workspace) -File -Recurse | Where-Object { $_.Name -notmatch '^\.refresh-' -and $_.Name -notin $unconsumedOutputs } | ForEach-Object { $_.FullName.Substring($repository.Length + 1).Replace('\', '/') })
        foreach ($relativeRoot in @('.llm-wiki/knowledge', '.artifacts/llm-wiki/scheduler/quality-adjustments', '.artifacts/llm-wiki/scheduler/context-feedback', '.artifacts/llm-wiki/scheduler/dispatches')) {
            $absoluteRoot = Join-Path $repository $relativeRoot
            if (Test-Path -LiteralPath $absoluteRoot) { $dependencies += @(Get-ChildItem -LiteralPath $absoluteRoot -File -Filter '*.json' | ForEach-Object { $_.FullName.Substring($repository.Length + 1).Replace('\', '/') }) }
        }
        $evidenceFile = Join-Path $repository "$workspace/evidence.json"
        if (Test-Path -LiteralPath $evidenceFile) {
            $evidence = Get-Content -LiteralPath $evidenceFile -Raw | ConvertFrom-Json
            if ($evidence.PSObject.Properties['artifacts']) {
                foreach ($artifact in @($evidence.artifacts)) {
                    if ($null -ne $artifact -and $artifact.PSObject.Properties['path']) { $dependencies += [string]$artifact.path }
                }
            }
            $items = @($evidence.checks) + @($evidence.reviews)
            if ($evidence.PSObject.Properties['supplementalChecks']) { $items += @($evidence.supplementalChecks) }
            foreach ($item in $items) {
                if ($null -ne $item -and $item.PSObject.Properties['lineage'] -and $null -ne $item.lineage -and $item.lineage.PSObject.Properties['artifacts']) {
                    foreach ($artifact in @($item.lineage.artifacts)) {
                        if ($null -ne $artifact -and $artifact.PSObject.Properties['path']) { $dependencies += [string]$artifact.path }
                    }
                }
            }
        }
        $gitDirectory = (Invoke-LlmWikiGitCommand -RepositoryRoot $repository -Arguments @('rev-parse', '--absolute-git-dir')).Lines[0].Trim()
        $telemetryPath = if ($env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH) { $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH } else { Join-Path $gitDirectory 'llm-wiki/verification-telemetry.json' }
        if (-not [IO.Path]::IsPathRooted($telemetryPath)) { $telemetryPath = Join-Path $repository $telemetryPath }
        $configuration = @(Get-ChildItem Env: | Where-Object Name -like 'LLM_WIKI_*' | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join "`n"
        $environment = [ordered]@{ os = [Runtime.InteropServices.RuntimeInformation]::OSDescription; architecture = [string][Runtime.InteropServices.RuntimeInformation]::OSArchitecture; dotnet = [string](& dotnet --version); npm = [string](& npm --version); node = [string](& node --version); telemetryPath = $telemetryPath; telemetryHash = Get-LlmWikiFileSha256 $telemetryPath; configurationHash = Get-LlmWikiSha256 $(if ($configuration) { $configuration } else { '<none>' }) }
        $cacheArguments = @{ tool = $ToolName; arguments = $argumentIdentity; environment = $environment }
        $entry = Get-LlmWikiQueryCacheEntry -RepositoryRoot $repository -Namespace 'task-assessments' -Arguments $cacheArguments -DependencyPath $dependencies
        $cached = Read-LlmWikiQueryCache $entry
        if ($null -ne $cached) {
            try {
                $envelope = $cached | ConvertFrom-Json
                if ($envelope.inputFingerprint -ceq $entry.fingerprint -and $envelope.outputHash -ceq (Get-LlmWikiSha256 $envelope.outputJson)) {
                    $replayed = $envelope.outputJson | ConvertFrom-Json
                    Write-Host "Task stage: $Label — reused successful assessment; source, policy, runtime and evidence hashes unchanged."
                    return $replayed
                }
            } catch { }
        }
    }
    $runId = [guid]::NewGuid().ToString('N')
    $runRoot = Join-Path $repository ".artifacts/llm-wiki/assessment-runs/$runId"
    $null = [IO.Directory]::CreateDirectory($runRoot)
    $argumentsPath = Join-Path $runRoot 'arguments.json'
    $outputPath = Join-Path $runRoot 'output.json'
    $resultPath = Join-Path $runRoot 'result.json'
    $logPath = Join-Path $runRoot 'stage.log'
    $serializable = @{}
    foreach ($key in $callArguments.Keys) { $serializable[$key] = if ($callArguments[$key] -is [Management.Automation.SwitchParameter]) { [bool]$callArguments[$key] } else { $callArguments[$key] } }
    [IO.File]::WriteAllText($argumentsPath, ($serializable | ConvertTo-Json -Depth 30 -Compress), [Text.UTF8Encoding]::new($false))
    $reason = if ($null -ne $entry) { $entry.missReason } else { 'execution required' }
    Write-Host "Task stage: $Label — starting ($reason). Log: $logPath"
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = (Get-Process -Id $PID).Path
    $startInfo.WorkingDirectory = $repository
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $worker = Join-Path $PSScriptRoot 'Invoke-LlmWikiObservedStage.ps1'
    $startInfo.Arguments = "-NoLogo -NoProfile -File `"$worker`" -ToolPath `"$toolPath`" -ArgumentsPath `"$argumentsPath`" -StageName `"$ToolName`" -LogPath `"$logPath`" -ResultPath `"$resultPath`" -OutputPath `"$outputPath`""
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    try {
        if (-not $process.Start()) { throw "Unable to start $Label." }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $nextHeartbeat = $HeartbeatSeconds
        while (-not $process.WaitForExit(250)) {
            if ($stopwatch.Elapsed.TotalSeconds -ge $nextHeartbeat) {
                $lastProgress = @(Get-Content -LiteralPath $logPath -Tail 8 -ErrorAction SilentlyContinue | Where-Object { $_ -match '^Task stage:' } | Select-Object -Last 1) -join ''
                Write-Host "Task stage: $Label — running, $([int]$stopwatch.Elapsed.TotalSeconds)s elapsed; waiting for worker. $lastProgress Log: $logPath"
                $nextHeartbeat += $HeartbeatSeconds
            }
            if ($stopwatch.Elapsed.TotalSeconds -ge $TimeoutSeconds) { throw "Task stage timed out: $Label after ${TimeoutSeconds}s. Log: $logPath" }
        }
        $outputText = $stdout.GetAwaiter().GetResult()
        $errorText = $stderr.GetAwaiter().GetResult()
        $stageResult = if (Test-Path -LiteralPath $resultPath) { Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json } else { $null }
        if ($process.ExitCode -ne 0 -or $null -eq $stageResult -or $stageResult.status -ne 'passed' -or -not (Test-Path -LiteralPath $outputPath)) {
            throw "Task stage failed: $Label. $($stageResult.detail) $errorText Log: $logPath"
        }
        $json = [IO.File]::ReadAllText($outputPath)
        $result = $json | ConvertFrom-Json
        $successful = $null -ne $result -and (($result.PSObject.Properties['valid'] -and $result.valid -eq $true) -or ($result.PSObject.Properties['verdict'] -and $result.verdict -eq 'ready'))
        foreach ($member in @('critique', 'ledger')) {
            if ($null -ne $result -and $result.PSObject.Properties[$member] -and $result.$member.verdict -in @('blocked', 'reject', 'request-changes')) { $successful = $false }
        }
        if ($null -ne $entry -and $successful) {
            $environment.telemetryHash = Get-LlmWikiFileSha256 $telemetryPath
            $after = Get-LlmWikiQueryCacheEntry -RepositoryRoot $repository -Namespace 'task-assessments' -Arguments $cacheArguments -DependencyPath $dependencies
            if ($after.fingerprint -cne $entry.fingerprint) { throw "Assessment inputs changed while $Label was running; discard this result and rerun the stage. Log: $logPath" }
            $envelope = @{ inputFingerprint = $entry.fingerprint; outputHash = Get-LlmWikiSha256 $json; outputJson = $json } | ConvertTo-Json -Compress
            Write-LlmWikiQueryCache -Entry $entry -Content $envelope
        }
        Write-Host "Task stage: $Label — completed in $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 1))s; successful=$successful."
        return $result
    } finally {
        if ($null -ne $process -and $process.Id -gt 0 -and -not $process.HasExited) {
            if ($PSVersionTable.PSVersion.Major -ge 7) { $process.Kill($true) } else { $process.Kill() }
        }
        $process.Dispose()
        [IO.File]::Delete($argumentsPath)
    }
}
