[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('init', 'run', 'check', 'review', 'artifact', 'summary', 'validate')]
    [string]$Action,

    [string]$Path = '.artifacts/llm-wiki/evidence.json',
    [string]$BaseRef = 'HEAD',
    [ValidateSet('Sqlite')]
    [string]$CompiledIndexSource = 'Sqlite',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string]$Id,
    [ValidateSet('pending', 'passed', 'passed-with-known-baseline-failures', 'failed', 'completed', 'not-applicable')]
    [string]$Status,
    [string]$Command,
    [string]$Reason,
    [object]$ReviewContent,
    [double]$DurationSeconds,
    [string]$OutputPath,
    [ValidateSet('screenshot', 'browser-log', 'accessibility-report', 'video')]
    [string]$ArtifactKind = 'screenshot',
    [switch]$NoExitOnFailure,
    [ValidateSet('Text', 'Json')][string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiEvidenceCommands.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSubstantiveReview.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$absolutePath = if ([System.IO.Path]::IsPathRooted($Path)) { $Path } else { Join-Path $repositoryRoot $Path }

function Write-EvidenceFile {
    param([object]$Evidence)

    $directory = Split-Path -Parent $absolutePath
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }
    $json = $Evidence | ConvertTo-Json -Depth 15
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($absolutePath, $json + [Environment]::NewLine, $utf8WithoutBom)
}

function Read-EvidenceFile {
    if (-not (Test-Path -LiteralPath $absolutePath)) {
        throw "Evidence bundle does not exist: $Path. Run evidence init first."
    }
    return Get-Content -LiteralPath $absolutePath -Raw | ConvertFrom-Json
}

function Get-EvidenceMarkdown {
    param([object]$Evidence)

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('# AI Development Evidence')
    $lines.Add('')
    $lines.Add(('- Commit: `{0}`' -f $Evidence.git.head))
    $lines.Add(('- Base: `{0}`' -f $Evidence.git.base))
    $lines.Add("- Changed paths: $(@($Evidence.change.changedPaths).Count)")
    $lines.Add("- Scopes: $(@($Evidence.change.scopes) -join ', ')")
    $lines.Add("- Modules: $(@($Evidence.change.modules) -join ', ')")
    $lines.Add('')
    $lines.Add('## Change Set')
    $lines.Add('')
    foreach ($changedPath in @($Evidence.change.changedPaths)) {
        $lines.Add("- ``$changedPath``")
    }
    if (@($Evidence.change.changedPaths).Count -eq 0) {
        $lines.Add('No changed paths were detected.')
    }
    $lines.Add('')
    $lines.Add('## Checks')
    $lines.Add('')
    if (@($Evidence.checks).Count -eq 0) {
        $lines.Add('No checks were required.')
    } else {
        $lines.Add('| Check | Status | Command | Evidence |')
        $lines.Add('| --- | --- | --- | --- |')
        foreach ($check in $Evidence.checks) {
            $detail = if (-not [string]::IsNullOrWhiteSpace([string]$check.reason)) {
                $check.reason
            } elseif ($null -ne $check.durationSeconds) {
                "$($check.durationSeconds)s"
            } else {
                ''
            }
            $lines.Add("| $($check.id) | $($check.status) | ``$($check.command)`` | $detail |")
        }
    }
    $lines.Add('')
    if ($Evidence.PSObject.Properties['supplementalChecks'] -and @($Evidence.supplementalChecks).Count -gt 0) {
        $lines.Add('## Supplemental Checks')
        $lines.Add('')
        $lines.Add('These observations do not replace required verification.')
        $lines.Add('')
        foreach ($check in $Evidence.supplementalChecks) { $lines.Add("- $($check.id): $($check.status); ``$($check.command)``") }
        $lines.Add('')
    }
    $lines.Add('## Review Obligations')
    $lines.Add('')
    if (@($Evidence.reviews).Count -eq 0) {
        $lines.Add('No explicit review obligations were triggered.')
    } else {
        $lines.Add('| Review | Status | Resolution |')
        $lines.Add('| --- | --- | --- |')
        foreach ($review in $Evidence.reviews) {
            $lines.Add("| $($review.id) | $($review.status) | $($review.reason) |")
        }
    }
    $lines.Add('')
    $lines.Add('## Browser and Visual Artifacts')
    $lines.Add('')
    if (@($Evidence.artifacts).Count -eq 0) {
        $lines.Add('No browser or visual artifacts were recorded.')
    } else {
        $lines.Add('| Kind | Path | Review | SHA-256 |')
        $lines.Add('| --- | --- | --- | --- |')
        foreach ($artifact in @($Evidence.artifacts)) {
            $lines.Add("| $($artifact.kind) | ``$($artifact.path)`` | $($artifact.reviewId) | ``$($artifact.sha256)`` |")
        }
    }
    $lines.Add('')
    $unresolvedChecks = @($Evidence.checks | Where-Object { $_.status -notin @('passed', 'passed-with-known-baseline-failures', 'not-applicable') })
    $unresolvedReviews = @($Evidence.reviews | Where-Object { $_.status -notin @('completed', 'not-applicable') })
    $lines.Add('## Handoff')
    $lines.Add('')
    $lines.Add("- Unresolved checks: $($unresolvedChecks.Count)")
    $lines.Add("- Unresolved reviews: $($unresolvedReviews.Count)")
    if ($unresolvedChecks.Count -eq 0 -and $unresolvedReviews.Count -eq 0) {
        $lines.Add('- Evidence state: complete')
    } else {
        $lines.Add('- Evidence state: incomplete')
    }
    return ($lines -join [Environment]::NewLine) + [Environment]::NewLine
}

