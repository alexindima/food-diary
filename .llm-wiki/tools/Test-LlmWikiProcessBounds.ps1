[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'process-bounds' -IndependentRepository
$process = $null
try {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = (Get-Process -Id $PID).Path; $start.UseShellExecute = $false; $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
    $rootScript = Join-Path $fixture 'root.ps1'
    '$start=[Diagnostics.ProcessStartInfo]::new();$start.FileName="node";$start.Arguments=''-e "setTimeout(()=>{},10000)"'';$start.UseShellExecute=$false;$start.CreateNoWindow=$true;$null=[Diagnostics.Process]::Start($start)' | Set-Content $rootScript
    foreach ($arg in @('-NoProfile','-File',$rootScript)) { $start.ArgumentList.Add($arg) }
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
    $timer = [Diagnostics.Stopwatch]::StartNew(); $failure = ''
    try { Wait-LlmWikiGitProcess $process $stdout $stderr 1 } catch { $failure = $_.Exception.Message }
    if ($failure -notlike '*timed out*' -or $timer.Elapsed.TotalSeconds -gt 5) { throw "Git output-drain deadline did not bound an exited root with an inherited pipe: elapsed=$($timer.Elapsed.TotalSeconds); error=$failure" }
    $process.Dispose(); $process = $null

    $tools = Join-Path $fixture '.llm-wiki/tools'; $null=[IO.Directory]::CreateDirectory($tools)
    $policies = Join-Path $fixture '.llm-wiki/policies'; $null=[IO.Directory]::CreateDirectory($policies)
    foreach ($file in @('Invoke-LlmWikiParallelSmoke.ps1','Invoke-LlmWikiObservedStage.ps1','LlmWikiProcess.ps1','LlmWikiJson.ps1','LlmWikiGitPaths.ps1','LlmWikiSmokeScheduling.ps1')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $tools }
    "@{ SchemaVersion=1;Groups=@(@{Id='fixture';IncludeInAll=`$true;ParallelSafe=`$true;Priority=0;GraphDependent=`$false}) }" | Set-Content (Join-Path $policies 'affected-smoke-catalog.psd1')
    'param($BaseRef,$ChangedPath,$RequestedGroup,[switch]$NoCache); Start-Sleep -Seconds 30' | Set-Content (Join-Path $tools 'Invoke-LlmWikiAffectedSmoke.ps1')
    $start = [Diagnostics.ProcessStartInfo]::new(); $start.FileName=(Get-Process -Id $PID).Path
    $start.UseShellExecute=$false; $start.CreateNoWindow=$true; $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
    foreach ($arg in @('-NoProfile','-File',(Join-Path $tools 'Invoke-LlmWikiParallelSmoke.ps1'),'-AllGroups','-NoCache','-GroupTimeoutSeconds','1','-CancellationGraceSeconds','1')) { $start.ArgumentList.Add($arg) }
    $process=[Diagnostics.Process]::Start($start); $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
    Wait-LlmWikiGitProcess $process $stdout $stderr 20
    $text=$stdout.GetAwaiter().GetResult()+$stderr.GetAwaiter().GetResult()
    if ($process.ExitCode -eq 0 -or $text -notmatch 'Focused smoke group timed out') { throw "Focused smoke did not enforce its group deadline: $text" }
    Write-Host 'Process bounds passed: inherited Git pipes, focused group deadline, failed receipt and owned-worker cleanup.'
} finally {
    if ($null -ne $process) { try { Stop-LlmWikiProcessTree $process } finally { $process.Dispose() } }
    if (-not [IO.Path]::GetFileName($fixture).StartsWith('process-bounds-', [StringComparison]::Ordinal)) { throw 'Unowned process bounds fixture.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
