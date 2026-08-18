[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$sandbox = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'learning-promotion-migration'
$registryPath = Join-Path $sandbox 'learning-promotions.json'
$previousRoot = $env:LLM_WIKI_TEST_KNOWLEDGE_ROOT

function Get-TestHash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    if ($null -eq $json) { $json = 'null' }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-LegacyPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion=$Event.schemaVersion; sequence=$Event.sequence; kind=$Event.kind; id=$Event.id
        createdAtUtc=$Event.createdAtUtc; previousHash=$Event.previousHash; observation=$Event.observation
        decision=$Event.decision; targetId=$Event.targetId; reason=$Event.reason
    }
}
function Add-LegacyEvent([Collections.Generic.List[object]]$Events, [string]$Kind, [string]$Id, [object]$Observation, [object]$Decision, [string]$Reason) {
    $event = [pscustomobject][ordered]@{
        schemaVersion=1; sequence=$Events.Count + 1; kind=$Kind; id=$Id
        createdAtUtc="2026-01-01T00:00:0$($Events.Count).0000000Z"
        previousHash=$(if ($Events.Count -eq 0) { '' } else { [string]$Events[-1].eventHash })
        observation=$Observation; decision=$Decision; targetId=''; reason=$Reason; eventHash=''
    }
    # The migration validates the persisted JSON shape, so build the legacy hash
    # from that same round-tripped representation on every PowerShell platform.
    $normalizedEvent = $event | ConvertTo-Json -Depth 40 | ConvertFrom-Json
    $event.eventHash = Get-TestHash (Get-LegacyPayload $normalizedEvent)
    $Events.Add($event)
}

try {
    New-Item -ItemType Directory -Path $sandbox -Force | Out-Null
    foreach ($name in @('learning-experiments.json','eval-promotions.json','learning-health.json')) {
        Copy-Item ".llm-wiki/knowledge/$name" (Join-Path $sandbox $name)
    }
    $identity = [pscustomobject][ordered]@{ type='decision'; statement='reuse verified boundary'; tags=@('architecture') }
    $candidateId = "learning-$((Get-TestHash $identity).Substring(0,20))"
    $events = [Collections.Generic.List[object]]::new()
    foreach ($index in 1..2) {
        $observation = [pscustomobject][ordered]@{
            workspace=".artifacts/llm-wiki/tasks/legacy-$index"; retrospectiveHash="retro-$index"; completionFingerprint="completion-$index"
            packetFingerprint="packet-$index"; sourceCandidateId="candidate-$index"; type='decision'; target='durable-memory'
            statement='Reuse verified boundary'; rationale='Repeated tasks confirm the boundary.'; score=90
            evidence=@("check-$index"); tags=@('architecture')
            data=[pscustomobject]@{ recordedAtUtc='2026-01-01T00:00:00.0000000Z'; recommendedSeconds=90.0 }
            changedPaths=@('FoodDiary.Application/Users/Boundary.cs'); subjectIds=@("subject-$index")
        }
        Add-LegacyEvent $events 'observed' $candidateId $observation $null ''
    }
    Add-LegacyEvent $events 'approved' $candidateId $null ([pscustomobject]@{ target='durable-memory'; distinctTaskCount=1; averageScore=1; evidenceHash='legacy' }) 'legacy approval'
    Add-LegacyEvent $events 'applied' $candidateId $null ([pscustomobject]@{ target='durable-memory'; scopePaths=@('legacy'); observationCount=0 }) 'legacy application'
    [IO.File]::WriteAllText($registryPath, (([pscustomobject][ordered]@{ schemaVersion=1; events=@($events) } | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $env:LLM_WIKI_TEST_KNOWLEDGE_ROOT = $sandbox

    $verification = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') verify -Format Json | ConvertFrom-Json
    if (-not $verification.valid) { throw 'A valid legacy learning registry did not migrate.' }
    $migrated = Get-Content $registryPath -Raw | ConvertFrom-Json
    if ([int]$migrated.hashSchemaVersion -ne 2) { throw 'Legacy registry did not record the current hash schema.' }
    if ([int]$migrated.events[2].decision.distinctTaskCount -ne 2 -or [double]$migrated.events[2].decision.averageScore -ne 90) { throw 'Decision snapshot was not rebuilt.' }
    if ([int]$migrated.events[3].decision.observationCount -ne 2 -or @($migrated.events[3].decision.scopePaths).Count -ne 1) { throw 'Application snapshot was not rebuilt.' }
    $firstMigrationHash = (Get-FileHash $registryPath -Algorithm SHA256).Hash
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') verify -Format Json | Out-Null
    if ((Get-FileHash $registryPath -Algorithm SHA256).Hash -cne $firstMigrationHash) { throw 'Learning registry migration is not idempotent.' }

    # A current registry must also remain valid after its real formatted JSON is
    # read by a fresh invocation; this catches CLR numeric/date round-trip drift.
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') verify -Format Text | Out-Null
    if ((Get-FileHash $registryPath -Algorithm SHA256).Hash -cne $firstMigrationHash) { throw 'Current registry changed after a persisted JSON round-trip.' }

    # Exercise a real append through the public command after the JSON
    # round-trip. This is the shape used by apply/rollback in the governed
    # lifecycle and guards against invalidating earlier hashes or snapshots
    # while a new event is validated.
    $rollback = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') rollback `
        -Id $candidateId -Reason 'Regression rollback.' -AsOfUtc ([DateTime]'2026-01-01T00:00:10Z') -Format Json | ConvertFrom-Json
    if (-not $rollback.valid -or $rollback.candidate.materialization -ne 'rolled-back') { throw 'A persisted current registry could not append a governed rollback.' }
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') verify -FailOnInvalid -Format Json | Out-Null
    $migrated = Get-Content $registryPath -Raw | ConvertFrom-Json

    $migrated.events[0].eventHash = ('0' * 64)
    [IO.File]::WriteAllText($registryPath, (($migrated | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $corruptionReport = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') verify -Format Json | ConvertFrom-Json
    if ($corruptionReport.valid -or @($corruptionReport.issues | Where-Object { $_ -like '*invalid eventHash*' }).Count -eq 0) {
        throw 'A corrupted current learning registry was silently accepted.'
    }
    $modernCorruptionRejected = $false
    try { & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') verify -FailOnInvalid -Format Json | Out-Null } catch { $modernCorruptionRejected = $_.Exception.Message -match 'invalid eventHash' }
    if (-not $modernCorruptionRejected) { throw 'Strict verification did not reject a corrupted current learning registry.' }
} finally {
    if ([string]::IsNullOrWhiteSpace($previousRoot)) { Remove-Item Env:LLM_WIKI_TEST_KNOWLEDGE_ROOT -ErrorAction SilentlyContinue } else { $env:LLM_WIKI_TEST_KNOWLEDGE_ROOT = $previousRoot }
    Remove-Item $sandbox -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki learning-promotion migration regression passed: legacy snapshots migrate atomically and current corruption is rejected.'