switch ($Action) {
    'init' {
        $diffArguments = @{ BaseRef = $BaseRef; CompiledIndexSource = $CompiledIndexSource; Format = 'Json' }
        $policyArguments = @{ BaseRef = $BaseRef; Format = 'Json' }
        if ($PSBoundParameters.ContainsKey('HeadRef')) {
            $diffArguments.HeadRef = $HeadRef
            $policyArguments.HeadRef = $HeadRef
        }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) {
            $diffArguments.ChangedPath = $ChangedPath
            $policyArguments.ChangedPath = $ChangedPath
        }
        $diff = & (Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1') @diffArguments | ConvertFrom-Json
        $policy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') @policyArguments | ConvertFrom-Json
        $head = git rev-parse HEAD
        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to resolve HEAD.'
        }
        $evidence = [ordered]@{
            schemaVersion = 1
            generatedBy = '.llm-wiki/tools/Manage-LlmWikiEvidence.ps1'
            git = [ordered]@{
                head = [string]$head
                base = [string]$policy.baseRef
                requestedBase = $BaseRef
                comparedHead = $HeadRef
            }
            change = [ordered]@{
                changedPaths = @($diff.changedPaths)
                scopes = @($diff.scopes)
                modules = @($diff.modules | ForEach-Object { $_.name })
            }
            checks = @(
                $policy.requiredChecks | ForEach-Object {
                    [ordered]@{
                        id = $_.id
                        status = 'pending'
                        command = $_.command
                        durationSeconds = $null
                        reason = ''
                    }
                }
            )
            reviews = @(
                $policy.reviewObligations | ForEach-Object {
                    [ordered]@{
                        id = $_.id
                        status = 'pending'
                        description = $_.description
                        reason = ''
                    }
                }
            )
            artifacts = @()
            supplementalChecks = @()
            structuralViolations = @($policy.violations)
        }
        Write-EvidenceFile $evidence
        Write-Host "Initialized evidence bundle: $Path"
        Write-Host "Checks: $(@($evidence.checks).Count); reviews: $(@($evidence.reviews).Count); structural violations: $(@($evidence.structuralViolations).Count)"
    }
    'run' {
        if ([string]::IsNullOrWhiteSpace($Id)) {
            throw 'run requires -Id.'
        }
        $evidence = Read-EvidenceFile
        $evidenceBeforeRun = [IO.File]::ReadAllText($absolutePath)
        $entry = @($evidence.checks | Where-Object { $_.id -eq $Id } | Select-Object -First 1)
        if ($entry.Count -eq 0) {
            throw "Required check is not present in the evidence bundle: $Id"
        }
        $commandToRun = if (-not [string]::IsNullOrWhiteSpace($Command)) {
            $Command
        } else {
            [string]$entry[0].command
        }
        if ([string]::IsNullOrWhiteSpace($commandToRun)) {
            throw "Check '$Id' has no command."
        }
        $policy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
            -BaseRef ([string]$evidence.git.base) `
            -ChangedPath @($evidence.change.changedPaths) `
            -Format Json | ConvertFrom-Json
        $canonicalCheck = @($policy.requiredChecks | Where-Object id -eq $Id | Select-Object -First 1)
        if ($canonicalCheck.Count -eq 0) {
            throw "Check '$Id' is not required by the current change policy."
        }
        if ($commandToRun -cne [string]$canonicalCheck[0].command) {
            throw "Refusing non-canonical command for check '$Id'. Reinitialize evidence from the current policy."
        }
        $allowedCommand = $commandToRun -match '^dotnet (test|format|list) [A-Za-z0-9_./\\-]+(?: [A-Za-z0-9_./\\:-]+)*$' -or
            $commandToRun -match '^cd FoodDiary\.Web\.Client && npm (run [A-Za-z0-9_:-]+|audit)$' -or
            $commandToRun -match '^\./\.llm-wiki/wiki\.ps1 [A-Za-z0-9_./\\:-]+(?: [A-Za-z0-9_./\\:-]+)*$'
        if (-not $allowedCommand) {
            throw "Refusing command outside the evidence execution allowlist: $commandToRun"
        }

        Write-Host "Running evidence check '$Id': $commandToRun"
        $beforeExecution = Get-LlmWikiVerificationFingerprint $repositoryRoot
        $logsDirectory = Join-Path (Split-Path -Parent $absolutePath) 'logs'
        $null = New-Item -ItemType Directory -Path $logsDirectory -Force
        $safeId = $Id -replace '[^A-Za-z0-9_.-]', '_'
        $executionLog = Join-Path $logsDirectory "$safeId-$([guid]::NewGuid().ToString('N')).log"
        $executionStartedAt = [DateTime]::UtcNow
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $global:LASTEXITCODE = 0
        $savedErrorPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        Push-Location $repositoryRoot
        try {
        if ($env:OS -eq 'Windows_NT' -and $commandToRun -match '^\./\.llm-wiki/wiki\.ps1\s+') {
            $commandParts = @($commandToRun -split '\s+' | Where-Object { $_ })
            $scriptPath = $commandParts[0]
            $scriptArguments = @($commandParts | Select-Object -Skip 1)
            $hostExecutable = (Get-Process -Id $PID).Path
            & $hostExecutable -NoLogo -NoProfile -File $scriptPath @scriptArguments *> $executionLog
        } elseif ($env:OS -eq 'Windows_NT') {
            & cmd.exe /d /s /c $commandToRun *> $executionLog
        } else {
            & bash -lc $commandToRun *> $executionLog
        }
        $exitCode = $LASTEXITCODE
        } finally { Pop-Location; $ErrorActionPreference = $savedErrorPreference }
        $stopwatch.Stop()
        $afterExecution = Get-LlmWikiVerificationFingerprint $repositoryRoot
        $historicalExecution = [ordered]@{
            command = $commandToRun; checkout = $repositoryRoot
            startedAtUtc = $executionStartedAt.ToString('o'); completedAtUtc = [DateTime]::UtcNow.ToString('o')
            exitCode = $exitCode; durationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
            fingerprintBefore = $beforeExecution.fingerprint; fingerprintAfter = $afterExecution.fingerprint
            logPath = $executionLog; logSha256 = (Get-FileHash -LiteralPath $executionLog -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        [IO.File]::WriteAllText($executionLog + '.execution.json', (($historicalExecution | ConvertTo-Json -Depth 6) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        if ($beforeExecution.fingerprint -cne $afterExecution.fingerprint) { throw "Source changed while check '$Id' executed; actual outcome/log retained at '$executionLog' but passing evidence cannot be recorded." }
        $entry[0].status = if ($exitCode -eq 0) { 'passed' } else { 'failed' }
        $entry[0].command = $commandToRun
        $entry[0] | Add-Member -NotePropertyName executionCommand -NotePropertyValue $commandToRun -Force
        $entry[0].durationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
        $entry[0].reason = if ($exitCode -eq 0) { '' } else { "Command exited with code $exitCode." }
        $lineage = & (Join-Path $PSScriptRoot 'New-LlmWikiEvidenceLineage.ps1') `
            -Kind executed-check `
            -EvidencePath $Path `
            -Id $Id `
            -Command $commandToRun `
            -Definition $commandToRun `
            -Status $entry[0].status `
            -ExitCode $exitCode `
            -DurationSeconds $entry[0].durationSeconds `
            -ArtifactPath $executionLog `
            -Format Json | ConvertFrom-Json
        if ((Get-LlmWikiVerificationFingerprint $repositoryRoot).fingerprint -cne $beforeExecution.fingerprint) { throw "Source changed while check '$Id' execution lineage was bound; its actual historical outcome/log is retained." }
        $executionRecordRelative = ($executionLog + '.execution.json').Substring($repositoryRoot.TrimEnd('\', '/').Length + 1).Replace('\', '/')
        $lineage.execution | Add-Member -NotePropertyName verification -NotePropertyValue ([pscustomobject][ordered]@{
            fingerprint = $beforeExecution.fingerprint
            recordPath = $executionRecordRelative
            recordSha256 = (Get-FileHash -LiteralPath ($executionLog + '.execution.json') -Algorithm SHA256).Hash.ToLowerInvariant()
        })
        $entry[0] | Add-Member -NotePropertyName lineage -NotePropertyValue $lineage -Force
        try {
            Write-EvidenceFile $evidence
            if ($exitCode -eq 0) {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationReceipts.ps1') -Action Record `
                    -RepositoryRoot $repositoryRoot -Command $commandToRun -SourceEvidencePath $Path -CheckId $Id -ExpectedFingerprint $beforeExecution.fingerprint -Format Json | Out-Null
                if ((Get-LlmWikiVerificationFingerprint $repositoryRoot).fingerprint -cne $beforeExecution.fingerprint) { throw 'Repository inputs changed while execution proof was published.' }
            }
        } catch {
            [IO.File]::WriteAllText($absolutePath, $evidenceBeforeRun, [Text.UTF8Encoding]::new($false))
            throw
        }
        Write-Host "Recorded check '$Id' as '$($entry[0].status)' in $($entry[0].durationSeconds)s."
        if ($exitCode -ne 0 -and -not $NoExitOnFailure) {
            exit $exitCode
        }
    }
    'check' {
        if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($Status)) {
            throw 'check requires -Id and -Status.'
        }
        if ($Status -notin @('passed', 'passed-with-known-baseline-failures', 'failed', 'not-applicable')) {
            throw 'Check status must be passed, passed-with-known-baseline-failures, failed, or not-applicable.'
        }
        if ($Status -in @('not-applicable', 'passed-with-known-baseline-failures') -and [string]::IsNullOrWhiteSpace($Reason)) {
            throw "$Status requires -Reason. For known baseline failures, identify the failing tests and the evidence that they predate this change."
        }
        $evidence = Read-EvidenceFile
        $policy = & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') -BaseRef ([string]$evidence.git.base) -ChangedPath @($evidence.change.changedPaths) -Format Json | ConvertFrom-Json
        $requirement = @($policy.requiredChecks | Where-Object id -eq $Id | Select-Object -First 1)
        $entry = @($evidence.checks | Where-Object { $_.id -eq $Id } | Select-Object -First 1)
        if (($entry.Count -gt 0) -ne ($requirement.Count -gt 0)) { throw "Required check '$Id' differs between the bundle and current policy. Reinitialize evidence before recording this result." }
        if ($entry.Count -eq 0) {
            if ([string]::IsNullOrWhiteSpace($Command)) { throw "Unknown check '$Id'. Required IDs: $(@($evidence.checks.id) -join ', '). Supply -EvidenceCommand to record a supplemental check." }
            if (-not $evidence.PSObject.Properties['supplementalChecks']) { $evidence | Add-Member -NotePropertyName supplementalChecks -NotePropertyValue @() }
            $entry = @($evidence.supplementalChecks | Where-Object id -eq $Id | Select-Object -First 1)
            if ($entry.Count -eq 0) {
                $entry = ,([pscustomobject]@{
                    id = $Id
                    status = $Status
                    command = $Command
                    durationSeconds = $DurationSeconds
                    reason = $Reason
                })
                $evidence.supplementalChecks = @($evidence.supplementalChecks) + $entry
            }
        }
        $executedCommand = if ($Command) { $Command } elseif ($entry[0].PSObject.Properties['executionCommand']) { [string]$entry[0].executionCommand } else { [string]$entry[0].command }
        if ($requirement.Count -gt 0) {
            $canonicalCommand = [string]$requirement[0].command
            if (-not (Test-LlmWikiEvidenceExecutionCommand $canonicalCommand $executedCommand $repositoryRoot)) { throw "Execution command changes required check '$Id'. Keep '$canonicalCommand'; output flags are supported, while --no-build requires a successful current build receipt. Record a different check under a supplemental ID." }
            $entry[0].command = $canonicalCommand
        } else { $entry[0].command = $executedCommand }
        $entry[0].status = $Status
        $entry[0] | Add-Member -NotePropertyName executionCommand -NotePropertyValue $executedCommand -Force
        if ($PSBoundParameters.ContainsKey('DurationSeconds')) { $entry[0].durationSeconds = $DurationSeconds }
        $entry[0].reason = $Reason
        $lineage = & (Join-Path $PSScriptRoot 'New-LlmWikiEvidenceLineage.ps1') `
            -Kind manual-check `
            -EvidencePath $Path `
            -Id $Id `
            -Command $executedCommand `
            -Definition ([string]$entry[0].command) `
            -PolicyInput $policy `
            -Reason $Reason `
            -Status $Status `
            -DurationSeconds $(if ($PSBoundParameters.ContainsKey('DurationSeconds')) { $DurationSeconds } else { $null }) `
            -Format Json | ConvertFrom-Json
        $entry[0] | Add-Member -NotePropertyName lineage -NotePropertyValue $lineage -Force
        Write-EvidenceFile $evidence
        Write-Host "Recorded check '$Id' as '$Status'."
    }
    'review' {
        if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($Status)) {
            throw 'review requires -Id and -Status.'
        }
        if ($Status -notin @('completed', 'not-applicable')) {
            throw 'Review status must be completed or not-applicable.'
        }
        if ([string]::IsNullOrWhiteSpace($Reason)) {
            throw 'Review evidence requires -Reason.'
        }
        $evidence = Read-EvidenceFile
        if ($Id -eq 'substantive-code-review') {
            if ($Status -ne 'completed' -or $null -eq $ReviewContent) { throw 'substantive-code-review requires -Status completed and structured -ReviewContent.' }
            $record = New-LlmWikiSubstantiveReview -Content $ReviewContent -Evidence $evidence -EvidencePath $Path -Reason $Reason
            $evidence | Add-Member -NotePropertyName substantiveReview -NotePropertyValue $record -Force
            Write-EvidenceFile $evidence
            Write-Host 'Recorded substantive review of requirements and code. Open findings remain delivery blockers.'
            break
        }
        if ($null -ne $ReviewContent) { throw 'Structured ReviewContent belongs to -Id substantive-code-review; use -Reason for policy review obligations.' }
        $entry = @($evidence.reviews | Where-Object { $_.id -eq $Id } | Select-Object -First 1)
        if ($entry.Count -eq 0) {
            throw "Review obligation is not present in the evidence bundle: $Id"
        }
        $entry[0].status = $Status
        $entry[0].reason = $Reason
        $lineage = & (Join-Path $PSScriptRoot 'New-LlmWikiEvidenceLineage.ps1') `
            -Kind review-attestation `
            -EvidencePath $Path `
            -Id $Id `
            -Definition ([string]$entry[0].description) `
            -Reason $Reason `
            -Status $Status `
            -Format Json | ConvertFrom-Json
        $entry[0] | Add-Member -NotePropertyName lineage -NotePropertyValue $lineage -Force
        Write-EvidenceFile $evidence
        Write-Host "Recorded review '$Id' as '$Status'."
    }
    'artifact' {
        if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($OutputPath)) {
            throw 'artifact requires -Id and -OutputPath.'
        }
        if ([string]::IsNullOrWhiteSpace($Reason)) {
            throw 'artifact requires -Reason.'
        }
        $evidence = Read-EvidenceFile
        $review = @($evidence.reviews | Where-Object { $_.id -eq $Id } | Select-Object -First 1)
        if ($review.Count -eq 0) {
            throw "Review obligation is not present in the evidence bundle: $Id"
        }
        $absoluteArtifactPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
            [System.IO.Path]::GetFullPath($OutputPath)
        } else {
            [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
        }
        $repositoryPrefix = $repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
        if (-not $absoluteArtifactPath.StartsWith($repositoryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'Evidence artifact must be inside the repository workspace.'
        }
        if (-not (Test-Path -LiteralPath $absoluteArtifactPath -PathType Leaf)) {
            throw "Evidence artifact does not exist: $OutputPath"
        }
        $relativeArtifactPath = $absoluteArtifactPath.Substring($repositoryPrefix.Length).Replace('\', '/')
        $artifactHash = (Get-FileHash -LiteralPath $absoluteArtifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
        $artifact = [pscustomobject]@{
            kind = $ArtifactKind
            path = $relativeArtifactPath
            reviewId = $Id
            sha256 = $artifactHash
            reason = $Reason
        }
        $evidence | Add-Member -NotePropertyName artifacts -NotePropertyValue @($evidence.artifacts) -Force
        $evidence.artifacts = @($evidence.artifacts | Where-Object {
            $_.reviewId -ne $Id -or $_.path -ne $relativeArtifactPath
        }) + $artifact
        $review[0].status = 'completed'
        $review[0].reason = "$Reason Artifact: $relativeArtifactPath ($ArtifactKind, sha256=$artifactHash)."
        $lineage = & (Join-Path $PSScriptRoot 'New-LlmWikiEvidenceLineage.ps1') `
            -Kind review-attestation `
            -EvidencePath $Path `
            -Id $Id `
            -Definition ([string]$review[0].description) `
            -Reason $review[0].reason `
            -Status completed `
            -Format Json | ConvertFrom-Json
        $review[0] | Add-Member -NotePropertyName lineage -NotePropertyValue $lineage -Force
        Write-EvidenceFile $evidence
        Write-Host "Recorded $ArtifactKind artifact '$relativeArtifactPath' and completed review '$Id'."
    }
    'summary' {
        $evidence = Read-EvidenceFile
        $markdown = Get-EvidenceMarkdown $evidence
        if ([string]::IsNullOrWhiteSpace($OutputPath)) {
            if ($Format -eq 'Text') { Write-Output $markdown }
        } else {
            $absoluteOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
                $OutputPath
            } else {
                Join-Path $repositoryRoot $OutputPath
            }
            $outputDirectory = Split-Path -Parent $absoluteOutputPath
            if (-not (Test-Path -LiteralPath $outputDirectory)) {
                New-Item -ItemType Directory -Path $outputDirectory | Out-Null
            }
            $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
            [System.IO.File]::WriteAllText($absoluteOutputPath, $markdown, $utf8WithoutBom)
            Write-Host "Wrote evidence summary: $OutputPath"
        }
    }
    'validate' {
        $currentEvidence = Read-EvidenceFile
        & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
            -BaseRef ([string]$currentEvidence.git.base) `
            -ChangedPath @($currentEvidence.change.changedPaths) `
            -EvidencePath $Path `
            -RequireEvidence `
            -Format $Format `
            -FailOnViolation
        return
    }
}
if ($Format -eq 'Json') {
    [pscustomobject]@{ schemaVersion = 1; action = $Action; evidencePath = $Path; evidence = (Read-EvidenceFile) } | ConvertTo-Json -Depth 25
}
