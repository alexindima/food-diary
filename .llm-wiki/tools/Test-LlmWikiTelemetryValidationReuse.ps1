[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'telemetry-validation'
$fixtureTools = Join-Path $fixture '.llm-wiki/tools'
$fixturePolicies = Join-Path $fixture '.llm-wiki/policies'
$managerPath = Join-Path $fixtureTools 'Manage-LlmWikiVerificationTelemetry.ps1'
$policyPath = Join-Path $fixturePolicies 'workspace-policies.json'
$registryPath = Join-Path $fixture 'registry.json'
$counterPath = Join-Path $fixture 'validation-count.txt'
$previousCache = Get-Variable -Name LlmWikiTelemetryValidation -Scope Global -ValueOnly -ErrorAction SilentlyContinue

function Get-ValidationCount { @([IO.File]::ReadAllLines($counterPath)).Count }
function Invoke-Fixture([string]$Action = 'verify', [string]$Path = $registryPath) {
    & $managerPath $Action -RegistryPath $Path -Format Json | ConvertFrom-Json
}
function Assert-ValidationCount([int]$Expected, [string]$Context) {
    if ((Get-ValidationCount) -ne $Expected) { throw "Telemetry validation count differs for $Context." }
}
try {
    $null = New-Item -ItemType Directory -Path $fixtureTools, $fixturePolicies -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1') -Destination $fixtureTools
    $policy = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../policies/workspace-policies.json') -Raw | ConvertFrom-Json
    $policy.scheduler.verificationPlanner.failurePrediction.costModel.telemetry.retentionCount = 2
    $policyText = $policy | ConvertTo-Json -Depth 100
    [IO.File]::WriteAllText($policyPath, $policyText)
    $source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1'))
    $counterLiteral = $counterPath.Replace("'", "''")
    $instrumented = $source.Replace('function Test-Registry([object]$Registry) {', "function Test-Registry([object]`$Registry) {`n    [IO.File]::AppendAllText('$counterLiteral', 'validation' + [Environment]::NewLine)")
    if ($instrumented -ceq $source) { throw 'Cannot instrument the real telemetry validator.' }
    [IO.File]::WriteAllText($managerPath, $instrumented)
    Remove-Variable -Name LlmWikiTelemetryValidation -Scope Global -ErrorAction SilentlyContinue

    $empty = Invoke-Fixture
    if (-not $empty.valid -or $empty.totalCount -ne 0) { throw 'Missing telemetry registry was not initialized.' }
    foreach ($action in @('verify', 'list', 'metrics', 'verify')) {
        if (-not (Invoke-Fixture $action).valid) { throw 'Unchanged telemetry input became invalid.' }
    }
    Assert-ValidationCount 1 'unchanged public actions'

    foreach ($check in @('sample-one', 'sample-two')) {
        & $managerPath record -RegistryPath $registryPath -CheckId $check -Status passed -DurationSeconds 1 -Format Json | Out-Null
    }
    $valid = Invoke-Fixture
    if (-not $valid.valid -or $valid.totalCount -ne 2) { throw 'Telemetry writes were not revalidated.' }
    Assert-ValidationCount 3 'successful writes'
    1..3 | ForEach-Object {
        $rejected = $false
        try { & $managerPath record -RegistryPath $registryPath -CheckId full -Status passed -Format Json | Out-Null }
        catch {
            $rejected = $_.Exception.Message -like '*retention limit is reached*' -and
                $_.FullyQualifiedErrorId.Split(',')[0] -ceq 'LlmWikiTelemetryRetentionReached'
        }
        if (-not $rejected) { throw 'Full telemetry history was silently overwritten or accepted.' }
    }
    Assert-ValidationCount 3 'repeated retention rejection'

    $validText = [IO.File]::ReadAllText($registryPath)
    $stamp = [IO.File]::GetLastWriteTimeUtc($registryPath)
    $tamperedText = $validText.Replace('sample-one', 'sample-ONE')
    if ($tamperedText -ceq $validText -or $tamperedText.Length -ne $validText.Length) { throw 'Invalid tamper fixture.' }
    [IO.File]::WriteAllText($registryPath, $tamperedText)
    [IO.File]::SetLastWriteTimeUtc($registryPath, $stamp)
    $invalid = Invoke-Fixture
    if ($invalid.valid -or @($invalid.issues).Count -lt 2) { throw 'Same-size, same-time case-only tampering was not rejected.' }
    $invalid.issues[0] = 'caller-mutated-result'
    $again = Invoke-Fixture
    if ($again.valid -or @($again.issues) -contains 'caller-mutated-result') { throw 'Caller mutation poisoned cached validation.' }
    Assert-ValidationCount 4 'tampering and detached invalid result'
    [IO.File]::WriteAllText($registryPath, $validText)
    if (-not (Invoke-Fixture).valid) { throw 'Restored valid history did not recover.' }
    Assert-ValidationCount 5 'restored registry'

    $policy.scheduler.verificationPlanner.failurePrediction.costModel.telemetry.retentionCount = 1
    [IO.File]::WriteAllText($policyPath, ($policy | ConvertTo-Json -Depth 100))
    if ((Invoke-Fixture).valid) { throw 'Policy change did not invalidate telemetry validation.' }
    Assert-ValidationCount 6 'policy change'
    [IO.File]::WriteAllText($policyPath, $policyText)
    if (-not (Invoke-Fixture).valid) { throw 'Restored policy did not recover.' }
    Assert-ValidationCount 7 'restored policy'
    [IO.File]::AppendAllText($managerPath, "`n# fixture implementation change`n")
    if (-not (Invoke-Fixture).valid) { throw 'Changed validator did not preserve public result.' }
    Assert-ValidationCount 8 'validator change'

    $otherPath = Join-Path $fixture 'other-registry.json'
    [IO.File]::WriteAllText($otherPath, $validText)
    if (-not (Invoke-Fixture -Path $otherPath).valid -or -not (Invoke-Fixture).valid) { throw 'Alternate registry failed validation.' }
    Assert-ValidationCount 10 'registry path isolation and one-entry bound'
    [IO.File]::WriteAllText($registryPath, '{broken JSON')
    $rejected = $false
    try { $null = Invoke-Fixture } catch { $rejected = $true }
    if (-not $rejected) { throw 'Malformed JSON reused a valid result.' }
    [IO.File]::WriteAllText($registryPath, $validText)
    if (-not (Invoke-Fixture).valid) { throw 'Valid JSON did not recover.' }
    Assert-ValidationCount 10 'malformed JSON and exact restoration'

    Remove-Item -LiteralPath $registryPath
    $reinitialized = Invoke-Fixture
    if (-not $reinitialized.valid -or $reinitialized.totalCount -ne 0) { throw 'Archived history did not reinitialize.' }
    Assert-ValidationCount 11 'missing registry after archive'
    & $managerPath record -RegistryPath $registryPath -CheckId resumed -Status passed -DurationSeconds 1 -Format Json | Out-Null
    $resumed = Invoke-Fixture
    if (-not $resumed.valid -or $resumed.totalCount -ne 1) { throw 'Recording did not resume after archive.' }
    Assert-ValidationCount 12 'recording after archive'
    Write-Host 'LLM Wiki telemetry validation reuse passed: exact inputs, tampering, policy, implementation, path, retention and recovery.'
} finally {
    if ($null -eq $previousCache) { Remove-Variable -Name LlmWikiTelemetryValidation -Scope Global -ErrorAction SilentlyContinue }
    else { $global:LlmWikiTelemetryValidation = $previousCache }
    $sandboxRoot = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot)).TrimEnd('\', '/')
    $ownedPath = [IO.Path]::GetFullPath($fixture)
    if (-not $ownedPath.StartsWith($sandboxRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Telemetry fixture cleanup escaped its sandbox.' }
    Remove-Item -LiteralPath $ownedPath -Recurse -Force
}
