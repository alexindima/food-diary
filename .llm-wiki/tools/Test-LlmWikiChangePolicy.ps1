[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string]$EvidencePath,
    [switch]$RequireEvidence,
    [switch]$FailOnViolation,
    [switch]$Detailed,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$policyPath = Join-Path $wikiRoot 'policies/change-policies.json'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$requestedBaseRef = $BaseRef
$BaseRef = Resolve-LlmWikiCommitRef -RepositoryRoot $repositoryRoot -Ref $BaseRef

function ConvertTo-RepositoryPath {
    param([string]$Path)

    return ConvertTo-LlmWikiRepositoryPath $Path
}

if (-not $PSBoundParameters.ContainsKey('ChangedPath')) {
    $workspaceHead = Test-LlmWikiWorkspaceHeadRef $HeadRef
    $gitArguments = @('diff', '--name-only', '--diff-filter=ACMRD', $BaseRef)
    if (-not $workspaceHead) {
        $gitArguments += $HeadRef
    }
    $gitArguments += '--'
    $ChangedPath = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments $gitArguments -FailureMessage "git diff failed for base '$BaseRef' and head '$HeadRef'.")
    if ($workspaceHead) {
        $ChangedPath += @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'git ls-files failed while collecting untracked paths.')
    }
}

$allChangedPaths = @(
    $ChangedPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { ConvertTo-RepositoryPath $_ } |
        Sort-Object -Unique
)
$derivedWikiPaths = @($allChangedPaths | Where-Object { $_ -match '^\.llm-wiki/(?:generated|reviews)/' })
$operationalArtifacts = @($allChangedPaths | Where-Object {
    $_ -eq '.llm-wiki/knowledge/verification-telemetry.json' -or
    $_ -match '^\.artifacts/llm-wiki/'
})
$changedPaths = @($allChangedPaths | Where-Object {
    $_ -notin $derivedWikiPaths -and $_ -notin $operationalArtifacts
})
$primaryChangedPaths = $changedPaths
$testOnlyChange = $primaryChangedPaths.Count -gt 0 -and @($primaryChangedPaths | Where-Object {
    $_ -notmatch '(?i)(^|/)(tests?|__tests__)/' -and
    $_ -notmatch '(?i)\.Tests?/' -and
    $_ -notmatch '(?i)(?:^|/)[^/]*(?:Tests?|Specs?)\.(?:cs|ts)$' -and
    $_ -notmatch '(?i)(?:spec|test)\.ts$'
}).Count -eq 0
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$matchedRules = [System.Collections.Generic.List[object]]::new()
$requiredChecksById = [ordered]@{}
$reviewObligationsById = [ordered]@{}
$violations = [System.Collections.Generic.List[object]]::new()

foreach ($rule in $policy.rules) {
    if ($testOnlyChange -and $rule.id -ne 'llm-wiki') { continue }
    $matchingPaths = @(
        $changedPaths | Where-Object {
            $candidatePath = $_
            $included = @($rule.pathPatterns | Where-Object { $candidatePath -match $_ }).Count -gt 0
            $excluded = @($rule.excludePatterns | Where-Object { $candidatePath -match $_ }).Count -gt 0
            $included -and -not $excluded
        }
    )
    if ($matchingPaths.Count -eq 0) {
        continue
    }

    $matchedRules.Add([pscustomobject][ordered]@{
        id = $rule.id
        description = $rule.description
        matchedPaths = $matchingPaths
    })
    foreach ($check in @($rule.requiredChecks)) {
        if (-not $requiredChecksById.Contains($check.id)) {
            $requiredChecksById[$check.id] = [pscustomobject][ordered]@{
                id = $check.id
                command = ([string]$check.command).Replace('{BaseRef}', $BaseRef)
                sourceRule = $rule.id
            }
        }
    }
    foreach ($obligation in @($rule.reviewObligations)) {
        if (-not $reviewObligationsById.Contains($obligation.id)) {
            $reviewObligationsById[$obligation.id] = [pscustomobject][ordered]@{
                id = $obligation.id
                description = $obligation.description
                sourceRule = $rule.id
            }
        }
    }

    $structuralChecks = if ($rule.PSObject.Properties['structuralChecks']) { @($rule.structuralChecks) } else { @() }
    foreach ($structuralCheck in $structuralChecks) {
        if ($structuralCheck -eq 'paired-locales') {
            foreach ($localePath in $matchingPaths) {
                if ($localePath -notmatch '^FoodDiary\.Web\.Client/assets/i18n/(?<locale>en|ru)/(?<file>.+\.json)$') {
                    continue
                }
                $otherLocale = if ($Matches['locale'] -eq 'en') { 'ru' } else { 'en' }
                $pairedPath = "FoodDiary.Web.Client/assets/i18n/$otherLocale/$($Matches['file'])"
                if ($changedPaths -notcontains $pairedPath) {
                    $violations.Add([pscustomobject][ordered]@{
                        rule = $rule.id
                        path = $localePath
                        message = "Paired locale file is not in the change set: $pairedPath"
                    })
                }
            }
        }
        if ($structuralCheck -eq 'migration-pair') {
            foreach ($migrationPath in $matchingPaths) {
                if ($migrationPath -match '\.Designer\.cs$|ModelSnapshot\.cs$') {
                    continue
                }
                if ($migrationPath -match '^(?<prefix>.+/Migrations/[^/]+)\.cs$') {
                    $designerPath = "$($Matches['prefix']).Designer.cs"
                    if ($changedPaths -notcontains $designerPath) {
                        $violations.Add([pscustomobject][ordered]@{
                            rule = $rule.id
                            path = $migrationPath
                            message = "Migration designer file is not in the change set: $designerPath"
                        })
                    }
                }
            }
        }
    }
}

