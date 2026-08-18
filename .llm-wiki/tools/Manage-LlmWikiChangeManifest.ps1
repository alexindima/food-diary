[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('init', 'show', 'validate')]
    [string]$Action = 'show',
    [string]$Path = '.artifacts/llm-wiki/change-manifest.json',
    [string]$Objective,
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string[]]$PlannedPath = @(),
    [string[]]$AllowedPath = @(),
    [string[]]$ExcludedPath = @(),
    [string]$EvidencePath = '.artifacts/llm-wiki/evidence.json',
    [switch]$RequireEvidence,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$absolutePath = if ([System.IO.Path]::IsPathRooted($Path)) { $Path } else { Join-Path $repositoryRoot $Path }
$absoluteEvidencePath = if ([System.IO.Path]::IsPathRooted($EvidencePath)) { $EvidencePath } else { Join-Path $repositoryRoot $EvidencePath }
$hasHeadRef = $PSBoundParameters.ContainsKey('HeadRef')
$hasChangedPath = $PSBoundParameters.ContainsKey('ChangedPath')
$ChangedPath = @($ChangedPath | Where-Object { $_ })
$PlannedPath = @($PlannedPath | Where-Object { $_ })
$AllowedPath = @($AllowedPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$ExcludedPath = @($ExcludedPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

function Write-Manifest([object]$Manifest) {
    $directory = Split-Path -Parent $absolutePath
    if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($absolutePath, (($Manifest | ConvertTo-Json -Depth 15) + [Environment]::NewLine), $utf8WithoutBom)
}

function Read-Manifest {
    if (-not (Test-Path -LiteralPath $absolutePath)) { throw "Change manifest does not exist: $Path" }
    Get-Content -LiteralPath $absolutePath -Raw | ConvertFrom-Json
}

function Test-PathMatch([string]$Value, [object[]]$Patterns) {
    $Value = $Value.Replace('\', '/')
    foreach ($pattern in @($Patterns)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$pattern) -and $Value -match $pattern) { return $true }
    }
    return $false
}

function Get-Fingerprint([object]$Value) {
    $text = $Value | ConvertTo-Json -Depth 15 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha256.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha256.Dispose() }
}

function Get-ChangeInputs([string]$SelectedBaseRef) {
    $briefArguments = @{ BaseRef = $SelectedBaseRef; Format = 'Json'; Intent = $Objective }
    $planArguments = @{ BaseRef = $SelectedBaseRef; Format = 'Json'; Objective = $Objective }
    if ($hasHeadRef) {
        $briefArguments.HeadRef = $HeadRef
        $planArguments.HeadRef = $HeadRef
    }
    if ($hasChangedPath) {
        $briefArguments.ChangedPath = $ChangedPath
        $planArguments.ChangedPath = $ChangedPath
    }
    elseif (@($PlannedPath).Count -gt 0) {
        $briefArguments.ProposedPath = @($PlannedPath)
        $planArguments.ChangedPath = @($PlannedPath)
    }
    $brief = & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskBrief.ps1') @briefArguments | ConvertFrom-Json
    $plan = & (Join-Path $PSScriptRoot 'Get-LlmWikiImplementationPlan.ps1') @planArguments -BriefInput $brief | ConvertFrom-Json
    return [pscustomobject]@{ brief = $brief; plan = $plan }
}

