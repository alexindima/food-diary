[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeScheduling.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$root = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) -Name 'smoke-scheduling' -IndependentRepository
try {
    for ($i=1; $i -le 7; $i++) {
        $path = Join-Path $root "$i.timings.json"
        @{ completed=$true; maxConcurrency=4; groups=@(@{group='long';durationSeconds=$i*10;exitCode=0;executed=$true},@{group='short';durationSeconds=2;exitCode=0;executed=$true},@{group='cached';durationSeconds=1;exitCode=0;executed=$false},@{group='failed';durationSeconds=100;exitCode=1}) } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path
        [IO.File]::SetLastWriteTimeUtc($path, [DateTime]::UtcNow.AddMinutes($i))
    }
    @{ completed=$false; maxConcurrency=4; groups=@(@{group='long';durationSeconds=900;exitCode=0}) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $root 'incomplete.timings.json')
    @{ completed=$true; maxConcurrency=1; groups=@(@{group='long';durationSeconds=900;exitCode=0}) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $root 'serial.timings.json')
    '{"truncated":' | Set-Content -LiteralPath (Join-Path $root 'corrupt.timings.json')
    $estimates = Get-LlmWikiSmokeDurationEstimates -HistoryRoot $root -MaxConcurrency 4
    if ($estimates.long -ne 50 -or $estimates.short -ne 2 -or $estimates.ContainsKey('cached') -or $estimates.ContainsKey('failed')) { throw 'History must use the last five successful executed samples at the same concurrency.' }
    $priority = @{long=1;short=100;new=200}
    if ((Get-LlmWikiSmokeSchedule -Groups @('short','long') -Priority $priority -Duration $estimates) -join ',' -cne 'long,short') { throw 'Independent measured groups must run longest first.' }
    if ((Get-LlmWikiSmokeSchedule -Groups @('short','long','new') -Priority $priority -Duration $estimates) -join ',' -cne 'new,short,long') { throw 'Incomplete history must retain catalog order.' }
    if (@(Get-LlmWikiSmokeSchedule -Groups @() -Priority $priority -Duration $estimates).Count -ne 0) { throw 'Empty lanes must remain empty.' }
    Write-Host 'Smoke scheduling passed: bounded median history, concurrency, failure/cache exclusion, incomplete history and deterministic ordering.'
} finally {
    $resolved = [IO.Path]::GetFullPath($root)
    $expected = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) -IndependentRepository)).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($expected,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe scheduling fixture cleanup.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
