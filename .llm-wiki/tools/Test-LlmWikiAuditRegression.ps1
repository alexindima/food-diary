[CmdletBinding()]
param([ValidateSet('All', 'Dependencies', 'Assembly', 'Time', 'Unicode', 'ReadOnly', 'OutputLimit', 'Comparison', 'Session', 'Mutex')][string]$Case = 'All')

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
function Assert-Audit([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Wiki audit regression: $Message" }
}
function Copy-AuditTools([string]$Root, [string[]]$Names) {
    $directory = Join-Path $Root '.llm-wiki/tools'
    $null = [IO.Directory]::CreateDirectory($directory)
    foreach ($name in $Names) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $directory }
    return $directory
}
function Initialize-AuditGit([string]$Root) {
    $null = & git -C $Root init --quiet
    $null = & git -C $Root -c core.autocrlf=false add .
    $null = & git -C $Root -c user.name=WikiFixture -c user.email=wiki-fixture@example.invalid commit --quiet -m baseline
    Assert-Audit ($LASTEXITCODE -eq 0) 'Private fixture baseline must commit successfully.'
}
$cases = if ($Case -eq 'All') { @('Dependencies', 'Assembly', 'Time', 'Unicode', 'ReadOnly', 'OutputLimit', 'Comparison', 'Session', 'Mutex') } else { @($Case) }
foreach ($scenario in $cases) {
    $fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name "audit-$scenario" -IndependentRepository
    try {
        switch ($scenario) {
            'Comparison' {
                $tools = Copy-AuditTools $fixture @('Get-LlmWikiReviewReport.ps1', 'Get-LlmWikiReleaseReadiness.ps1', 'LlmWikiChangePacket.ps1', 'LlmWikiJson.ps1', 'LlmWikiSubstantiveReview.ps1')
                @'
param($BaseRef,$HeadRef,$ChangedPath,$Objective,$Format)
@{fingerprint='fixture';inputs=@{objective='fixture';baseRef=$BaseRef};diff=@{changedPaths=@($ChangedPath);scopes=@('Api');modules=@()};policy=@{violations=@();matchedRules=@();requiredChecks=@();reviewObligations=@()};brief=@{risk=@{level='low'};architectureHealthImpact=@{dependencyViolations=@();untrackedProductionProjects=@();moduleCycleNodes=@()};privacyImpact=@{fields=@();boundaries=@();potentialLogging=@()}};rollout=@{flags=@{}};testPlan=@{commands=@();scenarios=@()}} | ConvertTo-Json -Depth 12
'@ | Set-Content (Join-Path $tools 'Get-LlmWikiChangePacket.ps1')
                @'
param($BaseRef='HEAD',$Format)
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'observed-base.txt'),$BaseRef)
'{"breakingCount":0,"breakingChanges":[],"behavioralRestrictions":[]}'
'@ | Set-Content (Join-Path $tools 'Test-LlmWikiApiCompatibility.ps1')
                $reportTool = Join-Path $tools 'Get-LlmWikiReviewReport.ps1'
                $report = & $reportTool -BaseRef 'selected-base' -ChangedPath @('Modules/Probe/Presentation/ProbeController.cs') -Format Json | ConvertFrom-Json
                Assert-Audit (($report.dimensions | Where-Object id -eq 'api-compatibility').status -ceq 'not-assessed') 'Synthetic scope must remain not-assessed through review reports.'
                $report = & $reportTool -BaseRef 'selected-base' -Format Json | ConvertFrom-Json
                Assert-Audit ([IO.File]::ReadAllText((Join-Path $tools 'observed-base.txt')) -ceq 'selected-base') 'Review readiness must compare the requested baseline.'
                $report = & $reportTool -BaseRef 'selected-base' -HeadRef 'selected-head' -Format Json | ConvertFrom-Json
                Assert-Audit (($report.dimensions | Where-Object id -eq 'api-compatibility').status -ceq 'not-assessed') 'Unsupported committed-head API comparison must remain not-assessed.'
            }
            'Session' {
                $tools = Copy-AuditTools $fixture @('Manage-LlmWikiTaskBaseline.ps1', 'Resolve-LlmWikiSession.ps1', 'LlmWikiJson.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiProcess.ps1')
                $null = [IO.Directory]::CreateDirectory((Join-Path $fixture 'owned'))
                '.artifacts/' | Set-Content (Join-Path $fixture '.gitignore')
                'baseline' | Set-Content (Join-Path $fixture 'owned/value.txt')
                Initialize-AuditGit $fixture
                $tool = Join-Path $tools 'Manage-LlmWikiTaskBaseline.ps1'
                $capture = & $tool -Action Capture -RepositoryRoot $fixture -SessionId bounded-task -PlannedPath owned -Objective 'Keep task scope'
                'task edit' | Set-Content (Join-Path $fixture 'owned/value.txt')
                'foreign edit' | Set-Content (Join-Path $fixture 'foreign.txt')
                $registryPath = Join-Path $fixture '.git/llm-wiki/sessions/registry.json'
                $registry = Get-Content $registryPath -Raw | ConvertFrom-Json
                $registry.sessions[0].createdAtUtc = [DateTime]::UtcNow.AddHours(-2).ToString('o')
                $registry.sessions[0].lastSeenAtUtc = $registry.sessions[0].createdAtUtc
                $registry | ConvertTo-Json -Depth 8 | Set-Content $registryPath
                $result = & $tool -Action ChangedPaths -RepositoryRoot $fixture -SessionId bounded-task
                Assert-Audit ($result.available -and 'owned/value.txt' -in $result.changedPaths -and 'foreign.txt' -in $result.excludedChangedPaths -and (Test-Path $capture.baselinePath)) 'Inactive bounded sessions must retain their existing baseline and exclusions.'
            }
            'Mutex' {
                $path = Join-Path $fixture 'observations.jsonl'
                $name = 'Local\LlmWikiContextObservation-' + [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($path.ToLowerInvariant())))
                $worker = Join-Path $fixture 'holder.ps1'
                @'
param($Name,$Ready)
$mutex=[Threading.Mutex]::new($false,$Name)
$null=$mutex.WaitOne()
[IO.File]::WriteAllText($Ready,'')
Start-Sleep -Seconds 30
'@ | Set-Content $worker
                $ready = Join-Path $fixture 'ready'
                $start = [Diagnostics.ProcessStartInfo]::new()
                $start.FileName = (Get-Process -Id $PID).Path
                $start.UseShellExecute = $false; $start.CreateNoWindow = $true
                foreach ($argument in @('-NoProfile', '-File', $worker, '-Name', $name, '-Ready', $ready)) { $start.ArgumentList.Add($argument) }
                $child = [Diagnostics.Process]::Start($start)
                $retained = $null
                try {
                    $timer = [Diagnostics.Stopwatch]::StartNew()
                    while (-not [IO.File]::Exists($ready)) {
                        if ($timer.Elapsed.TotalSeconds -ge 10 -or $child.HasExited) { throw 'Owned mutex holder did not become ready.' }
                        Start-Sleep -Milliseconds 20
                    }
                    $retained = [Threading.Mutex]::OpenExisting($name)
                    $child.Kill(); $null = $child.WaitForExit(5000)
                    $failure = ''
                    try { & (Join-Path $PSScriptRoot 'Write-LlmWikiContextQueryObservation.ps1') -DurationMs 1 -QueryTermCount 1 -CandidateCount 1 -OutputPath $path } catch { $failure = $_.Exception.Message }
                    Assert-Audit (-not $failure -and [IO.File]::Exists($path)) 'Observation logger must recover ownership of an abandoned mutex.'
                } finally {
                    if (-not $child.HasExited) { $child.Kill($true) }
                    $child.Dispose()
                    if ($retained) { $retained.Dispose() }
                }
            }
            'Dependencies' {
                $tools = Copy-AuditTools $fixture @('Get-LlmWikiDependencyChanges.ps1', 'LlmWikiDependencyManifest.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiProcess.ps1')
                $manifest = Join-Path $fixture 'Directory.Packages.props'
                '<Project><ItemGroup><PackageVersion Include="Fixture.Package" Version="1.0.0" /></ItemGroup></Project>' | Set-Content $manifest
                '<Project><ItemGroup><PackageReference Include="Fixture.Package" /></ItemGroup></Project>' | Set-Content (Join-Path $fixture 'Fixture.csproj')
                Initialize-AuditGit $fixture
                '<Project><ItemGroup><PackageVersion Include="Fixture.Package" Version="2.0.0" /></ItemGroup></Project>' | Set-Content $manifest
                $result = & (Join-Path $tools 'Get-LlmWikiDependencyChanges.ps1') -Format Json | ConvertFrom-Json
                $change = @($result.changes | Where-Object package -eq 'Fixture.Package')
                Assert-Audit ($change.Count -eq 1 -and $change[0].before -ceq '1.0.0' -and $change[0].after -ceq '2.0.0' -and $change[0].manifest -ceq 'Directory.Packages.props') 'Central package version changes must include the exact before and after versions.'
                $inventory = & (Join-Path $tools 'Get-LlmWikiDependencyChanges.ps1') -RepositoryWide -Format Json | ConvertFrom-Json
                Assert-Audit (@($inventory.inventory.manifests | Where-Object manifest -eq 'Directory.Packages.props').Count -eq 1) 'Repository inventory must include the central manifest.'
            }
            'Assembly' {
                $tools = Copy-AuditTools $fixture @('Get-LlmWikiContractConsumers.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiProcess.ps1')
                $projectDirectory = Join-Path $fixture 'Modules/Example/Contracts'
                $null = [IO.Directory]::CreateDirectory($projectDirectory)
                '<Project><PropertyGroup><AssemblyName>Fixture.Example.Contracts</AssemblyName></PropertyGroup></Project>' | Set-Content (Join-Path $projectDirectory 'Different.Project.Name.csproj')
                'public interface IFixtureContract { }' | Set-Content (Join-Path $projectDirectory 'IFixtureContract.cs')
                Initialize-AuditGit $fixture
                $result = & (Join-Path $tools 'Get-LlmWikiContractConsumers.ps1') -Contract IFixtureContract -Format Json | ConvertFrom-Json
                Assert-Audit ($result.owningAssembly -ceq 'Fixture.Example.Contracts') 'Nested contracts must report their project AssemblyName rather than the Modules folder.'
                Assert-Audit ($result.readiness.abstractionOwned -and @($result.readiness.blockers).Count -eq 0) 'A Contracts assembly must be recognized as a contract owner.'
            }
            'Time' {
                . (Join-Path $PSScriptRoot 'LlmWikiTaskAuditHelpers.ps1')
                $utc = [DateTime]::new(2026, 11, 7, 10, 20, 30, [DateTimeKind]::Utc)
                $fallback = [DateTime]::MinValue
                Assert-Audit ((Convert-ToUtc $utc.ToLocalTime() $fallback).Ticks -eq $utc.Ticks) 'Typed local timestamps must retain their instant.'
                Assert-Audit ((Convert-ToUtc ([DateTimeOffset]::new($utc)) $fallback).Ticks -eq $utc.Ticks) 'Typed DateTimeOffset timestamps must retain their instant.'
                Assert-Audit ((Convert-ToUtc '2026-11-07T14:20:30+04:00' $fallback).Ticks -eq $utc.Ticks) 'Offset strings must retain their instant.'
            }
            'Unicode' {
                $tools = Copy-AuditTools $fixture @('Get-LlmWikiToolingContext.ps1')
                '# Самопроверка: проверка корректности' | Set-Content (Join-Path $tools 'Probe.ps1') -Encoding utf8
                $null = [IO.Directory]::CreateDirectory((Join-Path $fixture '.llm-wiki/workflows'))
                $result = & (Join-Path $tools 'Get-LlmWikiToolingContext.ps1') -Query 'Самопроверка' -Format Json | ConvertFrom-Json
                Assert-Audit ('самопроверка' -in $result.tokens -and '.llm-wiki/tools/Probe.ps1' -in $result.groundedPaths) 'Literal Russian tooling terms must remain searchable.'
            }
            'ReadOnly' {
                $tools = Copy-AuditTools $fixture @('Invoke-LlmWikiReadOnlyTool.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiProcess.ps1')
                '"{`"ok`":true}"' | Set-Content (Join-Path $tools 'Reader.ps1')
                'throw "fixture tool failure"' | Set-Content (Join-Path $tools 'Failure.ps1')
                @'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
[IO.File]::WriteAllText((Join-Path $root 'value.txt'), 'changed')
& git -C $root add value.txt
& git -C $root -c user.name=WikiFixture -c user.email=wiki-fixture@example.invalid commit --quiet -m mutation
if ($LASTEXITCODE) { throw 'Fixture mutation commit failed.' }
'@ | Set-Content (Join-Path $tools 'Committer.ps1')
                'original' | Set-Content (Join-Path $fixture 'value.txt')
                Initialize-AuditGit $fixture
                $names = @('LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT', 'LLM_WIKI_READ_ONLY_SOURCE_ROOT', 'GIT_DIR', 'GIT_CONFIG_PARAMETERS')
                $saved = @{}
                foreach ($name in $names) { $saved[$name] = @{ exists = Test-Path "Env:$name"; value = [Environment]::GetEnvironmentVariable($name) } }
                try {
                    $env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT = $fixture
                    $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = $fixture
                    $env:GIT_DIR = 'fixture-caller-directory'
                    $env:GIT_CONFIG_PARAMETERS = 'fixture-caller-config'
                    $wrapper = Join-Path $tools 'Invoke-LlmWikiReadOnlyTool.ps1'
                    $response = & $wrapper -ToolPath (Join-Path $tools 'Reader.ps1') | ConvertFrom-Json
                    Assert-Audit ($response.ok -and $env:GIT_DIR -ceq 'fixture-caller-directory' -and $env:GIT_CONFIG_PARAMETERS -ceq 'fixture-caller-config') 'Read-only success must preserve caller Git environment.'
                    $failure = ''
                    try { & $wrapper -ToolPath (Join-Path $tools 'Failure.ps1') } catch { $failure = $_.Exception.Message }
                    Assert-Audit ($failure -like '*fixture tool failure*' -and $env:GIT_DIR -ceq 'fixture-caller-directory' -and $env:GIT_CONFIG_PARAMETERS -ceq 'fixture-caller-config') 'Read-only failure must preserve caller Git environment.'
                    $failure = ''
                    try { & $wrapper -ToolPath (Join-Path $tools 'Committer.ps1') | Out-Null } catch { $failure = $_.Exception.Message }
                    Assert-Audit ($failure -like '*modified its isolated snapshot*') 'Read-only guard must reject a committed mutation even when Git status stays clean.'
                } finally {
                    foreach ($name in $names) {
                        if ($saved[$name].exists) { Set-Item "Env:$name" $saved[$name].value }
                        else { Remove-Item "Env:$name" -ErrorAction SilentlyContinue }
                    }
                }
            }
            'OutputLimit' {
                $tools = Copy-AuditTools $fixture @('LlmWikiAssessmentRunner.ps1', 'Invoke-LlmWikiObservedStage.ps1', 'LlmWikiQueryCache.ps1', 'LlmWikiJson.ps1', 'LlmWikiProcess.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiChangeSetSnapshot.ps1')
                'param($Action,$Format); @{valid=$true;blob=("x"*100000)} | ConvertTo-Json -Compress' | Set-Content (Join-Path $tools 'Probe.ps1')
                . (Join-Path $tools 'LlmWikiAssessmentRunner.ps1')
                $failure = ''
                try { $null = Invoke-LlmWikiAssessment -ToolName Probe.ps1 -Arguments @{Action='assess'} -MaximumOutputBytes 4096 -TimeoutSeconds 10 } catch { $failure = $_.Exception.Message }
                Assert-Audit ($failure -like '*output exceeded 4096 bytes*') 'Assessment must reject oversized result JSON before deserialization.'
            }
        }
        Write-Host "Wiki audit regression: $scenario passed."
    } finally {
        $resolvedFixture = [IO.Path]::GetFullPath($fixture)
        $sandboxPrefix = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot -IndependentRepository)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        if (-not $resolvedFixture.StartsWith($sandboxPrefix, [StringComparison]::OrdinalIgnoreCase) -or
            -not [IO.Path]::GetFileName($resolvedFixture).StartsWith("audit-$scenario-", [StringComparison]::Ordinal)) {
            throw 'Refusing to clean an unowned audit fixture.'
        }
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
