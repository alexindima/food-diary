[CmdletBinding()]
param(
    [string]$CorpusPath = '.llm-wiki/evals/behavioral-instructions.json',
    [string]$EvaluationPath,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')][string]$Format = 'Text'
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Resolve-EvalFile([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { throw 'Evaluation evidence needs a repository-relative file path.' }
    $absolute = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $Path))
    $prefix = $repositoryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $absolute.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Evaluation evidence leaves the repository: $Path" }
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { throw "Evaluation file is missing: $Path" }
    $absolute
}
function Get-EvalSha([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
$corpusAbsolute = Resolve-EvalFile $CorpusPath
$corpus = Get-Content -LiteralPath $corpusAbsolute -Raw | ConvertFrom-Json
$issues = [Collections.Generic.List[string]]::new()
if ([int]$corpus.schemaVersion -ne 1 -or @($corpus.cases).Count -eq 0 -or @($corpus.cases.id | Sort-Object -Unique).Count -ne @($corpus.cases).Count) { throw 'Behavioral corpus requires schemaVersion=1 and unique nonempty cases.' }
foreach ($case in $corpus.cases) {
    if ([string]::IsNullOrWhiteSpace([string]$case.prompt) -or @($case.rubric).Count -eq 0 -or @($case.rubric.id | Sort-Object -Unique).Count -ne @($case.rubric).Count) { throw "Invalid prompt/rubric: $($case.id)" }
}
$instructions = @(foreach ($path in $corpus.instructions) { [pscustomobject]@{ path = $path; sha256 = Get-EvalSha (Resolve-EvalFile $path) } })
$rows = [Collections.Generic.List[object]]::new()
$pairs = [Collections.Generic.List[object]]::new()
if ($EvaluationPath) {
    $request = Get-Content -LiteralPath (Resolve-EvalFile $EvaluationPath) -Raw | ConvertFrom-Json
    if (-not $request.PSObject.Properties['runs'] -or @($request.runs | Where-Object { $null -ne $_ }).Count -eq 0) { $issues.Add('A submitted evaluation requires actual run records; use no EvaluationPath to list scenarios.') }
    if ([string]$request.corpusSha256 -cne (Get-EvalSha $corpusAbsolute)) { $issues.Add('Corpus changed; run/review evidence is stale.') }
    $keys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($run in @($request.runs)) {
        $key = "$($run.caseId)/$($run.trial)/$($run.condition)"
        $case = @($corpus.cases | Where-Object id -CEQ $run.caseId)
        $runIssues = [Collections.Generic.List[string]]::new()
        if (-not $keys.Add($key)) { $runIssues.Add('Duplicate case/trial/condition.') }
        if ($case.Count -ne 1 -or $run.condition -notin @('baseline', 'guided') -or [int]$run.trial -lt 1) { $runIssues.Add('Unknown case, condition or trial.') }
        if ([string]::IsNullOrWhiteSpace([string]$run.model) -or -not $run.PSObject.Properties['settings']) { $runIssues.Add('Record the actual model and settings.') }
        $transcript = ''
        try {
            $transcriptAbsolute = Resolve-EvalFile $run.transcriptPath
            if ((Get-EvalSha $transcriptAbsolute) -cne [string]$run.transcriptSha256) { $runIssues.Add('Transcript hash changed.') }
            $transcript = Get-Content -LiteralPath $transcriptAbsolute -Raw
            if ([string]::IsNullOrWhiteSpace($transcript)) { $runIssues.Add('Transcript is empty.') }
        } catch { $runIssues.Add($_.Exception.Message) }
        $expectedInstructions = @(if ($run.condition -eq 'guided') { $instructions })
        if (@($run.instructions).Count -ne $expectedInstructions.Count) { $runIssues.Add('Instruction snapshot differs from the declared condition.') }
        foreach ($instruction in $expectedInstructions) {
            $matches = @($run.instructions | Where-Object { $_.path -ceq $instruction.path -and $_.sha256 -ceq $instruction.sha256 })
            if ($matches.Count -ne 1) { $runIssues.Add("Instruction source changed or absent: $($instruction.path)") }
        }
        if ([string]::IsNullOrWhiteSpace([string]$run.review.reviewer)) { $runIssues.Add('A named independent transcript reviewer is required.') }
        $verdicts = @($run.review.assessments)
        if ($case.Count -eq 1) {
            if ($verdicts.Count -ne @($case[0].rubric).Count) { $runIssues.Add('Review every rubric item exactly once.') }
            foreach ($rubric in $case[0].rubric) {
                $judgments = @($verdicts | Where-Object id -CEQ $rubric.id)
                if ($judgments.Count -ne 1) { $runIssues.Add("Missing or duplicated rubric judgment: $($rubric.id)"); continue }
                $judgment = $judgments[0]
                if ($judgment.verdict -notin @('passed', 'failed', 'inconclusive') -or [string]::IsNullOrWhiteSpace([string]$judgment.reason)) { $runIssues.Add("Invalid judgment: $($rubric.id)") }
                if ([string]::IsNullOrWhiteSpace([string]$judgment.evidenceQuote) -or -not $transcript.Contains([string]$judgment.evidenceQuote)) { $runIssues.Add("Judgment lacks a literal transcript anchor: $($rubric.id)") }
            }
        }
        foreach ($issue in $runIssues) { $issues.Add("${key}: $issue") }
        $rows.Add([pscustomobject][ordered]@{ caseId = $run.caseId; trial = $run.trial; condition = $run.condition; model = $run.model; settings = $run.settings; valid = $runIssues.Count -eq 0; outcome = if ($runIssues.Count) { 'invalid' } elseif (@($verdicts | Where-Object verdict -eq 'inconclusive').Count) { 'inconclusive' } elseif (@($verdicts | Where-Object verdict -eq 'failed').Count) { 'failed' } else { 'passed' }; transcriptSha256 = $run.transcriptSha256 })
    }
    foreach ($group in @($rows | Group-Object { "$($_.caseId)/$($_.trial)" })) {
        $baseline = @($group.Group | Where-Object condition -eq 'baseline')
        $guided = @($group.Group | Where-Object condition -eq 'guided')
        $comparable = $baseline.Count -eq 1 -and $guided.Count -eq 1
        if ($comparable) { $comparable = $baseline[0].valid -and $guided[0].valid -and $baseline[0].model -ceq $guided[0].model -and ($baseline[0].settings | ConvertTo-Json -Depth 10 -Compress) -ceq ($guided[0].settings | ConvertTo-Json -Depth 10 -Compress) -and $baseline[0].outcome -ne 'inconclusive' -and $guided[0].outcome -ne 'inconclusive' }
        $pairs.Add([pscustomobject]@{ caseTrial = $group.Name; comparable = $comparable; baseline = $(if ($baseline.Count -eq 1) { $baseline[0].outcome } else { 'missing' }); guided = $(if ($guided.Count -eq 1) { $guided[0].outcome } else { 'missing' }) })
    }
}
$completeCases = @($pairs | Where-Object comparable | ForEach-Object { ($_.caseTrial -split '/')[0] } | Sort-Object -Unique)
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    valid = $issues.Count -eq 0
    status = if ($issues.Count) { 'invalid-evidence' } elseif ($rows.Count -eq 0) { 'not-run' } elseif ($completeCases.Count -lt @($corpus.cases).Count) { 'insufficient-data' } else { 'observed' }
    corpusSha256 = Get-EvalSha $corpusAbsolute
    instructions = $instructions
    cases = @($corpus.cases)
    runs = @($rows)
    pairs = @($pairs)
    comparablePairCount = @($pairs | Where-Object comparable).Count
    issues = @($issues)
    authority = 'Submitted agent transcripts with independently supplied semantic judgments. Valid means evidence structure and hashes match, not that a model ran here or that the reviewer is correct. No automatic instruction promotion or statistical effectiveness claim.'
    nextAction = 'Run each identical scenario in fresh isolated sessions with baseline (candidate instructions withheld) and guided (listed instruction hashes) conditions, the same model/settings and a positive trial number. Save actual transcripts. Have a separate reviewer judge every rubric item with verdict, reason and a literal evidenceQuote. Submit the paired records through -EvaluationPath. Repeat trials to assess variance; deterministic smoke fixtures are excluded from behavioral results.'
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 25 } else {
    Write-Host "Behavioral instruction evaluation: $($result.status), valid=$($result.valid), comparable pairs=$($result.comparablePairCount)"
    foreach ($case in $result.cases) { Write-Host " - $($case.id): $($case.prompt)" }
    foreach ($issue in $issues) { Write-Host " - $issue" }
    Write-Host $result.nextAction
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
