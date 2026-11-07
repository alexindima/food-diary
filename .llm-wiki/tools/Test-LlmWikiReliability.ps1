[CmdletBinding()]
param([ValidateSet('All', 'Extractor', 'Scope', 'Environment', 'Concurrency', 'Session', 'Reader', 'Assessment', 'Ownership')][string]$Case = 'All')

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$shell = (Get-Process -Id $PID).Path
$cases = if ($Case -eq 'All') { @('Extractor', 'Scope', 'Environment', 'Concurrency', 'Session', 'Reader', 'Assessment', 'Ownership') } else { @($Case) }
function Assert-Reliability([bool]$Condition, [string]$Message) { if (-not $Condition) { throw "Wiki reliability: $Message" } }
function Copy-Tools([string]$Fixture, [string[]]$Names) {
    $directory = Join-Path $Fixture '.llm-wiki/tools'
    $null = [IO.Directory]::CreateDirectory($directory)
    foreach ($name in $Names) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $directory -Force }
    return $directory
}
function Start-Fixture([string]$Script, [string[]]$Arguments) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $shell; $start.UseShellExecute = $false; $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
    foreach ($argument in @('-NoLogo', '-NoProfile', '-File', $Script) + $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    return [pscustomobject]@{ process=$process; output=$process.StandardOutput.ReadToEndAsync(); errors=$process.StandardError.ReadToEndAsync() }
}
function Finish-Fixture($Child, [int]$Seconds = 20) {
    if (-not $Child.process.WaitForExit($Seconds * 1000)) { $Child.process.Kill($true); throw 'Owned reliability fixture timed out.' }
    return [pscustomobject]@{ code=$Child.process.ExitCode; output=$Child.output.GetAwaiter().GetResult(); errors=$Child.errors.GetAwaiter().GetResult() }
}
foreach ($scenario in $cases) {
    $fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name "reliability-$scenario" -IndependentRepository
    $children = [Collections.Generic.List[object]]::new()
    try {
        switch ($scenario) {
            'Extractor' {
                if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { Write-Host 'Extractor Windows exit transport: not applicable on this OS.'; continue }
                $tools = Copy-Tools $fixture @('Invoke-LlmWikiContractReferenceExtractor.ps1', 'LlmWikiProcess.ps1')
                'function Get-LlmWikiIndexInputFingerprint { param($RepositoryRoot, $InputPath) "fixture" }' | Set-Content (Join-Path $tools 'LlmWikiIndexCache.ps1')
                $output = Join-Path $fixture '.artifacts/llm-wiki/contract-reference-extractor/fixture/bin/LlmWiki.ContractReferenceExtractor/release'
                $null = [IO.Directory]::CreateDirectory($output)
                [IO.File]::WriteAllText((Join-Path (Split-Path (Split-Path (Split-Path $output))) 'ready.txt'), '')
                $project = Join-Path $fixture 'Fixture.csproj'
                '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AssemblyName>LlmWiki.ContractReferenceExtractor</AssemblyName></PropertyGroup></Project>' | Set-Content $project
                'System.Console.WriteLine("[]"); return 17;' | Set-Content (Join-Path $fixture 'Program.cs')
                & dotnet build $project --output $output --artifacts-path (Join-Path $fixture '.artifacts/build') --nologo --verbosity quiet | Out-Null
                if ($LASTEXITCODE) { throw 'Cannot compile the owned exit-code fixture.' }
                $failure = ''
                try { $null = & (Join-Path $tools 'Invoke-LlmWikiContractReferenceExtractor.ps1') -RepositoryRoot $fixture -Path @('fixture.cs') -Name @('Contract') } catch { $failure = $_.Exception.Message }
                Assert-Reliability ($failure -like '*exit code 17*') 'extractor must preserve child exit code 17, even with valid JSON output.'
            }
            'Scope' {
                $tools = Copy-Tools $fixture @('Get-LlmWikiContextExplanation.ps1')
                @'
param($Action,$Query,$ChangeType,$Limit,$Format,[switch]$SkipRefresh,[string[]]$ChangedPath,[string]$PathPrefix)
@{ ready=$true; queryTerms=@('fixture'); indexedDocuments=1; durationMs=1; records=@(@{rank=1;path=($ChangedPath -join ';');score=1;lexicalRank=0;layer='application';module='Fixture';role='handler';isTest=$false;extension='.cs';reasons=@()}) } | ConvertTo-Json -Depth 5
'@ | Set-Content (Join-Path $tools 'Manage-LlmWikiCodeGraph.ps1')
                'param($DurationMs,$QueryTermCount,$CandidateCount,$TopLayer,$TopRole,$Ready)' | Set-Content (Join-Path $tools 'Write-LlmWikiContextQueryObservation.ps1')
                $actual = & (Join-Path $tools 'Get-LlmWikiContextExplanation.ps1') -Query fixture -ScopePath @('Modules/One', 'Modules/Two') -Format Json | ConvertFrom-Json
                Assert-Reliability ($actual.candidates[0].path -ceq 'Modules/One;Modules/Two') 'context explanation must forward every scope through ChangedPath.'
            }
            'Environment' {
                $probe = Join-Path $fixture 'probe.ps1'
                @'
param($Wiki)
$ErrorActionPreference='Stop'
$env:GIT_CONFIG_COUNT='1';$env:GIT_CONFIG_KEY_0='fixture.key';$env:GIT_CONFIG_VALUE_0='original'
& $Wiki help *> $null
if ($env:GIT_CONFIG_COUNT -cne '1' -or $env:GIT_CONFIG_KEY_0 -cne 'fixture.key' -or $env:GIT_CONFIG_VALUE_0 -cne 'original' -or (Test-Path Env:GIT_CONFIG_KEY_1)) { throw 'Wiki reliability: facade must restore the caller Git environment.' }
'@ | Set-Content $probe
                $child = Start-Fixture $probe @('-Wiki', (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1')); $children.Add($child)
                $result = Finish-Fixture $child
                Assert-Reliability ($result.code -eq 0) "facade must restore the caller Git environment. $($result.errors)"
            }
            'Concurrency' {
                $tools = Copy-Tools $fixture @('Measure-LlmWikiContextConcurrency.ps1')
                'function Get-LlmWikiChangeSetSnapshot { param($RepositoryRoot) [pscustomobject]@{fingerprint="stable"} }' | Set-Content (Join-Path $tools 'LlmWikiChangeSetSnapshot.ps1')
                'param($Action,$Query,$ChangeType,$Limit,$Format,[switch]$SkipRefresh); ''{"ready":false,"records":[]}''' | Set-Content (Join-Path $tools 'Manage-LlmWikiCodeGraph.ps1')
                $corpus = Join-Path $fixture 'corpus.json'; '{"cases":[{"id":"unavailable","query":"fixture","changeType":"Any"}]}' | Set-Content $corpus
                $child = Start-Fixture (Join-Path $tools 'Measure-LlmWikiContextConcurrency.ps1') @('-CorpusPath',$corpus,'-Workers','1','-QueriesPerWorker','1','-Format','Json'); $children.Add($child)
                $result = Finish-Fixture $child
                Assert-Reliability ($result.code -ne 0 -and $result.errors -match 'not ready') 'concurrency benchmark must reject a search that is not ready.'
            }
            'Session' {
                $tools = Copy-Tools $fixture @('Resolve-LlmWikiSession.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiJson.ps1', 'LlmWikiProcess.ps1')
                $tool = Join-Path $tools 'Resolve-LlmWikiSession.ps1'
                # Delay publication only: make competing readers overlap without changing registry logic.
                $source = [IO.File]::ReadAllText($tool).Replace('function Write-Registry([object]$Value) {', "function Write-Registry([object]`$Value) {`n Start-Sleep -Milliseconds 350")
                [IO.File]::WriteAllText($tool, $source)
                foreach ($id in 1..6) { $children.Add((Start-Fixture $tool @('-Create','-SessionId',"fixture-$id",'-RepositoryRoot',$fixture,'-Format','Json'))) }
                $results = @($children | ForEach-Object { Finish-Fixture $_ })
                $count = 0
                try { $registry = Get-Content (Join-Path $fixture '.git/llm-wiki/sessions/registry.json') -Raw | ConvertFrom-Json; $count = @($registry.sessions).Count } catch { }
                Assert-Reliability ($count -eq 6 -and @($results | Where-Object code -ne 0).Count -eq 0) 'concurrent successful session creation must retain all six sessions.'
            }
            'Reader' {
                $tools = Copy-Tools $fixture @('Build-LlmWikiInProcessSqliteReader.ps1', 'LlmWikiJson.ps1', 'LlmWikiProcess.ps1')
                $paths = @('.llm-wiki/tools/LlmWiki.SqliteReader/LlmWiki.SqliteReader.csproj','.llm-wiki/tools/LlmWiki.SqliteReader/DomainDataReader.cs','.llm-wiki/tools/LlmWiki.SqliteReader/CompiledIndexReader.cs','.llm-wiki/tools/LlmWiki.SqliteReader/StandaloneIndexReader.cs','.llm-wiki/tools/LlmWiki.SqliteReader/ContextSearchReader.cs','FoodDiary.Development.Mcp/Wiki/SqliteContextSearchReader.cs','FoodDiary.Development.Mcp/Protocol/WikiContextSearchResult.cs','FoodDiary.Development.Mcp/Protocol/WikiContextSearchCandidate.cs','Directory.Build.props','Directory.Packages.props')
                foreach ($path in $paths) { $target=Join-Path $fixture $path; $null=[IO.Directory]::CreateDirectory((Split-Path $target)); Copy-Item -LiteralPath (Join-Path $repositoryRoot $path) -Destination $target }
                $tool = Join-Path $tools 'Build-LlmWikiInProcessSqliteReader.ps1'
                $before = & $tool -CheckOnly -Format Json | ConvertFrom-Json
                $null = [IO.Directory]::CreateDirectory($before.outputPath)
                [IO.File]::WriteAllText($before.assemblyPath,'invalid DLL'); [IO.File]::WriteAllText((Join-Path $before.outputPath 'build-manifest.json'),'not JSON')
                $after = & $tool -CheckOnly -Format Json | ConvertFrom-Json
                Assert-Reliability (-not $after.ready -and -not $after.reused) 'reader cache must reject corrupt DLL and manifest.'
            }
            'Assessment' {
                $tools = Copy-Tools $fixture @('LlmWikiAssessmentRunner.ps1','LlmWikiQueryCache.ps1','LlmWikiGitPaths.ps1','LlmWikiChangeSetSnapshot.ps1','LlmWikiJson.ps1','LlmWikiProcess.ps1','Invoke-LlmWikiObservedStage.ps1')
                @'
param($Format,$Action)
$start=[Diagnostics.ProcessStartInfo]::new();$start.FileName='node';$start.Arguments='-e "setTimeout(()=>{},4500)"';$start.UseShellExecute=$false;$start.CreateNoWindow=$true
$child=[Diagnostics.Process]::Start($start)
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'child.pid'),[string]$child.Id)
'{"valid":true}'
'@ | Set-Content (Join-Path $tools 'Fixture.ps1')
                . (Join-Path $tools 'LlmWikiAssessmentRunner.ps1')
                $failure='';$timer=[Diagnostics.Stopwatch]::StartNew()
                try { $null=Invoke-LlmWikiAssessment -ToolName Fixture.ps1 -Arguments @{Action='fixture'} -TimeoutSeconds 2 -HeartbeatSeconds 1 } catch { $failure=$_.Exception.Message }
                $timer.Stop()
                Assert-Reliability ($failure -like '*timed out*' -and $timer.Elapsed.TotalSeconds -lt 4.5) 'assessment deadline must include inherited output pipes after root exit.'
            }
            'Ownership' {
                . (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
                $start=[Diagnostics.ProcessStartInfo]::new();$start.FileName='node';$start.Arguments='-e "setTimeout(()=>{},15000)"';$start.UseShellExecute=$false;$start.CreateNoWindow=$true
                $script:fixtureRootProcess=[Diagnostics.Process]::Start($start)
                $script:fixtureStopped=[Collections.Generic.List[int]]::new()
                function Get-CimInstance { param($ClassName,$Filter,$ErrorAction) if ($Filter -eq "ParentProcessId = $($script:fixtureRootProcess.Id)") { [pscustomobject]@{ProcessId=987654;ParentProcessId=$script:fixtureRootProcess.Id;CreationDate=[DateTime]::UtcNow.AddDays(-1)} } }
                function Stop-Process { param($Id,$InputObject,[switch]$Force,$ErrorAction) if ($Id) {$script:fixtureStopped.Add($Id)} }
                function taskkill.exe { $script:fixtureRootProcess.Kill() }
                try { Stop-LlmWikiProcessTree $script:fixtureRootProcess; Assert-Reliability (-not $script:fixtureStopped.Contains(987654)) 'process cleanup must ignore descendants older than their alleged parent.' }
                finally { if (-not $script:fixtureRootProcess.HasExited) {$script:fixtureRootProcess.Kill()};$script:fixtureRootProcess.Dispose(); Remove-Item Function:Get-CimInstance,Function:Stop-Process,Function:taskkill.exe }
            }
        }
        Write-Host "Wiki reliability regression passed: $scenario."
    } finally {
        foreach ($child in $children) { if (-not $child.process.HasExited) { $child.process.Kill($true) }; $child.process.Dispose() }
        $parent = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture))
        if (-not [IO.Path]::GetFileName($fixture).StartsWith("reliability-$scenario-",[StringComparison]::Ordinal)) { throw 'Refusing unowned reliability fixture cleanup.' }
        Remove-Item -LiteralPath (Join-Path $parent ([IO.Path]::GetFileName($fixture))) -Recurse -Force
    }
}
