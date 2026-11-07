[CmdletBinding()]
param([ValidateSet('All','ContextRecovery','ModelRecovery','InstructionRecovery','ModelForeign','InstructionForeign','Decomposition','ContextProfile','ContextConcurrent','ModelConcurrent','InstructionConcurrent')][string]$Case='All')
$ErrorActionPreference='Stop'
$repositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
function Assert-Continuity([bool]$Condition,[string]$Message) { if(-not $Condition) { throw "Wiki continuity regression: $Message" } }
function Write-ContinuityJson([string]$Path,[object]$Value) { [IO.File]::WriteAllText($Path,($Value | ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false)) }
function Test-ConcurrentOutcome([string]$Tools,[string]$Manager,[string]$Registry,[string]$Workspace,[string]$AbsoluteWorkspace,[string]$Fixture,[string]$Owner) {
    . (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
    $otherWorkspace="$Workspace-other"
    $otherAbsolute=Join-Path $Fixture $otherWorkspace
    $null=New-Item -ItemType Directory -Path $otherAbsolute -Force
    Get-ChildItem -LiteralPath $AbsoluteWorkspace -File | Copy-Item -Destination $otherAbsolute
    Write-ContinuityJson (Join-Path $otherAbsolute 'completion.json') @{completionFingerprint=('e'*64);readiness=@{verdict='ready'}}
    Write-ContinuityJson $Registry @{schemaVersion=$(if($Owner -eq 'Instruction'){2}else{1});events=@()}
    $workerPath=Join-Path $Fixture 'concurrent-observe.ps1'
    [IO.File]::WriteAllText($workerPath,@'
param($Manager,$Registry,$Workspace,$Control,$Label)
$ErrorActionPreference='Stop'
$script:capturedRegistry=$false
function Get-Content {
    param([Parameter(Position=0)][string]$Path,[string]$LiteralPath,[switch]$Raw)
    $value=Microsoft.PowerShell.Management\Get-Content @PSBoundParameters
    $target=if($LiteralPath){$LiteralPath}else{$Path}
    if(-not $script:capturedRegistry -and [IO.Path]::GetFullPath($target) -eq $Registry -and $Label -in @('a','b')){
        $script:capturedRegistry=$true
        [IO.File]::WriteAllText((Join-Path $Control "$Label.read"),'captured original registry bytes')
        $watch=[Diagnostics.Stopwatch]::StartNew()
        while(-not [IO.File]::Exists((Join-Path $Control "$Label.go"))){
            if($watch.Elapsed.TotalSeconds -gt 15){throw 'Owned registry-read barrier timed out.'}
            Start-Sleep -Milliseconds 10
        }
    }
    return $value
}
try {
    $result=& $Manager observe -WorkspacePath $Workspace -Format Json | ConvertFrom-Json
    $report=@{valid=$result.valid;addedCount=$result.addedCount;error=''}
} catch {$report=@{valid=$false;error=$_.Exception.Message}}
[IO.File]::WriteAllText((Join-Path $Control "$Label.result"),($report | ConvertTo-Json -Compress))
'@,[Text.UTF8Encoding]::new($false))
    $workers=[Collections.Generic.List[Diagnostics.Process]]::new()
    function Start-OutcomeWorker([string]$Label,[string]$TargetWorkspace) {
        $info=[Diagnostics.ProcessStartInfo]::new((Get-Process -Id $PID).Path)
        $info.UseShellExecute=$false;$info.CreateNoWindow=$true
        foreach($argument in @('-NoLogo','-NoProfile','-File',$workerPath,'-Manager',(Join-Path $Tools $Manager),'-Registry',$Registry,'-Workspace',$TargetWorkspace,'-Control',$Fixture,'-Label',$Label)){$info.ArgumentList.Add($argument)}
        $process=[Diagnostics.Process]::new();$process.StartInfo=$info
        if(-not $process.Start()){throw 'Unable to start owned outcome observer.'}
        $null=$process.StartTime
        $workers.Add($process)
        return $process
    }
    function Wait-OutcomeMarker([string[]]$Names) {
        $watch=[Diagnostics.Stopwatch]::StartNew()
        while(@($Names | Where-Object {[IO.File]::Exists((Join-Path $Fixture $_))}).Count -eq 0){
            if($watch.Elapsed.TotalSeconds -gt 12){throw "Owned outcome marker timed out: $($Names -join ',')."}
            Start-Sleep -Milliseconds 10
        }
    }
    function Complete-OutcomeWorker([Diagnostics.Process]$Process,[string]$Label) {
        if(-not $Process.WaitForExit(12000)){throw 'Owned outcome observer did not exit.'}
        Assert-Continuity ($Process.ExitCode -eq 0) 'Owned outcome observer failed outside its result protocol.'
        return Get-Content -LiteralPath (Join-Path $Fixture "$Label.result") -Raw | ConvertFrom-Json
    }
    try {
        $first=Start-OutcomeWorker a $Workspace
        Wait-OutcomeMarker @('a.read')
        $second=Start-OutcomeWorker b $otherWorkspace
        Wait-OutcomeMarker @('b.read','b.result')
        [IO.File]::WriteAllText((Join-Path $Fixture 'a.go'),'continue')
        $firstResult=Complete-OutcomeWorker $first a
        Assert-Continuity $firstResult.valid 'First actual outcome observer must succeed.'
        [IO.File]::WriteAllText((Join-Path $Fixture 'b.go'),'continue')
        $secondResult=Complete-OutcomeWorker $second b
        if(-not $secondResult.valid){
            Assert-Continuity ($secondResult.error -match 'busy|retry') 'Contention must return an explicit retryable ownership failure.'
            $retry=Start-OutcomeWorker retry $otherWorkspace
            $secondResult=Complete-OutcomeWorker $retry retry
        }
        Assert-Continuity $secondResult.valid 'Second actual outcome observer must succeed after retry.'
        $saved=Get-Content -LiteralPath $Registry -Raw | ConvertFrom-Json
        Assert-Continuity (@($saved.events).Count -eq 2) "$Owner concurrent successful observers silently lost a canonical outcome."
        foreach($targetWorkspace in @($Workspace,$otherWorkspace)){
            $verification=& (Join-Path $Tools $Manager) verify -WorkspacePath $targetWorkspace -Format Json | ConvertFrom-Json
            Assert-Continuity $verification.valid "$Owner successful receipt must remain linked to a canonical event."
        }
        $beforeRepeat=[IO.File]::ReadAllText($Registry)
        $repeat=& (Join-Path $Tools $Manager) observe -WorkspacePath $Workspace -Format Json | ConvertFrom-Json
        Assert-Continuity ($repeat.valid -and $repeat.addedCount -eq 0 -and [IO.File]::ReadAllText($Registry) -ceq $beforeRepeat) 'Concurrent recovery must retain duplicate-observe idempotence.'
        foreach($name in @('a.read','a.go','a.result')){[IO.File]::Delete((Join-Path $Fixture $name))}
        $crashed=Start-OutcomeWorker a $Workspace
        Wait-OutcomeMarker @('a.read')
        if($Owner -eq 'Context'){
            $preview=& (Join-Path $Tools $Manager) prune -Format Json | ConvertFrom-Json
            Assert-Continuity $preview.valid 'Read-only prune preview must remain available during an observation.'
            $pruneBlocked=$false
            try {& (Join-Path $Tools $Manager) prune -Apply -Format Json | Out-Null}
            catch {$pruneBlocked=$_.Exception.Message -match 'lease.*retry'}
            Assert-Continuity $pruneBlocked 'Applied prune must share the observer registry lease.'
        }
        Stop-LlmWikiProcessTree $crashed
        Assert-Continuity ([IO.File]::Exists("$Registry.lock")) 'Outcome recovery must retain a stable lease file.'
        $afterCrash=& (Join-Path $Tools $Manager) observe -WorkspacePath $Workspace -Format Json | ConvertFrom-Json
        Assert-Continuity ($afterCrash.valid -and $afterCrash.addedCount -eq 0 -and [IO.File]::ReadAllText($Registry) -ceq $beforeRepeat) 'A dead observer must release OS ownership without changing canonical events.'
    } finally {
        foreach($label in @('a','b')){[IO.File]::WriteAllText((Join-Path $Fixture "$label.go"),'release owned barrier')}
        foreach($process in $workers){try{if(-not $process.HasExited){Stop-LlmWikiProcessTree $process}}finally{$process.Dispose()}}
    }
}
$cases=if($Case -eq 'All') { @('ContextRecovery','ModelRecovery','InstructionRecovery','ModelForeign','InstructionForeign','Decomposition','ContextProfile','ContextConcurrent','ModelConcurrent','InstructionConcurrent') } else { @($Case) }
foreach($scenario in $cases) {
    $sandbox=Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot
    $fixture=New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'audit-continuity'
    $savedContext=[Environment]::GetEnvironmentVariable('LLM_WIKI_CONTEXT_OUTCOME_REGISTRY_PATH','Process')
    $savedModel=[Environment]::GetEnvironmentVariable('LLM_WIKI_MODEL_ROUTE_OUTCOME_REGISTRY_PATH','Process')
    try {
        $tools=Join-Path $fixture '.llm-wiki/tools'
        $policies=Join-Path $fixture '.llm-wiki/policies'
        $knowledge=Join-Path $fixture '.llm-wiki/knowledge'
        $workspace='.artifacts/llm-wiki/tasks/recovery'
        $absoluteWorkspace=Join-Path $fixture $workspace
        $null=New-Item -ItemType Directory -Path $tools,$policies,$knowledge,$absoluteWorkspace -Force
        [IO.File]::WriteAllText((Join-Path $fixture 'AGENTS.md'),'Use the current task scope and verify its outcomes.')
        foreach($name in @('workspace-policies.json','change-policies.json')) { Copy-Item -LiteralPath (Join-Path $repositoryRoot ".llm-wiki/policies/$name") -Destination $policies }
        if($scenario -eq 'Decomposition') {
            foreach($name in @('Manage-LlmWikiTaskDecomposition.ps1','Get-LlmWikiWorkspacePolicy.ps1')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $tools }
            $paths=@(1..10 | ForEach-Object { "Modules/Users/Application/Feature$_.cs"; "Modules/Meals/Application/Feature$_.cs" })
            Write-ContinuityJson (Join-Path $absoluteWorkspace 'workspace.json') @{objective='Review independent Users and Meals changes';currentPacketFingerprint=('a'*64)}
            Write-ContinuityJson (Join-Path $absoluteWorkspace 'change-packet.json') @{diff=@{changedPaths=$paths}}
            try { $result=& (Join-Path $tools 'Manage-LlmWikiTaskDecomposition.ps1') create -WorkspacePath $workspace -Format Json | ConvertFrom-Json }
            catch { throw "Current Modules owners must form independent decomposition buckets. $($_.Exception.Message)" }
            Assert-Continuity ($result.valid -and @($result.plan.shards).Count -eq 2) 'Current Modules owners must form independent decomposition buckets.'
            Assert-Continuity (@($result.plan.shards.changedPaths | ForEach-Object { @($_) }).Count -eq $paths.Count) 'Module decomposition must preserve every assigned source path.'
            continue
        }
        $owner=if($scenario.StartsWith('Context')) { 'Context' } elseif($scenario.StartsWith('Model')) { 'ModelRouting' } else { 'Instruction' }
        $manager="Manage-LlmWiki${owner}Outcome.ps1"
        $receiptName=switch($owner) { 'Context' {'context-strategy-outcome.json'} 'ModelRouting' {'model-routing-outcome.json'} default {'instruction-outcome.json'} }
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $manager) -Destination $tools
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LlmWikiJson.ps1') -Destination $tools
        $registry=Join-Path $knowledge $(switch($owner) { 'Context' {'context-strategy-outcomes.json'} 'ModelRouting' {'model-routing-outcomes.json'} default {'instruction-outcomes.json'} })
        $env:LLM_WIKI_CONTEXT_OUTCOME_REGISTRY_PATH=Join-Path $knowledge 'context-strategy-outcomes.json'
        $env:LLM_WIKI_MODEL_ROUTE_OUTCOME_REGISTRY_PATH=Join-Path $knowledge 'model-routing-outcomes.json'
        $validatedInputs=@{
            valid=$true;issues=@()
            strategy=@{state='applied';applicationHash=('a'*64);applied=@{variantId='compact';itemLimit=10;characterBudget=10000;qualityScore=90}}
            route=@{receiptHash=('d'*64);recommendation=@{routeId='fixture';rank=1;model='fixture';reasoningEffort='low';relativeCostUnits=1};signals=@{complexityScore=20;riskLevel='low'}}
            retrospective=@{retrospectiveHash=('b'*64);outcome=@{readinessScore=90;confidenceScore=90;critiqueScore=90;quality='good';impactDriftCount=0;quarantinedContextSourceCount=0;flakyCheckCount=0;risk=@{level='low';score=20};prediction=@{resolvedCount=0;falseNegativeCount=0;falsePositiveCount=0};repair=@{totalAttempts=0;failedAttempts=0}}}
        } | ConvertTo-Json -Depth 20 -Compress
        $stub="param([string]`$Action,[string]`$WorkspacePath,[string]`$Format)`n'$($validatedInputs.Replace("'","''"))'"
        foreach($name in @('Manage-LlmWikiContextStrategy.ps1','Manage-LlmWikiModelRouting.ps1','Manage-LlmWikiRetrospective.ps1')) { [IO.File]::WriteAllText((Join-Path $tools $name),$stub) }
        Write-ContinuityJson (Join-Path $absoluteWorkspace 'completion.json') @{completionFingerprint=('c'*64);readiness=@{verdict='ready'}}
        foreach($name in @('retrospective.json','context-strategy-application.json','model-routing.json')) { Write-ContinuityJson (Join-Path $absoluteWorkspace $name) @{} }
        $controlData=$validatedInputs | ConvertFrom-Json
        Write-ContinuityJson (Join-Path $absoluteWorkspace 'model-routing.json') $controlData.route
        Write-ContinuityJson (Join-Path $absoluteWorkspace 'context-strategy-application.json') $controlData.strategy
        Write-ContinuityJson (Join-Path $absoluteWorkspace 'change-packet.json') @{diff=@{scopes=@('Backend');modules=@()};brief=@{instructions=@();risk=@{level='low'}}}
        if($scenario.EndsWith('Concurrent')) {
            Test-ConcurrentOutcome -Tools $tools -Manager $manager -Registry $registry -Workspace $workspace -AbsoluteWorkspace $absoluteWorkspace -Fixture $fixture -Owner $owner
            continue
        }
        if($scenario -eq 'ContextProfile') {
            Set-StrictMode -Version Latest
            try { $profile=& (Join-Path $tools $manager) profile -WorkspacePath $workspace -Format Json | ConvertFrom-Json }
            catch { throw "Empty module scopes must produce a valid context profile under StrictMode. $($_.Exception.Message)" }
            Assert-Continuity ($profile.valid -and @($profile.profile.modules).Count -eq 0) 'Empty module scopes must produce a valid context profile under StrictMode.'
            continue
        }
        $receipt=Join-Path $absoluteWorkspace $receiptName
        if($scenario.EndsWith('Recovery')) {
            $null=New-Item -ItemType Directory -Path $receipt
            $writeFailed=$false
            try { & (Join-Path $tools $manager) observe -WorkspacePath $workspace -Format Json | Out-Null }
            catch { $writeFailed=$_.Exception.Message -match 'WriteAllText|Access|denied|already exists' }
            Assert-Continuity $writeFailed 'The controlled receipt write failure must actually occur.'
            Assert-Continuity (@((Get-Content -LiteralPath $registry -Raw | ConvertFrom-Json).events).Count -eq 1) 'The failed publication must leave exactly one canonical event.'
            Remove-Item -LiteralPath $receipt
            $registryBefore=[IO.File]::ReadAllText($registry)
            $retry=& (Join-Path $tools $manager) observe -WorkspacePath $workspace -Format Json | ConvertFrom-Json
            Assert-Continuity ($retry.valid -and $retry.addedCount -eq 0 -and [IO.File]::Exists($receipt)) "$owner retry must recreate its missing outcome receipt."
            Assert-Continuity ([IO.File]::ReadAllText($registry) -ceq $registryBefore) 'Receipt recovery must leave canonical event history unchanged.'
        } else {
            $observed=& (Join-Path $tools $manager) observe -WorkspacePath $workspace -Format Json | ConvertFrom-Json
            Assert-Continuity ($observed.valid -and $observed.addedCount -eq 1 -and [IO.File]::Exists($receipt)) 'The positive control must publish a canonical event and receipt.'
        }
        $own=& (Join-Path $tools $manager) verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
        Assert-Continuity $own.valid 'A canonical receipt must verify for its own task.'
        if($scenario.EndsWith('Foreign')) {
            $foreign='.artifacts/llm-wiki/tasks/foreign'
            $foreignAbsolute=Join-Path $fixture $foreign
            $null=New-Item -ItemType Directory -Path $foreignAbsolute
            Copy-Item -LiteralPath $receipt -Destination (Join-Path $foreignAbsolute $receiptName)
            $result=& (Join-Path $tools $manager) verify -WorkspacePath $foreign -Format Json | ConvertFrom-Json
            Assert-Continuity (-not $result.valid) "$owner verifier must reject another task's outcome receipt."
        }
        Write-Host "Wiki continuity regression passed: $scenario."
    } finally {
        [Environment]::SetEnvironmentVariable('LLM_WIKI_CONTEXT_OUTCOME_REGISTRY_PATH',$savedContext,'Process')
        [Environment]::SetEnvironmentVariable('LLM_WIKI_MODEL_ROUTE_OUTCOME_REGISTRY_PATH',$savedModel,'Process')
        $target=[IO.Path]::GetFullPath($fixture)
        if([IO.Path]::GetDirectoryName($target) -cne [IO.Path]::GetFullPath($sandbox).TrimEnd('\','/') -or [IO.Path]::GetFileName($target) -notmatch '^audit-continuity-[a-f0-9]{32}$') { throw 'Unsafe audit continuity cleanup target.' }
        Remove-Item -LiteralPath $target -Recurse -Force
    }
}
Write-Host 'Wiki continuity regressions passed.'