switch ($Action) {
    'init' {
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'manifest init requires -Objective.' }
        foreach ($pattern in @($AllowedPath + $ExcludedPath)) {
            try { $null = [regex]::new($pattern) } catch { throw "Invalid path regex: $pattern" }
        }
        $inputs = Get-ChangeInputs $BaseRef
        $candidatePlannedPaths = @(if (@($PlannedPath).Count -gt 0) { @($PlannedPath) } else { @($inputs.brief.change.paths) })
        $plannedPaths = @($candidatePlannedPaths | ForEach-Object { ([string]$_).Replace('\', '/').TrimEnd('/') } | Where-Object {
            $_ -notmatch '^\.llm-wiki/(?:generated|reviews)/' -and
            $_ -notmatch '^\.artifacts/llm-wiki/'
        } | Sort-Object -Unique)
        $allowedPatterns = @(if (@($AllowedPath).Count -gt 0) {
            @($AllowedPath)
        } else {
            @($plannedPaths | ForEach-Object { '^' + [regex]::Escape($_) + '$' })
        })
        if (@($AllowedPath).Count -gt 0) {
            $plannedPaths = @($plannedPaths | Where-Object {
                $candidate = [string]$_
                @($AllowedPath | Where-Object { $candidate -match [string]$_ }).Count -gt 0 -and
                @($ExcludedPath | Where-Object { $candidate -match [string]$_ }).Count -eq 0
            })
        }
        if ($allowedPatterns.Count -eq 0) { throw 'manifest init requires changed paths or at least one -AllowedPath regex.' }
        $head = git rev-parse HEAD
        if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD.' }
        $planSnapshot = [ordered]@{
            risk = $inputs.plan.risk
            scopes = @($inputs.plan.scopes)
            directModules = @($inputs.plan.modules.direct)
            downstreamModules = @($inputs.plan.modules.downstream)
            phases = @($inputs.plan.phases | ForEach-Object {
                [ordered]@{ order = $_.order; id = $_.id; title = $_.title; files = @($_.files) }
            })
            requiredChecks = @($inputs.brief.requiredChecks | ForEach-Object {
                [ordered]@{ id = $_.id; command = $_.command }
            } | Sort-Object id, command)
            reviewObligations = @($inputs.brief.reviewObligations | ForEach-Object {
                [ordered]@{ id = $_.id; description = $_.description }
            } | Sort-Object id)
            scenarios = @($inputs.brief.testScenarios | ForEach-Object {
                [ordered]@{ id = $_.id; evidence = $_.evidence }
            } | Sort-Object id)
            generatedActions = @($inputs.brief.generatedActions | Sort-Object -Unique)
            rolloutFlags = $inputs.brief.rolloutFlags
        }
        $resolvedBase = git rev-parse --verify "$BaseRef^{commit}"
        if ($LASTEXITCODE -ne 0) { throw "Unable to resolve BaseRef '$BaseRef'." }
        $manifest = [ordered]@{
            schemaVersion = 1
            objective = $Objective
            createdAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
            git = [ordered]@{ base = ([string]$resolvedBase).Trim(); requestedBase = $BaseRef; headAtInit = ([string]$head).Trim() }
            scope = [ordered]@{
                plannedPaths = $plannedPaths
                allowedPathPatterns = $allowedPatterns
                excludedPathPatterns = @($ExcludedPath)
            }
            plan = $planSnapshot
            planFingerprint = Get-Fingerprint $planSnapshot
            evidencePath = $EvidencePath
        }
        Write-Manifest $manifest
        Write-Host "Initialized change manifest: $Path"
        Write-Host "Planned paths: $($plannedPaths.Count); phases: $(@($planSnapshot.phases).Count); checks: $(@($planSnapshot.requiredChecks).Count)."
    }
    'validate' {
        $manifest = Read-Manifest
        $manifestDirectory = (Split-Path -Parent $Path).Replace('\', '/').TrimEnd('/')
        $normalizedManifestEvidence = ([string]$manifest.evidencePath).Replace('\', '/')
        $workspaceEvidenceMismatch = $manifestDirectory -match '^\.artifacts/llm-wiki/tasks/' -and
            $normalizedManifestEvidence -cne "$manifestDirectory/evidence.json"
        $Objective = $manifest.objective
        $BaseRef = $manifest.git.base
        $inputs = Get-ChangeInputs $BaseRef
        $actualPaths = @($inputs.brief.change.paths)
        $outOfScope = @($actualPaths | Where-Object {
            -not (Test-PathMatch $_ @($manifest.scope.allowedPathPatterns)) -or
            (Test-PathMatch $_ @($manifest.scope.excludedPathPatterns))
        })
        $plannedNotChanged = @($manifest.scope.plannedPaths | Where-Object { $_ -notin $actualPaths })
        $currentChecks = @($inputs.brief.requiredChecks | ForEach-Object { "$($_.id)|$($_.command)" } | Sort-Object -Unique)
        $manifestChecks = @($manifest.plan.requiredChecks | ForEach-Object { "$($_.id)|$($_.command)" } | Sort-Object -Unique)
        $newChecks = @($currentChecks | Where-Object { $_ -notin $manifestChecks })
        $currentReviews = @($inputs.brief.reviewObligations | ForEach-Object { if ($_.PSObject.Properties['id']) { [string]$_.id } } | Where-Object { $_ } | Sort-Object -Unique)
        $manifestReviews = @($manifest.plan.reviewObligations | ForEach-Object { if ($_.PSObject.Properties['id']) { [string]$_.id } } | Where-Object { $_ } | Sort-Object -Unique)
        $newReviews = @($currentReviews | Where-Object { $_ -notin $manifestReviews })
        $structuralViolations = @($inputs.brief.structuralViolations)
        $unresolvedEvidence = [System.Collections.Generic.List[object]]::new()
        $evidenceMissing = $false
        if ($RequireEvidence) {
            if (-not (Test-Path -LiteralPath $absoluteEvidencePath)) {
                $evidenceMissing = $true
            } else {
                $evidence = Get-Content -LiteralPath $absoluteEvidencePath -Raw | ConvertFrom-Json
                foreach ($check in @($manifest.plan.requiredChecks)) {
                    $entry = $evidence.checks | Where-Object id -eq $check.id | Select-Object -First 1
                    if ($null -eq $entry -or $entry.status -notin @('passed', 'passed-with-known-baseline-failures', 'not-applicable')) {
                        $unresolvedEvidence.Add([pscustomobject]@{ kind = 'check'; id = $check.id; status = $entry.status })
                    }
                }
                foreach ($review in @($manifest.plan.reviewObligations)) {
                    $entry = $evidence.reviews | Where-Object id -eq $review.id | Select-Object -First 1
                    if ($null -eq $entry -or $entry.status -notin @('completed', 'not-applicable')) {
                        $unresolvedEvidence.Add([pscustomobject]@{ kind = 'review'; id = $review.id; status = $entry.status })
                    }
                }
            }
        }
        $valid = $outOfScope.Count -eq 0 -and $newChecks.Count -eq 0 -and $newReviews.Count -eq 0 -and -not $workspaceEvidenceMismatch -and
            $structuralViolations.Count -eq 0 -and -not $evidenceMissing -and $unresolvedEvidence.Count -eq 0
        $result = [pscustomobject][ordered]@{
            valid = $valid
            objective = $manifest.objective
            changedPaths = $actualPaths
            outOfScope = $outOfScope
            plannedNotChanged = $plannedNotChanged
            newRequiredChecks = $newChecks
            newReviewObligations = $newReviews
            structuralViolations = $structuralViolations
            evidenceRequired = [bool]$RequireEvidence
            evidenceMissing = $evidenceMissing
            workspaceEvidenceMismatch = $workspaceEvidenceMismatch
            unresolvedEvidence = @($unresolvedEvidence)
        }
        if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 10 } else {
            Write-Host "Change manifest: $(if ($valid) { 'valid' } else { 'invalid' }); $($actualPaths.Count) changed path(s), $($outOfScope.Count) out of scope."
            foreach ($item in $outOfScope) { Write-Host " - OUT OF SCOPE: $item" }
            foreach ($item in $newChecks) { Write-Host " - NEW CHECK: $item" }
            foreach ($item in $newReviews) { Write-Host " - NEW REVIEW: $item" }
            if ($evidenceMissing) { Write-Host " - EVIDENCE MISSING: $EvidencePath" }
            if ($workspaceEvidenceMismatch) { Write-Host " - WORKSPACE EVIDENCE PATH MISMATCH: $normalizedManifestEvidence (expected $manifestDirectory/evidence.json)" }
            foreach ($item in $unresolvedEvidence) { Write-Host " - UNRESOLVED $($item.kind): $($item.id) [$($item.status)]" }
        }
        if ($FailOnInvalid -and -not $valid) { exit 1 }
    }
    default {
        $manifest = Read-Manifest
        if ($Format -eq 'Json') { $manifest | ConvertTo-Json -Depth 15; exit 0 }
        Write-Host "Objective: $($manifest.objective)"
        Write-Host "Base: $($manifest.git.base)"
        Write-Host "Plan fingerprint: $($manifest.planFingerprint)"
        Write-Host "Planned paths: $(@($manifest.scope.plannedPaths).Count)"
        Write-Host "Phases: $(@($manifest.plan.phases).Count)"
        Write-Host "Required checks: $(@($manifest.plan.requiredChecks).Count)"
        Write-Host "Review obligations: $(@($manifest.plan.reviewObligations).Count)"
    }
}
