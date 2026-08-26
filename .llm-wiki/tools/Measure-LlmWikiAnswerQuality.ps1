[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CorpusPath,
    [Parameter(Mandatory)][string]$SubmissionPath,
    [Parameter(Mandatory)][string]$ReviewPath,
    [switch]$FailOnRegression,
    [ValidateSet('Text', 'Json')][string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
function Resolve-AnswerEvalPath([string]$Path) {
    if ([IO.Path]::IsPathRooted($Path)) { return [IO.Path]::GetFullPath($Path) }
    [IO.Path]::GetFullPath((Join-Path $repositoryRoot $Path))
}
function Read-AnswerEvalJson([string]$Path) {
    [IO.File]::ReadAllText((Resolve-Path -LiteralPath (Resolve-AnswerEvalPath $Path)), [Text.Encoding]::UTF8) | ConvertFrom-Json
}
function Assert-AnswerEval([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Get-Rate([double]$Numerator, [double]$Denominator) {
    if ($Denominator -le 0) { return 0.0 }
    [Math]::Round($Numerator / $Denominator, 4, [MidpointRounding]::AwayFromZero)
}

$corpus = Read-AnswerEvalJson $CorpusPath
$submission = Read-AnswerEvalJson $SubmissionPath
$review = Read-AnswerEvalJson $ReviewPath
Assert-AnswerEval ($corpus.schemaVersion -eq 1 -and [string]$corpus.status -eq 'frozen-independent-answer-corpus') 'Answer-quality measurement requires a frozen independent corpus.'
Assert-AnswerEval ($submission.schemaVersion -eq 1 -and @($submission.answers).Count -gt 0) 'Answer submission is invalid or empty.'
Assert-AnswerEval ($review.schemaVersion -eq 1 -and [bool]$review.independentOfGenerator) 'Answer review must declare independence from the generator.'
Assert-AnswerEval (-not [string]::IsNullOrWhiteSpace([string]$submission.generatorId)) 'Answer submission generatorId is required.'
Assert-AnswerEval (-not [string]::IsNullOrWhiteSpace([string]$review.reviewerId) -and
    [string]$review.reviewerId -ne [string]$submission.generatorId) 'Answer reviewer must differ from the generator.'

$cases = @($corpus.cases)
$answers = @($submission.answers)
$reviews = @($review.caseReviews)
Assert-AnswerEval (@($answers | Group-Object id | Where-Object Count -gt 1).Count -eq 0) 'Answer submission contains duplicate ids.'
Assert-AnswerEval (@($reviews | Group-Object id | Where-Object Count -gt 1).Count -eq 0) 'Answer review contains duplicate ids.'
$expectedIds = @($cases.id | Sort-Object)
Assert-AnswerEval ((@($answers.id | Sort-Object) -join "`0") -ceq ($expectedIds -join "`0")) 'Answer submission ids do not exactly match the corpus.'
Assert-AnswerEval ((@($reviews.id | Sort-Object) -join "`0") -ceq ($expectedIds -join "`0")) 'Answer review ids do not exactly match the corpus.'

$results = [Collections.Generic.List[object]]::new()
$totalClaims = 0
$citedClaims = 0
$totalCitations = 0
$validCitations = 0
$requiredEvidence = 0
$citedRequiredEvidence = 0
$unsupportedClaims = 0
foreach ($case in $cases) {
    $answer = @($answers | Where-Object id -eq $case.id)[0]
    $caseReview = @($reviews | Where-Object id -eq $case.id)[0]
    Assert-AnswerEval (-not [string]::IsNullOrWhiteSpace([string]$answer.answer)) "Answer '$($case.id)' is empty."
    $claims = @($answer.claims)
    Assert-AnswerEval ($claims.Count -gt 0) "Answer '$($case.id)' must expose claim-to-citation structure."
    $caseCitationPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $caseClaimsWithCitations = 0
    $caseValidCitations = 0
    $caseCitationCount = 0
    foreach ($claim in $claims) {
        Assert-AnswerEval (-not [string]::IsNullOrWhiteSpace([string]$claim.text)) "Answer '$($case.id)' contains an empty claim."
        $citations = @($claim.citations)
        if ($citations.Count -gt 0) { $caseClaimsWithCitations++; $citedClaims++ }
        $totalClaims++
        foreach ($citation in $citations) {
            $caseCitationCount++
            $totalCitations++
            $normalized = ([string]$citation.path).Replace('\', '/').TrimStart('/')
            $absolute = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $normalized))
            $insideRepository = $absolute.StartsWith($repositoryRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
            $exists = $insideRepository -and (Test-Path -LiteralPath $absolute -PathType Leaf)
            $lineValid = $true
            if ($null -ne $citation.PSObject.Properties['line'] -and [int]$citation.line -gt 0) {
                $lineCount = if ($exists) { ([IO.File]::ReadLines($absolute) | Measure-Object).Count } else { 0 }
                $lineValid = $exists -and [int]$citation.line -le $lineCount
            }
            if ($exists -and $lineValid) {
                $caseValidCitations++
                $validCitations++
                [void]$caseCitationPaths.Add($normalized)
            }
        }
    }
    $caseRequiredEvidence = @($case.requiredEvidencePaths)
    $caseEvidenceHits = @($caseRequiredEvidence | Where-Object { $caseCitationPaths.Contains(([string]$_).Replace('\', '/')) }).Count
    $requiredEvidence += $caseRequiredEvidence.Count
    $citedRequiredEvidence += $caseEvidenceHits
    $caseUnsupportedClaims = [int]$caseReview.unsupportedClaimCount
    $unsupportedClaims += $caseUnsupportedClaims
    foreach ($scoreName in @('correctness', 'completeness', 'actionability')) {
        $score = [double]$caseReview.$scoreName
        Assert-AnswerEval ($score -ge 0 -and $score -le 4) "Review '$($case.id)' $scoreName must be between 0 and 4."
    }
    Assert-AnswerEval ($caseUnsupportedClaims -ge 0 -and $caseUnsupportedClaims -le $claims.Count) "Review '$($case.id)' has an invalid unsupported-claim count."
    $results.Add([pscustomobject][ordered]@{
        id = [string]$case.id
        claimCount = $claims.Count
        claimCitationCoverage = Get-Rate $caseClaimsWithCitations $claims.Count
        citationCount = $caseCitationCount
        validCitationRate = Get-Rate $caseValidCitations $caseCitationCount
        evidenceRecall = Get-Rate $caseEvidenceHits $caseRequiredEvidence.Count
        correctness = [double]$caseReview.correctness
        completeness = [double]$caseReview.completeness
        actionability = [double]$caseReview.actionability
        unsupportedClaimCount = $caseUnsupportedClaims
        reviewNotesPresent = -not [string]::IsNullOrWhiteSpace([string]$caseReview.notes)
    })
}

$metrics = [pscustomobject][ordered]@{
    averageCorrectness = [Math]::Round([double](($results.correctness | Measure-Object -Average).Average), 4)
    averageCompleteness = [Math]::Round([double](($results.completeness | Measure-Object -Average).Average), 4)
    averageActionability = [Math]::Round([double](($results.actionability | Measure-Object -Average).Average), 4)
    claimCitationCoverage = Get-Rate $citedClaims $totalClaims
    validCitationRate = Get-Rate $validCitations $totalCitations
    evidenceRecall = Get-Rate $citedRequiredEvidence $requiredEvidence
    unsupportedClaimRate = Get-Rate $unsupportedClaims $totalClaims
}
$thresholds = $corpus.thresholds
$passed = $metrics.averageCorrectness -ge [double]$thresholds.minimumAverageCorrectness -and
    $metrics.averageCompleteness -ge [double]$thresholds.minimumAverageCompleteness -and
    $metrics.averageActionability -ge [double]$thresholds.minimumAverageActionability -and
    $metrics.claimCitationCoverage -ge [double]$thresholds.minimumClaimCitationCoverage -and
    $metrics.validCitationRate -ge [double]$thresholds.minimumValidCitationRate -and
    $metrics.evidenceRecall -ge [double]$thresholds.minimumEvidenceRecall -and
    $metrics.unsupportedClaimRate -le [double]$thresholds.maximumUnsupportedClaimRate
$evaluation = [pscustomobject][ordered]@{
    schemaVersion = 1
    passed = $passed
    caseCount = $cases.Count
    corpusStatus = [string]$corpus.status
    generatorId = [string]$submission.generatorId
    reviewerId = [string]$review.reviewerId
    metrics = $metrics
    thresholds = $thresholds
    failures = @($results | Where-Object {
        $_.claimCitationCoverage -lt [double]$thresholds.minimumClaimCitationCoverage -or
        $_.validCitationRate -lt [double]$thresholds.minimumValidCitationRate -or
        $_.evidenceRecall -lt [double]$thresholds.minimumEvidenceRecall -or
        $_.unsupportedClaimCount -gt 0
    })
    results = @($results)
}
if ($Format -eq 'Json') { $evaluation | ConvertTo-Json -Depth 8 } else {
    Write-Host "Answer quality: passed=$passed, cases=$($cases.Count), correctness=$($metrics.averageCorrectness)/4, completeness=$($metrics.averageCompleteness)/4, actionability=$($metrics.averageActionability)/4, claim-citations=$($metrics.claimCitationCoverage), valid-citations=$($metrics.validCitationRate), evidence-recall=$($metrics.evidenceRecall), unsupported=$($metrics.unsupportedClaimRate)."
}
if ($FailOnRegression -and -not $passed) { throw 'Answer-quality evaluation regressed below its frozen thresholds.' }
