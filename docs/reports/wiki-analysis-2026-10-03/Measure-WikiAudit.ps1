[CmdletBinding()]
param([int]$Iterations = 3, [string]$OutputDirectory = '.artifacts/wiki-analysis-replay')

$ErrorActionPreference = 'Stop'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$auditOutput = Join-Path $repositoryRoot $OutputDirectory
New-Item -ItemType Directory -Force $auditOutput | Out-Null
$shellPath = (Get-Process -Id $PID).Path
$cases = @(
    @{ name = 'process-empty'; arguments = @('-Command', 'exit 0') },
    @{ name = 'facade-help'; arguments = @('-File', '.llm-wiki/wiki.ps1', 'help') },
    @{ name = 'context-json-direct'; arguments = @('-File', '.llm-wiki/tools/Find-LlmWikiContext.ps1', '-Query', 'Hydration daily water goal', '-Module', 'Hydration', '-ChangeType', 'Backend', '-CompiledIndexSource', 'Json', '-Format', 'Json') },
    @{ name = 'context-json-facade'; arguments = @('-File', '.llm-wiki/wiki.ps1', 'context', '-Query', 'Hydration daily water goal', '-Module', 'Hydration', '-ChangeType', 'Backend', '-CompiledIndexSource', 'Json', '-Format', 'Json', '-Verbose') },
    @{ name = 'context-sqlite-direct'; arguments = @('-File', '.llm-wiki/tools/Find-LlmWikiContext.ps1', '-Query', 'Hydration daily water goal', '-Module', 'Hydration', '-ChangeType', 'Backend', '-Format', 'Json') },
    @{ name = 'context-sqlite-facade'; arguments = @('-File', '.llm-wiki/wiki.ps1', 'context', '-Query', 'Hydration daily water goal', '-Module', 'Hydration', '-ChangeType', 'Backend', '-Format', 'Json', '-Verbose') },
    @{ name = 'graph-no-change'; arguments = @('-File', '.llm-wiki/tools/Manage-LlmWikiCodeGraph.ps1', '-Action', 'build', '-Format', 'Json') },
    @{ name = 'brief-json-facade'; arguments = @('-File', '.llm-wiki/wiki.ps1', 'brief', '-Intent', 'Assess wiki tooling latency and caches', '-PlannedPath', '.llm-wiki/wiki.ps1', '-CompiledIndexSource', 'Json', '-SkipTestPlan', '-Compact', '-Format', 'Json', '-Verbose') },
    @{ name = 'research-json-facade'; arguments = @('-File', '.llm-wiki/wiki.ps1', 'research', '-Intent', 'Assess wiki tooling latency and caches', '-ResearchPurpose', 'Assessment', '-PlannedPath', '.llm-wiki/wiki.ps1', '-CompiledIndexSource', 'Json', '-SkipHistory', '-Compact', '-Format', 'Json', '-Verbose') }
)
$results = [Collections.Generic.List[object]]::new()
Push-Location $repositoryRoot
try {
    foreach ($case in $cases) {
        for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
            $sw = [Diagnostics.Stopwatch]::StartNew()
            $output = & $shellPath -NoLogo -NoProfile @($case.arguments) 2>&1
            $exitCode = $LASTEXITCODE
            $sw.Stop()
            $raw = $output -join [Environment]::NewLine
            $logPath = Join-Path $auditOutput "$($case.name)-$iteration.log"
            [IO.File]::WriteAllText($logPath, $raw, [Text.UTF8Encoding]::new($false))
            $row = [pscustomobject]@{ name=$case.name; iteration=$iteration; milliseconds=[Math]::Round($sw.Elapsed.TotalMilliseconds,2); exitCode=$exitCode; outputCharacters=$raw.Length; log=[IO.Path]::GetFileName($logPath) }
            $results.Add($row)
            Write-Host "$($row.name) iteration=$iteration milliseconds=$($row.milliseconds) exit=$exitCode"
            $results | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $auditOutput 'command-measurements.json') -Encoding utf8
        }
    }
} finally { Pop-Location }
