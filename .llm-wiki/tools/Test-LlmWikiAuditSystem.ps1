[CmdletBinding()]
param([ValidateSet('All','FastCache','FullResume','Facade','FacadeDrift','Domain','Api','Intent','Completion')][string]$Case='All')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repositoryRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$toolsRoot=$PSScriptRoot
$shell=(Get-Process -Id $PID).Path
. (Join-Path $toolsRoot 'LlmWikiSmokeSandbox.ps1')
. (Join-Path $toolsRoot 'LlmWikiProcess.ps1')
function Assert-System([bool]$Condition,[string]$Message) { if(-not $Condition){throw $Message} }
function New-SystemFixture([string]$Name,[string[]]$Owners) {
 $root=New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name "system-$Name" -IndependentRepository
 [void][IO.Directory]::CreateDirectory((Join-Path $root '.llm-wiki/tools'))
 [void][IO.Directory]::CreateDirectory((Join-Path $root '.artifacts/llm-wiki'))
 foreach($name in $Owners){Copy-Item -LiteralPath (Join-Path $toolsRoot $name) -Destination (Join-Path $root '.llm-wiki/tools')}
 [IO.File]::WriteAllText((Join-Path $root '.gitignore'),'.artifacts/')
 [IO.File]::WriteAllText((Join-Path $root 'source.cs'),'state-A')
 return $root
}
function Initialize-SystemGit([string]$Root) {
 & git -C $Root init --quiet
 & git -C $Root -c core.autocrlf=false add .
 & git -C $Root -c user.name=WikiFixture -c user.email=wiki-fixture@example.invalid commit --quiet -m baseline
 if($LASTEXITCODE){throw 'Unable to initialize owned system fixture.'}
}
function Remove-SystemFixture([string]$Path) {
 $resolved=[IO.Path]::GetFullPath($Path)
 $ownedParent=[IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot -IndependentRepository)).TrimEnd('\','/')
 if([IO.Path]::GetDirectoryName($resolved) -ine $ownedParent -or [IO.Path]::GetFileName($resolved) -notmatch '^system-[a-z-]+-[a-f0-9]{32}$'){throw 'Fixture cleanup escaped its exact owned parent.'}
 if(Test-Path -LiteralPath $resolved){Remove-Item -LiteralPath $resolved -Recurse -Force}
}
if($Case -in @('All','FastCache')){
 $fixture=New-SystemFixture 'fast-cache' @('Manage-LlmWikiVerificationCache.ps1','LlmWikiGitPaths.ps1')
 try {
  Initialize-SystemGit $fixture
  $arguments=@{RepositoryRoot=$fixture;BaseRef='HEAD';ChangedPath=@('source.cs')}
  $start=& (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check @arguments
  [IO.File]::WriteAllText((Join-Path $fixture 'source.cs'),'state-B')
  $rejected=$false
  try { $null=& (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Record -ExpectedFingerprint $start.fingerprint @arguments }
  catch { $rejected=$_.Exception.Message -match 'changed during|fingerprint.*changed' }
  Assert-System $rejected 'Fast verification published a receipt for sources changed after the checks started.'
  $miss=& (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check @arguments
  Assert-System (-not $miss.hit) 'Source drift left a reusable fast-verification receipt.'
  $null=& (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Record -ExpectedFingerprint $miss.fingerprint @arguments
  $hit=& (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check @arguments
  Assert-System $hit.hit 'Unchanged tested source did not retain successful fast-cache reuse.'
 } finally {Remove-SystemFixture $fixture}
 Write-Host 'System FastCache passed.'
}
if($Case -in @('All','FullResume')){
 $fixture=New-SystemFixture 'full-resume' @('Invoke-LlmWikiFullVerification.ps1','Get-LlmWikiVerificationStageFingerprint.ps1','LlmWikiGitPaths.ps1','LlmWikiProcess.ps1','Write-LlmWikiWorkflowMetric.ps1')
 try {
  $fixtureTools=Join-Path $fixture '.llm-wiki/tools'
  $stub=@'
param([switch]$Check,[int]$MaxConcurrency,[switch]$AllGroups,[switch]$NoCache)
$root=Split-Path (Split-Path $PSScriptRoot)
[IO.File]::AppendAllText((Join-Path $root '.artifacts/check-states.txt'),[IO.File]::ReadAllText((Join-Path $root 'source.cs'))+"`n")
if($env:LLM_WIKI_SYSTEM_DRIFT -eq '1'){[IO.File]::WriteAllText((Join-Path $root 'source.cs'),'state-B')}
'@
  foreach($name in @('Test-LlmWikiSessionResolution.ps1','Test-LlmWikiProcess.ps1','Invoke-LlmWikiIndexPipeline.ps1','Invoke-LlmWikiParallelSmoke.ps1')){[IO.File]::WriteAllText((Join-Path $fixtureTools $name),$stub)}
  Initialize-SystemGit $fixture
  $old=$env:LLM_WIKI_SYSTEM_DRIFT
  try {
   $env:LLM_WIKI_SYSTEM_DRIFT='1'
   $output=& $shell -NoProfile -File (Join-Path $fixtureTools 'Invoke-LlmWikiFullVerification.ps1') -ResumePassedStages 2>&1
   Assert-System ($LASTEXITCODE -ne 0 -and ($output -join "`n") -match 'changed during') 'Full verification accepted a source mutation and published resumable success.'
   $env:LLM_WIKI_SYSTEM_DRIFT='0'
   [IO.File]::WriteAllText((Join-Path $fixture 'source.cs'),'state-A')
   $output=& $shell -NoProfile -File (Join-Path $fixtureTools 'Invoke-LlmWikiFullVerification.ps1') -ResumePassedStages 2>&1
   Assert-System ($LASTEXITCODE -eq 0 -and ($output -join "`n") -notmatch 'Resuming LLM Wiki') 'Untested original state reused a source-drift receipt.'
   $output=& $shell -NoProfile -File (Join-Path $fixtureTools 'Invoke-LlmWikiFullVerification.ps1') -ResumePassedStages 2>&1
   Assert-System ($LASTEXITCODE -eq 0 -and ($output -join "`n") -match 'Resuming LLM Wiki') 'Valid unchanged full verification did not resume.'
  } finally {$env:LLM_WIKI_SYSTEM_DRIFT=$old}
 } finally {Remove-SystemFixture $fixture}
 Write-Host 'System FullResume passed.'
}
if($Case -in @('All','Facade','FacadeDrift')){
 $fixture=New-SystemFixture 'facade' @('LlmWikiGitPaths.ps1','LlmWikiJson.ps1','LlmWikiProcess.ps1','Invoke-LlmWikiObservedStage.ps1','Get-LlmWikiVerificationStageFingerprint.ps1')
 try {
  $fixtureTools=Join-Path $fixture '.llm-wiki/tools'
  Copy-Item -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') -Destination (Join-Path $fixture '.llm-wiki/wiki.ps1')
  [IO.File]::WriteAllText((Join-Path $fixtureTools 'Write-LlmWikiWorkflowMetric.ps1'),'param($Operation,$Outcome,$DurationSeconds,$ScopePathCount)')
  [IO.File]::WriteAllText((Join-Path $fixtureTools 'Manage-LlmWikiVerificationTelemetry.ps1'),'param($Action,$WorkspacePath,$CheckId,$Status,$DurationSeconds,$Command,$InputFingerprint,$Format); ''{}''')
  [IO.File]::WriteAllText((Join-Path $fixtureTools 'Invoke-LlmWikiAffectedSmoke.ps1'),'param($BaseRef,$ChangedPath,[switch]$Plan,$Format); ''{"groups":[]}''')
  $stub=@'
$root=Split-Path (Split-Path $PSScriptRoot)
[IO.File]::AppendAllText((Join-Path $root '.artifacts/checked.txt'),"checked`n")
if($env:LLM_WIKI_SYSTEM_DRIFT -eq '1'){[IO.File]::WriteAllText((Join-Path $root '.llm-wiki/index.md'),'state-B')}
'@
  [IO.File]::WriteAllText((Join-Path $fixtureTools 'Test-LlmWiki.ps1'),$stub)
  [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/index.md'),'state-A')
  Initialize-SystemGit $fixture
  $old=$env:LLM_WIKI_SYSTEM_DRIFT
  try {
   $env:LLM_WIKI_SYSTEM_DRIFT='0'
   if($Case -ne 'FacadeDrift'){
    @{status='running';stage='page contracts';ownerProcessId=1073741824;childProcessId=$PID;updatedAtUtc=[DateTime]::UtcNow.AddDays(-1).ToString('o')} | ConvertTo-Json | Set-Content (Join-Path $fixture '.artifacts/llm-wiki/verify-progress.json')
    $output=& $shell -NoProfile -File (Join-Path $fixture '.llm-wiki/wiki.ps1') verify -Stage 'page contracts' -ChangedPath '.llm-wiki/index.md' -ResumePassedStages:$false 2>&1
    Assert-System ($LASTEXITCODE -eq 0 -and (Test-Path (Join-Path $fixture '.artifacts/checked.txt'))) "Orphan progress with an unrelated live PID produced success without executing the requested check. Output: $($output -join ' | ')"
    $progressPath=Join-Path $fixture '.artifacts/llm-wiki/verify-progress.json'
    @{status='running';stage='page contracts';ownerProcessId=1073741824;ownerProcessIdentityToken='missing-owner';childProcessId=$PID;childProcessIdentityToken=(Get-LlmWikiProcessIdentityToken -ProcessId $PID);updatedAtUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content $progressPath
    $countBefore=[IO.File]::ReadAllLines((Join-Path $fixture '.artifacts/checked.txt')).Count
    foreach($attempt in 1..2){
     $output=& $shell -NoProfile -File (Join-Path $fixture '.llm-wiki/wiki.ps1') verify -Stage 'page contracts' -ChangedPath '.llm-wiki/index.md' 2>&1
     Assert-System ($LASTEXITCODE -ne 0 -and ($output -join "`n") -match 'worker is still running') 'A verified active orphan worker was bypassed on retry.'
    }
    Assert-System ([IO.File]::ReadAllLines((Join-Path $fixture '.artifacts/checked.txt')).Count -eq $countBefore) 'Recovery duplicated a verified live worker.'
    Remove-Item -LiteralPath $progressPath -Force
   }
   $env:LLM_WIKI_SYSTEM_DRIFT='1'
   $output=& $shell -NoProfile -File (Join-Path $fixture '.llm-wiki/wiki.ps1') verify -Stage 'page contracts' -ChangedPath '.llm-wiki/index.md' -ResumePassedStages 2>&1
   Assert-System ($LASTEXITCODE -ne 0 -and ($output -join "`n") -match 'changed during') 'Observed stage accepted source drift.'
   [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/index.md'),'state-A')
   $env:LLM_WIKI_SYSTEM_DRIFT='0'
   $countBefore=[IO.File]::ReadAllLines((Join-Path $fixture '.artifacts/checked.txt')).Count
   $output=& $shell -NoProfile -File (Join-Path $fixture '.llm-wiki/wiki.ps1') verify -Stage 'page contracts' -ChangedPath '.llm-wiki/index.md' -ResumePassedStages 2>&1
   Assert-System ($LASTEXITCODE -eq 0 -and [IO.File]::ReadAllLines((Join-Path $fixture '.artifacts/checked.txt')).Count -gt $countBefore) 'Canonical facade reused a source-drift worker receipt.'
   $output=& $shell -NoProfile -File (Join-Path $fixture '.llm-wiki/wiki.ps1') verify -Stage 'page contracts' -ChangedPath '.llm-wiki/index.md' -ResumePassedStages 2>&1
   Assert-System ($LASTEXITCODE -eq 0 -and ($output -join "`n") -match 'Resuming Wiki verify') 'Canonical stage no longer reuses an unchanged successful check.'
  } finally {$env:LLM_WIKI_SYSTEM_DRIFT=$old}
 } finally {Remove-SystemFixture $fixture}
 Write-Host 'System Facade passed.'
}
if($Case -in @('All','Domain')){
 $fixture=New-SystemFixture 'projection' @()
 $ownedArtifacts=Join-Path $repositoryRoot ('.artifacts/dotnet/wiki-system-'+[guid]::NewGuid().ToString('N'))
 try {
  $build=& (Join-Path $toolsRoot 'Build-LlmWikiInProcessSqliteReader.ps1') -Format Json | ConvertFrom-Json
  $references=@('LlmWiki.SqliteReader','Microsoft.Data.Sqlite','SQLitePCLRaw.core','SQLitePCLRaw.provider.e_sqlite3','SQLitePCLRaw.batteries_v2') | ForEach-Object {'<Reference Include="'+$_+'"><HintPath>'+[Security.SecurityElement]::Escape((Join-Path $build.outputPath ($_.ToString()+'.dll')))+'</HintPath></Reference>'}
  $native=@('e_sqlite3.dll','libe_sqlite3.so','libe_sqlite3.dylib') | ForEach-Object {Join-Path $build.outputPath $_} | Where-Object {Test-Path -LiteralPath $_} | ForEach-Object {'<None Include="'+[Security.SecurityElement]::Escape($_)+'" Link="'+[IO.Path]::GetFileName($_)+'" CopyToOutputDirectory="PreserveNewest" />'}
  $project='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><EnableNETAnalyzers>false</EnableNETAnalyzers><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $toolsRoot 'Test-LlmWikiProjectionSnapshot.cs'))+'" />'+($references -join '')+($native -join '')+'</ItemGroup></Project>'
  $projectPath=Join-Path $fixture 'Projection.csproj'
  [IO.File]::WriteAllText($projectPath,$project)
  $startInfo=[Diagnostics.ProcessStartInfo]::new('dotnet')
  $startInfo.UseShellExecute=$false;$startInfo.CreateNoWindow=$true
  foreach($argument in @('run','--project',$projectPath,'--configuration','Release','--artifacts-path',$ownedArtifacts,'-p:RunAnalyzers=false',"-p:NuGetLockFilePath=$fixture/packages.lock.json")){$startInfo.ArgumentList.Add($argument)}
  $process=[Diagnostics.Process]::new();$process.StartInfo=$startInfo
  try {
   if(-not $process.Start()){throw 'Cannot start projection regression.'}
   if(-not $process.WaitForExit(120000)){. (Join-Path $toolsRoot 'LlmWikiProcess.ps1');Stop-LlmWikiProcessTree $process;throw 'Projection regression exceeded its120second deadline.'}
   Assert-System ($process.ExitCode -eq 0) 'Projection reader mixed verified metadata with unverified rows.'
  } finally {$process.Dispose()}
 } finally {
  Remove-SystemFixture $fixture
  $resolvedArtifacts=[IO.Path]::GetFullPath($ownedArtifacts)
  $expectedParent=[IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/dotnet'))
  if([IO.Path]::GetDirectoryName($resolvedArtifacts) -ine $expectedParent -or [IO.Path]::GetFileName($resolvedArtifacts) -notmatch '^wiki-system-[a-f0-9]{32}$'){throw 'Projection artifact cleanup escaped its owned directory.'}
  if(Test-Path -LiteralPath $resolvedArtifacts){Remove-Item -LiteralPath $resolvedArtifacts -Recurse -Force}
 }
 Write-Host 'System Domain passed.'
}
if($Case -in @('All','Api')){
 function New-SystemContract($Schema){
  @{paths=@{};components=@{schemas=@{Probe=@{type='object';properties=@{value=$Schema};required=@()}}}} | ConvertTo-Json -Depth 15 -Compress
 }
 foreach($pair in @(
  @(@{type='string';enum=@('A','B')},@{type='string';enum=@('A')}),
  @(@{type='integer';minimum=0},@{type='integer';minimum=1}),
  @(@{type='string';maxLength=100},@{type='string';maxLength=10}),
  @(@{type='array';items=@{type='string'};maxItems=10},@{type='array';items=@{type='string'};maxItems=2})
 )){
  $before=New-SystemContract $pair[0];$after=New-SystemContract $pair[1]
  $result=& (Join-Path $toolsRoot 'Test-LlmWikiApiCompatibility.ps1') -BaseSnapshotContent $before -CurrentSnapshotContent $after -Format Json | ConvertFrom-Json
  Assert-System ($result.breakingCount -gt 0) "Component property restriction was ignored: $after"
  $same=& (Join-Path $toolsRoot 'Test-LlmWikiApiCompatibility.ps1') -BaseSnapshotContent $before -CurrentSnapshotContent $before -Format Json | ConvertFrom-Json
  Assert-System ($same.breakingCount -eq 0) 'Identical component schema became a breaking change.'
 }
 $reordered=& (Join-Path $toolsRoot 'Test-LlmWikiApiCompatibility.ps1') -BaseSnapshotContent (New-SystemContract @{type='string';enum=@('A','B')}) -CurrentSnapshotContent (New-SystemContract @{type='string';enum=@('B','A')}) -Format Json | ConvertFrom-Json
 Assert-System ($reordered.breakingCount -eq 0) 'Reordered enum values became a false breaking change.'
 Write-Host 'System Api passed.'
}
if($Case -in @('All','Intent')){
 foreach($intent in @('Fix LLM Wiki indexes','Fix Wiki indexes','Исправить индексы вики')){
 $brief=& (Join-Path $toolsRoot 'Get-LlmWikiTaskBrief.ps1') -Intent $intent -SkipTestPlan -SkipQueryCache -Format Json | ConvertFrom-Json
 $plan=& (Join-Path $toolsRoot 'Get-LlmWikiTestPlan.ps1') -Intent $intent -ChangedPath '.llm-wiki/tools/Get-LlmWikiTaskBrief.ps1' -Format Json | ConvertFrom-Json
 Assert-System (@($brief.change.paths | Where-Object {$_ -match '^FoodDiary.Infrastructure/(Persistence|Migrations)/|^Platform/tests/'}).Count -eq 0) 'Wiki-only index intent added product persistence source paths.'
 Assert-System (@($plan.commands | Where-Object id -in @('database-model-sync','database-provider-tests')).Count -eq 0) 'Wiki-only index scope requires product database checks.'
 }
 foreach($intent in @('Improve PostgreSQL database indexes','Fix LLM Wiki indexes and PostgreSQL database indexes','Fix LLM Wiki indexes')){
  $plan=& (Join-Path $toolsRoot 'Get-LlmWikiTestPlan.ps1') -Intent $intent -ChangedPath @('.llm-wiki/wiki.ps1','FoodDiary.Infrastructure/Persistence/FoodDiaryDbContext.cs') -Format Json | ConvertFrom-Json
  Assert-System (@($plan.commands | Where-Object id -eq 'database-provider-tests').Count -eq 1) 'Database or mixed scope lost required provider checks.'
 }
 Write-Host 'System Intent passed.'
}
if($Case -in @('All','Completion')){
 $fixture=New-SystemFixture 'completion' @('Complete-LlmWikiTaskWorkspace.ps1')
 try {
  $fixtureTools=Join-Path $fixture '.llm-wiki/tools'
  $workspaceName='.artifacts/llm-wiki/tasks/probe'
  $workspace=Join-Path $fixture $workspaceName
  [void][IO.Directory]::CreateDirectory($workspace)
  $assessment=@'
function Invoke-LlmWikiAssessment {
 param($ToolName,$Arguments)
 [pscustomobject]@{verdict='ready';score=100;objective='completion retry';currentPacketFingerprint='fixture';ledger=@{verdict='ready';score=100};critique=@{verdict='accept';score=100}}
}
'@
  [IO.File]::WriteAllText((Join-Path $fixtureTools 'LlmWikiAssessmentRunner.ps1'),$assessment)
  [IO.File]::WriteAllText((Join-Path $fixtureTools 'Get-LlmWikiWorkspacePolicy.ps1'),'param($Action,[switch]$WithFingerprint,$Format); ''{"fingerprint":"fixture-policy"}''')
  $report=@'
param($PacketInput,$ManifestPath,$AcceptancePath,$EvidencePath,$OutputPath)
[IO.File]::WriteAllText((Join-Path (Split-Path (Split-Path $PSScriptRoot)) $OutputPath),'fixture report')
'@
  [IO.File]::WriteAllText((Join-Path $fixtureTools 'Get-LlmWikiReviewReport.ps1'),$report)
  $retrospective=@'
param($Action,$WorkspacePath,$Format)
$path=Join-Path (Split-Path (Split-Path $PSScriptRoot)) "$WorkspacePath/retrospective.json"
if($Action -eq 'create'){[IO.File]::WriteAllText($path,'{}')}
'{"valid":true,"issues":[],"retrospective":{"retrospectiveHash":"fixture","summary":{"candidateCount":0}}}'
'@
  [IO.File]::WriteAllText((Join-Path $fixtureTools 'Manage-LlmWikiRetrospective.ps1'),$retrospective)
  $observer=@'
param($Action,$WorkspacePath,$Format)
$path=Join-Path (Split-Path (Split-Path $PSScriptRoot)) "$WorkspacePath/$($MyInvocation.MyCommand.Name).observed"
if($MyInvocation.MyCommand.Name -eq 'Manage-LlmWikiLearningHealth.ps1' -and -not (Test-Path $path)){
 [IO.File]::WriteAllText($path,'first-failed')
 throw 'Injected transient observation I/O failure'
}
[IO.File]::WriteAllText($path,'observed')
'{"valid":true,"addedCount":0,"eventHash":"fixture","eventHashes":[],"observationEventHashes":[],"issues":[]}'
'@
  foreach($name in @('LearningPromotion','EvalPromotion','LearningHealth','ContextOutcome','ModelRoutingOutcome','InstructionOutcome')){[IO.File]::WriteAllText((Join-Path $fixtureTools "Manage-LlmWiki$name.ps1"),$observer)}
  [IO.File]::WriteAllText((Join-Path $fixtureTools 'Write-LlmWikiWorkspaceOutcome.ps1'),'param($WorkspacePath,$Completion); [pscustomobject]@{completionFingerprint=$Completion.completionFingerprint}')
  foreach($name in @('workspace.json','change-packet.json','task-contract.json','change-manifest.json','acceptance-matrix.json','evidence.json','journal.json','review-report.md','risk-calibration.json','failure-prediction.json','verification-cost.json','plan-conformance.json','requirement-model.json','impact-simulation.json','proof-of-change.json','confidence-ledger.json','change-critique.json')){[IO.File]::WriteAllText((Join-Path $workspace $name),'{}')}
  Initialize-SystemGit $fixture
  $firstFailure=''
  try {$null=& (Join-Path $fixtureTools 'Complete-LlmWikiTaskWorkspace.ps1') finish -WorkspacePath $workspaceName -Format Json}
  catch {$firstFailure=$_.Exception.Message}
  Assert-System ($firstFailure -match 'Injected transient') 'Completion fixture did not reach the injected observation failure.'
  $sealPath=Join-Path $workspace 'completion.json'
  $sealBefore=[IO.File]::ReadAllText($sealPath)
  $retryFailure='';$result=$null
  try {$result=& (Join-Path $fixtureTools 'Complete-LlmWikiTaskWorkspace.ps1') finish -WorkspacePath $workspaceName -Format Json | ConvertFrom-Json}
  catch {$retryFailure=$_.Exception.Message}
  Assert-System ([string]::IsNullOrEmpty($retryFailure) -and $null -ne $result -and $result.valid) "Transient completion failure could not be retried: $retryFailure"
  Assert-System ([IO.File]::ReadAllText($sealPath) -ceq $sealBefore) 'Recovery changed the canonical completion fingerprint or time.'
  foreach($name in @('LearningPromotion','EvalPromotion','LearningHealth','ContextOutcome','ModelRoutingOutcome','InstructionOutcome')){Assert-System ([IO.File]::ReadAllText((Join-Path $workspace "Manage-LlmWiki$name.ps1.observed")) -eq 'observed') "Recovery skipped $name."}
  $verified=& (Join-Path $fixtureTools 'Complete-LlmWikiTaskWorkspace.ps1') verify -WorkspacePath $workspaceName -Format Json | ConvertFrom-Json
  Assert-System $verified.valid 'Recovered completion failed final seal verification.'
 } finally {Remove-SystemFixture $fixture}
 Write-Host 'System Completion passed.'
}
