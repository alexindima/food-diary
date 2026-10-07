[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'scheduler-locks'
$managers = [ordered]@{
    'Manage-LlmWikiTaskLease.ps1' = '.lease-lock'
    'Manage-LlmWikiAgentRegistry.ps1' = '.agent-lock'
    'Manage-LlmWikiSchedulePlan.ps1' = '.schedule-plan-lock'
    'Manage-LlmWikiOrchestrationCycle.ps1' = '.orchestration-cycle-lock'
    'Manage-LlmWikiDispatchWatchdog.ps1' = '.watchdog-lock'
    'Manage-LlmWikiWorkspaceCircuit.ps1' = '.circuit-lock'
    'Manage-LlmWikiTaskDecomposition.ps1' = '.decomposition-lock'
}
try {
    $tools = Join-Path $fixture '.llm-wiki/tools'
    $policies = Join-Path $fixture '.llm-wiki/policies'
    $scheduler = Join-Path $fixture '.artifacts/llm-wiki/scheduler'
    $null = New-Item -ItemType Directory -Path $tools, $policies, $scheduler -Force
    foreach ($name in @($managers.Keys) + 'Get-LlmWikiWorkspacePolicy.ps1') {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $tools
    }
    foreach ($name in @('workspace-policies.json', 'change-policies.json')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot "../policies/$name") -Destination $policies
    }
    $holderScript = Join-Path $fixture 'holder.ps1'
    [IO.File]::WriteAllText($holderScript, @'
param($Manager, $Scheduler, $LockPath, $Signal)
$ErrorActionPreference = 'Stop'
$ast = [Management.Automation.Language.Parser]::ParseFile($Manager, [ref]$null, [ref]$null)
$guard = $ast.Find({ param($node)
    $node -is [Management.Automation.Language.IfStatementAst] -and
        $node.Clauses[0].Item1.Extent.Text -eq '$mutating' -and $node.Extent.Text.Contains('$lockStream')
}, $false)
if (-not $guard) { throw 'Actual manager lock acquisition was not found.' }
$schedulerRoot = $Scheduler; $schedulerPath = $Scheduler; $mutating = $true; $now = [DateTime]::UtcNow
$lockStream = $null
. ([scriptblock]::Create($guard.Extent.Text))
[IO.File]::WriteAllText($Signal, 'held')
# The parent terminates this owned holder to exercise OS lease release on crash.
$signalWait = [Threading.ManualResetEvent]::new($false)
$null = $signalWait.WaitOne(30000)
exit 7
'@, [Text.UTF8Encoding]::new($false))

    foreach ($entry in $managers.GetEnumerator()) {
        $manager = Join-Path $tools $entry.Key
        $lock = Join-Path $scheduler $entry.Value
        # A dead writer can leave a newly created file. Age is not ownership.
        [IO.File]::WriteAllText($lock, 'stable-lock-identity')
        try { $null = & $manager -Action prune -Format Json | ConvertFrom-Json }
        catch { throw "Fresh orphan lock blocks $($entry.Key): $($_.Exception.Message)" }
        if (-not (Test-Path -LiteralPath $lock) -or [IO.File]::ReadAllText($lock) -cne 'stable-lock-identity') {
            throw "Manager replaced or unlinked its stable lock: $($entry.Key)"
        }
        foreach ($ageMinutes in @(0, 20)) {
            [IO.File]::SetLastWriteTimeUtc($lock, [DateTime]::UtcNow.AddMinutes(-$ageMinutes))
            $held = [IO.File]::Open($lock, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            try {
                $failure = $null
                try { $null = & $manager -Action prune -Format Json } catch { $failure = $_.Exception.Message }
                if ($failure -notmatch 'busy|already running|already being mutated') { throw "A live lock was not rejected normally by $($entry.Key): $failure" }
                $held.Position = 0
                $bytes = [byte[]]::new([int]$held.Length)
                $null = $held.Read($bytes, 0, $bytes.Length)
                if (-not (Test-Path -LiteralPath $lock) -or [Text.Encoding]::UTF8.GetString($bytes) -cne 'stable-lock-identity') { throw 'A live lock identity changed.' }
            } finally { $held.Dispose() }
            $null = & $manager -Action prune -Format Json | ConvertFrom-Json
        }
        $signal = Join-Path $fixture 'holder.signal'
        Remove-Item -LiteralPath $signal -Force -ErrorAction SilentlyContinue
        $start = [Diagnostics.ProcessStartInfo]::new()
        $start.FileName = (Get-Process -Id $PID).Path
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $start.Arguments = "-NoLogo -NoProfile -File `"$holderScript`" -Manager `"$manager`" -Scheduler `"$scheduler`" -LockPath `"$lock`" -Signal `"$signal`""
        $child = [Diagnostics.Process]::new()
        $child.StartInfo = $start
        try {
            $null = $child.Start()
            $output = $child.StandardOutput.ReadToEndAsync()
            $errorOutput = $child.StandardError.ReadToEndAsync()
            $deadline = [DateTime]::UtcNow.AddSeconds(10)
            while (-not (Test-Path -LiteralPath $signal)) {
                if ($child.HasExited -or [DateTime]::UtcNow -ge $deadline) { throw "Owned lock holder failed: $($entry.Key)" }
                Start-Sleep -Milliseconds 25
            }
            $heldFailure = $null
            try { $null = & $manager -Action prune -Format Json } catch { $heldFailure = $_.Exception.Message }
            if ($heldFailure -notmatch 'busy|already running|already being mutated') { throw 'The child did not hold the actual exclusive manager lease.' }
            $child.Kill()
            if (-not $child.WaitForExit(5000)) { throw 'Owned lock holder did not terminate.' }
            $null = $output.GetAwaiter().GetResult()
            $null = $errorOutput.GetAwaiter().GetResult()
            $null = & $manager -Action prune -Format Json | ConvertFrom-Json
            if (-not (Test-Path -LiteralPath $lock)) { throw 'Crash recovery unlinked the stable coordination file.' }
        } finally {
            if (-not $child.HasExited) { $child.Kill(); $null = $child.WaitForExit(5000) }
            $child.Dispose()
        }
    }
    Write-Host 'Scheduler locks passed: seven managers recover fresh orphans and killed holders, reject recent/old live leases, and preserve stable lock identity.'
} finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    $parent = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot)).TrimEnd('\', '/')
    if ([IO.Path]::GetDirectoryName($resolved) -cne $parent -or [IO.Path]::GetFileName($resolved) -notmatch '^scheduler-locks-[a-f0-9]{32}$') { throw 'Unsafe scheduler lock fixture cleanup.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
