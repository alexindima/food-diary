[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('init', 'run', 'check', 'review', 'artifact', 'summary', 'validate')]
    [string]$Action,

    [string]$Path = '.artifacts/llm-wiki/evidence.json',
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string]$Id,
    [ValidateSet('pending', 'passed', 'failed', 'completed', 'not-applicable')]
    [string]$Status,
    [string]$Command,
    [string]$Reason,
    [double]$DurationSeconds,
    [string]$OutputPath,
    [ValidateSet('screenshot', 'browser-log', 'accessibility-report', 'video')]
    [string]$ArtifactKind = 'screenshot',
    [switch]$NoExitOnFailure
)

$ErrorActionPreference = 'Stop'
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
    $unresolvedChecks = @($Evidence.checks | Where-Object { $_.status -notin @('passed', 'not-applicable') })
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
        $diffArguments = @{ BaseRef = $BaseRef; Format = 'Json' }
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
                base = $BaseRef
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
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $global:LASTEXITCODE = 0
        if ($env:OS -eq 'Windows_NT' -and $commandToRun -match '^\./\.llm-wiki/wiki\.ps1\s+') {
            $commandParts = @($commandToRun -split '\s+' | Where-Object { $_ })
            $scriptPath = $commandParts[0]
            $scriptArguments = @($commandParts | Select-Object -Skip 1)
            $hostExecutable = (Get-Process -Id $PID).Path
            & $hostExecutable -NoLogo -NoProfile -File $scriptPath @scriptArguments
        } elseif ($env:OS -eq 'Windows_NT') {
            & cmd.exe /d /s /c $commandToRun
        } else {
            & bash -lc $commandToRun
        }
        $exitCode = $LASTEXITCODE
        $stopwatch.Stop()
        $entry[0].status = if ($exitCode -eq 0) { 'passed' } else { 'failed' }
        $entry[0].command = $commandToRun
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
            -Format Json | ConvertFrom-Json
        $entry[0] | Add-Member -NotePropertyName lineage -NotePropertyValue $lineage -Force
        Write-EvidenceFile $evidence
        Write-Host "Recorded check '$Id' as '$($entry[0].status)' in $($entry[0].durationSeconds)s."
        if ($exitCode -ne 0 -and -not $NoExitOnFailure) {
            exit $exitCode
        }
    }
    'check' {
        if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($Status)) {
            throw 'check requires -Id and -Status.'
        }
        if ($Status -notin @('passed', 'failed', 'not-applicable')) {
            throw 'Check status must be passed, failed, or not-applicable.'
        }
        if ($Status -eq 'not-applicable' -and [string]::IsNullOrWhiteSpace($Reason)) {
            throw 'not-applicable requires -Reason.'
        }
        $evidence = Read-EvidenceFile
        $entry = @($evidence.checks | Where-Object { $_.id -eq $Id } | Select-Object -First 1)
        if ($entry.Count -eq 0) {
            $entry = [pscustomobject]@{
                id = $Id
                status = $Status
                command = $Command
                durationSeconds = $DurationSeconds
                reason = $Reason
            }
            $evidence.checks = @($evidence.checks) + $entry
        } else {
            $entry[0].status = $Status
            if (-not [string]::IsNullOrWhiteSpace($Command)) { $entry[0].command = $Command }
            if ($PSBoundParameters.ContainsKey('DurationSeconds')) { $entry[0].durationSeconds = $DurationSeconds }
            $entry[0].reason = $Reason
        }
        $lineage = & (Join-Path $PSScriptRoot 'New-LlmWikiEvidenceLineage.ps1') `
            -Kind manual-check `
            -EvidencePath $Path `
            -Id $Id `
            -Command ([string]$entry[0].command) `
            -Definition ([string]$entry[0].command) `
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
            Write-Output $markdown
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
        & (Join-Path $PSScriptRoot 'Test-LlmWikiChangePolicy.ps1') `
            -ChangedPath @((Read-EvidenceFile).change.changedPaths) `
            -EvidencePath $Path `
            -RequireEvidence `
            -FailOnViolation
    }
}