$evidence = $null
if (-not [string]::IsNullOrWhiteSpace($EvidencePath)) {
    $absoluteEvidencePath = if ([System.IO.Path]::IsPathRooted($EvidencePath)) {
        $EvidencePath
    } else {
        Join-Path $repositoryRoot $EvidencePath
    }
    if (Test-Path -LiteralPath $absoluteEvidencePath) {
        $evidence = Get-Content -LiteralPath $absoluteEvidencePath -Raw | ConvertFrom-Json
    } elseif ($RequireEvidence) {
        $violations.Add([pscustomobject][ordered]@{
            rule = 'evidence'
            path = $EvidencePath
            message = 'Required evidence bundle does not exist.'
        })
    }
} elseif ($RequireEvidence) {
    $violations.Add([pscustomobject][ordered]@{
        rule = 'evidence'
        path = ''
        message = 'RequireEvidence was set but no EvidencePath was provided.'
    })
}

if ($null -ne $evidence) {
    foreach ($requiredCheck in $requiredChecksById.Values) {
        $entry = @($evidence.checks | Where-Object { $_.id -eq $requiredCheck.id } | Select-Object -First 1)
        if ($entry.Count -eq 0 -or $entry[0].status -notin @('passed', 'passed-with-known-baseline-failures', 'not-applicable')) {
            $violations.Add([pscustomobject][ordered]@{
                rule = 'evidence'
                path = $EvidencePath
                message = "Required check has no resolved evidence: $($requiredCheck.id)"
            })
        } elseif ($entry[0].status -in @('not-applicable', 'passed-with-known-baseline-failures') -and [string]::IsNullOrWhiteSpace([string]$entry[0].reason)) {
            $violations.Add([pscustomobject][ordered]@{
                rule = 'evidence'
                path = $EvidencePath
                message = "Evidence status '$($entry[0].status)' requires a reason: $($requiredCheck.id)"
            })
        }
    }
    foreach ($obligation in $reviewObligationsById.Values) {
        $entry = @($evidence.reviews | Where-Object { $_.id -eq $obligation.id } | Select-Object -First 1)
        if ($entry.Count -eq 0 -or $entry[0].status -notin @('completed', 'not-applicable')) {
            $violations.Add([pscustomobject][ordered]@{
                rule = 'evidence'
                path = $EvidencePath
                message = "Review obligation is unresolved: $($obligation.id)"
            })
        } elseif ($entry[0].status -eq 'not-applicable' -and [string]::IsNullOrWhiteSpace([string]$entry[0].reason)) {
            $violations.Add([pscustomobject][ordered]@{
                rule = 'evidence'
                path = $EvidencePath
                message = "Not-applicable review requires a reason: $($obligation.id)"
            })
        }
    }
    $lineageValidation = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') `
        -EvidencePath $EvidencePath `
        -Format Json | ConvertFrom-Json
    foreach ($lineageIssue in @($lineageValidation.issues)) {
        $violations.Add([pscustomobject][ordered]@{
            rule = 'evidence-lineage'
            path = $EvidencePath
            message = [string]$lineageIssue
        })
    }
}

$result = [ordered]@{
    schemaVersion = 1
    requestedBaseRef = $requestedBaseRef
    baseRef = $BaseRef
    allChangedPaths = $allChangedPaths
    productPaths = $changedPaths
    derivedWikiPaths = $derivedWikiPaths
    operationalArtifacts = $operationalArtifacts
    changedPaths = $changedPaths
    matchedRules = @($matchedRules)
    requiredChecks = @($requiredChecksById.Values)
    reviewObligations = @($reviewObligationsById.Values)
    violations = @($violations)
    valid = $violations.Count -eq 0
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
} else {
    Write-Host "Change policy: $($changedPaths.Count) path(s), $($matchedRules.Count) rule(s), $($violations.Count) violation(s)"
    if ($Detailed) {
        foreach ($matchedRule in $matchedRules) {
            Write-Host " - $($matchedRule.id): $($matchedRule.matchedPaths.Count) matching path(s)"
        }
    }
    if ($Detailed -and $requiredChecksById.Count -gt 0) {
        Write-Host ''
        Write-Host 'Required checks:'
        foreach ($check in $requiredChecksById.Values) {
            Write-Host " - $($check.id): $($check.command)"
        }
    }
    if ($Detailed -and $reviewObligationsById.Count -gt 0) {
        Write-Host ''
        Write-Host 'Review obligations:'
        foreach ($obligation in $reviewObligationsById.Values) {
            Write-Host " - $($obligation.id): $($obligation.description)"
        }
    }
    if ($violations.Count -gt 0 -and -not $Detailed) {
        $firstViolation = $violations[0]
        Write-Host "BLOCKED: [$($firstViolation.rule)] $($firstViolation.message)"
        Write-Host 'Diagnostic: ./.llm-wiki/wiki.ps1 policy -Detailed'
    } elseif ($violations.Count -gt 0) {
        Write-Host ''
        Write-Host 'Violations:'
        foreach ($violation in $violations) {
            Write-Host " - [$($violation.rule)] $($violation.message)"
        }
    }
}

if ($FailOnViolation -and $violations.Count -gt 0) {
    exit 1
}
