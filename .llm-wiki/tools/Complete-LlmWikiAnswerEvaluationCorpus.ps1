[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$DraftPath,
    [Parameter(Mandatory)][string]$OutputPath,
    [ValidateRange(1, 1000)][int]$MinimumCaseCount = 100
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
function Resolve-AnswerEvalPath([string]$Path) {
    if ([IO.Path]::IsPathRooted($Path)) { return [IO.Path]::GetFullPath($Path) }
    [IO.Path]::GetFullPath((Join-Path $repositoryRoot $Path))
}
function Assert-AnswerEval([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$resolvedDraftPath = Resolve-AnswerEvalPath $DraftPath
$resolvedOutputPath = Resolve-AnswerEvalPath $OutputPath
$draft = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $resolvedDraftPath), [Text.Encoding]::UTF8) | ConvertFrom-Json
Assert-AnswerEval ($draft.schemaVersion -eq 1) 'Unsupported answer-evaluation corpus schema.'
Assert-AnswerEval ([string]$draft.status -eq 'draft-human-query-intake') 'Answer-evaluation corpus must start as draft-human-query-intake.'
$cases = @($draft.cases)
Assert-AnswerEval ($cases.Count -ge $MinimumCaseCount) "Answer-evaluation corpus requires at least $MinimumCaseCount independently collected cases."
Assert-AnswerEval (@($cases | Group-Object id | Where-Object Count -gt 1).Count -eq 0) 'Answer-evaluation corpus contains duplicate case ids.'
$allowedSources = @('real-user-query', 'independent-human-authored')
foreach ($case in $cases) {
    $caseId = [string]$case.id
    $query = [string]$case.query
    Assert-AnswerEval (-not [string]::IsNullOrWhiteSpace($caseId)) 'Answer-evaluation case id is required.'
    Assert-AnswerEval (-not [string]::IsNullOrWhiteSpace($query) -and
        $query -notmatch '^<.*>$' -and $query -notmatch '(?i)placeholder|todo|tbd') "Case '$caseId' requires a real independently supplied query."
    Assert-AnswerEval ([string]$case.authorship.source -in $allowedSources) "Case '$caseId' has unsupported query authorship."
    Assert-AnswerEval ([bool]$case.authorship.collectedBeforeAnswerGeneration) "Case '$caseId' was not recorded before answer generation."
    Assert-AnswerEval (-not [string]::IsNullOrWhiteSpace([string]$case.authorship.authorOrSessionId)) "Case '$caseId' is missing an opaque author/session id."
    Assert-AnswerEval (@($case.requiredEvidencePaths).Count -gt 0) "Case '$caseId' requires at least one reviewed evidence path."
    foreach ($path in @($case.requiredEvidencePaths)) {
        $normalized = ([string]$path).Replace('\', '/').TrimStart('/')
        $absolute = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $normalized))
        Assert-AnswerEval ($absolute.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) "Case '$caseId' evidence path escapes the repository."
        Assert-AnswerEval (Test-Path -LiteralPath $absolute -PathType Leaf) "Case '$caseId' evidence path does not exist: $normalized."
    }
}

$thresholds = if ($null -ne $draft.thresholds) { $draft.thresholds } else {
    [pscustomobject][ordered]@{
        minimumAverageCorrectness = 3.5
        minimumAverageCompleteness = 3.5
        minimumAverageActionability = 3.25
        minimumClaimCitationCoverage = 0.95
        minimumEvidenceRecall = 0.8
        minimumValidCitationRate = 1.0
        maximumUnsupportedClaimRate = 0.05
    }
}
$frozen = [pscustomobject][ordered]@{
    schemaVersion = 1
    status = 'frozen-independent-answer-corpus'
    description = [string]$draft.description
    frozenAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    methodology = [pscustomobject][ordered]@{
        querySources = $allowedSources
        queriesCollectedBeforeAnswerGeneration = $true
        targetDerivedQueriesForbidden = $true
        reviewMustBeIndependentOfGenerator = $true
        note = 'The freeze tool validates declared provenance and never generates or rewrites query text.'
    }
    thresholds = $thresholds
    cases = $cases
}
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $resolvedOutputPath) -Force
$json = ($frozen | ConvertTo-Json -Depth 10) + [Environment]::NewLine
[IO.File]::WriteAllText($resolvedOutputPath, $json, [Text.UTF8Encoding]::new($false))
$hash = (Get-FileHash -LiteralPath $resolvedOutputPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Frozen answer-evaluation corpus: cases=$($cases.Count), sha256=$hash, path=$resolvedOutputPath"
