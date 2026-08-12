[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('assess', 'create', 'expand', 'show', 'verify')]
    [string]$Action = 'assess',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$Reason,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$normalizedWorkspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $normalizedWorkspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$workspaceAbsolute = Join-Path $repositoryRoot $normalizedWorkspace
$acceptancePath = Join-Path $workspaceAbsolute 'acceptance-matrix.json'
$packetPath = Join-Path $workspaceAbsolute 'change-packet.json'
$receiptPath = Join-Path $workspaceAbsolute 'requirement-model.json'
$proofPath = Join-Path $workspaceAbsolute 'proof-of-change.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
foreach ($requiredPath in @($acceptancePath, $packetPath, $policyPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { throw "Required requirement-model input is absent: $requiredPath" }
}
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$modelPolicy = $policy.requirementModel

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    if ($null -eq $json) { $json = 'null' }
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}
function Get-FileHashValue([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-Tokens([string]$Text) {
    @([regex]::Matches($Text.ToLowerInvariant(), '[\p{L}\p{Nd}]+') | ForEach-Object Value | Where-Object { $_.Length -gt 2 } | Sort-Object -Unique)
}
function Get-Similarity([string]$Left, [string]$Right) {
    $a = @(Get-Tokens $Left)
    $b = @(Get-Tokens $Right)
    $union = @($a + $b | Sort-Object -Unique)
    if ($union.Count -eq 0) { return 0 }
    $intersection = @($a | Where-Object { $_ -in $b })
    [Math]::Round(($intersection.Count * 100.0) / $union.Count, 2)
}
function Test-Coverage([object[]]$Criteria, [string]$Pattern) {
    @($Criteria | Where-Object { [string]$_.text -match $Pattern }).Count -gt 0
}
function Get-RequirementType([string]$Text) {
    if ($Text -match '(?i)security|authori[sz]|permission|tenant|secret') { return 'security' }
    if ($Text -match '(?i)compatib|consumer|contract|backward') { return 'compatibility' }
    if ($Text -match '(?i)invalid|error|fail|reject|exception') { return 'failure' }
    if ($Text -match '(?i)performance|latency|throughput') { return 'performance' }
    if ($Text -match '(?i)locali[sz]|translation|russian|english') { return 'localization' }
    if ($Text -match '(?i)migrat|database|persist|data') { return 'data' }
    if ($Text -match '(?i)observ|metric|log|trace') { return 'observability' }
    if ($Text -match '(?i)abstraction|projection|module boundary|ownership|dependency direction') { return 'structure' }
    'behavior'
}
function Split-CompoundCriterion([string]$Text) {
    $normalized = $Text.Trim()
    $suffix = ''
    if ($normalized -match '^(?<main>.+?)(?<suffix>\s+without\s+.+?)(?<period>\.)?$') {
        $normalized = $Matches.main.Trim()
        $suffix = $Matches.suffix.Trim()
    }
    $lead = $null
    $tail = $suffix
    $itemsText = $null
    if ($normalized -match '^(?<lead>.+?\b(?:preserves|prevents|rejects|supports|ensures|validates|keeps|maintains|allows|requires)\s+)(?<items>.+)$') {
        $lead = $Matches.lead.TrimEnd()
        $itemsText = $Matches.items.Trim().TrimEnd('.')
    } elseif ($normalized -match '^(?<items>.+?)(?<tail>\s+remain(?:s)?\s+.+)$') {
        $itemsText = $Matches.items.Trim()
        $tail = $Matches.tail.Trim().TrimEnd('.')
    } else {
        return @($Text)
    }
    if ($itemsText -notmatch '[,;]') { return @($Text) }
    $items = @($itemsText -split '\s*[,;]\s*|\s+(?:and|or)\s+' | ForEach-Object { $_.Trim() -replace '^(?i:and|or)\s+', '' } | Where-Object { $_ })
    if ($items.Count -le 1) { return @($Text) }
    return @($items | ForEach-Object {
        if ($lead) { "$lead $_$(if ($tail) { " $tail" })." }
        else { "$_ $tail." }
    })
}
function Get-CriterionOriginKind([object]$Criterion) {
    if ($null -ne $Criterion -and $Criterion.PSObject.Properties['origin'] -and $null -ne $Criterion.origin -and $Criterion.origin.PSObject.Properties['kind']) {
        return [string]$Criterion.origin.kind
    }
    return ''
}
function Get-Recommendations([object]$Packet, [object[]]$Criteria) {
    $recommendations = [Collections.Generic.List[object]]::new()
    $appliedRecommendationIds = @($Criteria | ForEach-Object {
        if ($null -ne $_ -and $_.PSObject.Properties['origin'] -and $null -ne $_.origin -and $_.origin.PSObject.Properties['recommendationId']) {
            [string]$_.origin.recommendationId
        }
    } | Where-Object { $_ } | Sort-Object -Unique)
    function Add-Recommendation([string]$Id, [string]$Type, [string]$Text, [string]$Rationale, [string]$Pattern) {
        if ($Id -notin $appliedRecommendationIds -and -not (Test-Coverage $Criteria $Pattern) -and $recommendations.Count -lt [int]$modelPolicy.maximumRecommendations) {
            $recommendations.Add([pscustomobject][ordered]@{ id = $Id; type = $Type; text = $Text; rationale = $Rationale })
        }
    }
    Add-Recommendation 'REC-BEHAVIOR' 'behavior' 'The intended user-visible behavior succeeds for the primary scenario.' 'Every change needs a positive behavioral outcome.' '(?i)succeed|success|return|create|update|display'
    Add-Recommendation 'REC-FAILURE' 'failure' 'Invalid or unsupported input is rejected with the expected observable outcome.' 'Boundary behavior should be explicit and testable.' '(?i)invalid|error|fail|reject'
    $reviewIds = @($Packet.brief.reviewObligations | ForEach-Object {
        if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id }
    })
    if ($reviewIds -contains 'security-review' -or [string]$Packet.brief.risk.level -in @('high', 'critical')) {
        Add-Recommendation 'REC-SECURITY-AUTHORIZATION' 'security' 'Authorization remains correct for the changed flow.' 'Elevated-risk changes require an explicit authorization outcome.' '(?i)authori[sz]|permission'
        Add-Recommendation 'REC-SECURITY-SCOPING' 'security' 'Identity data remains scoped to the intended user.' 'Elevated-risk changes require an explicit data-scoping outcome.' '(?i)data scop|intended user|tenant'
        Add-Recommendation 'REC-SECURITY-SECRETS' 'security' 'Secrets remain protected throughout the changed flow.' 'Elevated-risk changes require an explicit secret-handling outcome.' '(?i)secret|credential|token protection'
        Add-Recommendation 'REC-SECURITY-LOGGING' 'security' 'Sensitive values remain absent from application logs.' 'Elevated-risk changes require an explicit logging outcome.' '(?i)sensitive log|absent from .*logs|redact'
    }
    if (@($Packet.diff.scopes) -contains 'Api') {
        Add-Recommendation 'REC-COMPATIBILITY' 'compatibility' 'Existing API consumers remain compatible, or the intentional contract change is versioned and documented.' 'API scope requires an explicit consumer outcome.' '(?i)compatib|consumer|contract'
    }
    if (@($Packet.diff.scopes) -contains 'Database') {
        Add-Recommendation 'REC-DATA' 'data' 'Existing data remains readable and the schema change can be deployed and rolled back safely.' 'Database scope requires migration and compatibility evidence.' '(?i)migrat|database|persist|data'
    }
    if (@($Packet.diff.scopes) -contains 'Frontend') {
        Add-Recommendation 'REC-UI' 'behavior' 'The changed UI is usable in supported viewport and interaction states.' 'Frontend scope needs observable interaction coverage.' '(?i)ui|screen|viewport|interaction'
    }
    if (@($Packet.diff.changedPaths | Where-Object { $_ -match 'assets/i18n|localiz|Resources' }).Count -gt 0) {
        Add-Recommendation 'REC-LOCALIZATION' 'localization' 'User-facing text is correct in English and Russian without encoding corruption.' 'Localized resources are in the change set.' '(?i)locali[sz]|translation|russian|english'
    }
    @($recommendations)
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = $Receipt.schemaVersion
        workspace = $Receipt.workspace
        assessedAtUtc = $Receipt.assessedAtUtc
        acceptanceHash = $Receipt.acceptanceHash
        packetHash = $Receipt.packetHash
        packetFingerprint = $Receipt.packetFingerprint
        policyFingerprint = $Receipt.policyFingerprint
        classification = $Receipt.classification
        recommendations = @($Receipt.recommendations)
        findings = @($Receipt.findings)
        valid = $Receipt.valid
    }
}
function Get-Assessment {
    $acceptance = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    $criteria = @($acceptance.criteria)
    $analyses = [Collections.Generic.List[object]]::new()
    $findings = [Collections.Generic.List[object]]::new()
    foreach ($criterion in $criteria) {
        $text = [string]$criterion.text
        $wordCount = @(Get-Tokens $text).Count
        $connectors = @([regex]::Matches($text, '(?i)\b(and|or|but|while|unless)\b|[,;]')).Count
        $vague = $text -match '(?i)^\s*(improve|optimi[sz]e|enhance|make .* better|fix)[.!]?\s*$'
        $criterionFindings = [Collections.Generic.List[string]]::new()
        if ($wordCount -lt [int]$modelPolicy.minimumCriterionWords) { $criterionFindings.Add('criterion-too-short') }
        if ([bool]$modelPolicy.blockVagueCriteria -and $vague) { $criterionFindings.Add('criterion-vague') }
        if ([bool]$modelPolicy.blockCompoundCriteria -and $connectors -gt [int]$modelPolicy.maximumCompoundConnectors) { $criterionFindings.Add('criterion-compound') }
        foreach ($findingId in $criterionFindings) {
            $findings.Add([pscustomobject][ordered]@{ id = $findingId; severity = 'block'; criterionId = [string]$criterion.id })
        }
        $analyses.Add([pscustomobject][ordered]@{
            id = [string]$criterion.id
            text = $text
            type = Get-RequirementType $text
            wordCount = $wordCount
            compoundConnectorCount = $connectors
            vague = $vague
            atomic = $connectors -le [int]$modelPolicy.maximumCompoundConnectors
            findings = @($criterionFindings)
        })
    }
    for ($left = 0; $left -lt $criteria.Count; $left++) {
        for ($right = $left + 1; $right -lt $criteria.Count; $right++) {
            if ((Get-CriterionOriginKind $criteria[$left]) -eq 'compound-split' -or (Get-CriterionOriginKind $criteria[$right]) -eq 'compound-split') { continue }
            $leftType = Get-RequirementType ([string]$criteria[$left].text)
            $rightType = Get-RequirementType ([string]$criteria[$right].text)
            if ($leftType -ne $rightType) { continue }
            $similarity = Get-Similarity ([string]$criteria[$left].text) ([string]$criteria[$right].text)
            if ($similarity -ge [double]$modelPolicy.duplicateSimilarityPercent) {
                $findings.Add([pscustomobject][ordered]@{ id = 'criteria-near-duplicate'; severity = 'block'; criterionId = [string]$criteria[$left].id; relatedCriterionId = [string]$criteria[$right].id; similarityPercent = $similarity })
            }
        }
    }
    $recommendations = @(Get-Recommendations $packet $criteria)
    [pscustomobject]@{
        acceptance = $acceptance
        packet = $packet
        classification = [pscustomobject][ordered]@{
            criteria = @($analyses)
            criteriaCount = $criteria.Count
            typeCoverage = @($analyses.type | Sort-Object -Unique)
            blockingFindingCount = @($findings | Where-Object severity -eq 'block').Count
            recommendationCount = $recommendations.Count
        }
        recommendations = $recommendations
        findings = @($findings)
        valid = @($findings | Where-Object severity -eq 'block').Count -eq 0
    }
}
function New-Receipt([object]$Assessment) {
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $normalizedWorkspace
        assessedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        acceptanceHash = Get-FileHashValue $acceptancePath
        packetHash = Get-FileHashValue $packetPath
        packetFingerprint = [string]$Assessment.packet.fingerprint
        policyFingerprint = Get-FileHashValue $policyPath
        classification = $Assessment.classification
        recommendations = @($Assessment.recommendations)
        findings = @($Assessment.findings)
        valid = [bool]$Assessment.valid
        modelHash = ''
    }
    $receipt.modelHash = Get-Hash (Get-Payload $receipt)
    $receipt
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    $current = New-Receipt (Get-Assessment)
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $normalizedWorkspace) { $issues.Add('Workspace does not match.') }
    foreach ($name in @('acceptanceHash', 'packetHash', 'packetFingerprint', 'policyFingerprint')) {
        if ([string]$Receipt.$name -cne [string]$current.$name) { $issues.Add("$name drifted.") }
    }
    if ((Get-Hash $Receipt.classification) -cne (Get-Hash $current.classification)) { $issues.Add('Requirement classification drifted.') }
    if ((Get-Hash @($Receipt.recommendations)) -cne (Get-Hash @($current.recommendations))) { $issues.Add('Requirement recommendations drifted.') }
    if ((Get-Hash @($Receipt.findings)) -cne (Get-Hash @($current.findings))) { $issues.Add('Requirement findings drifted.') }
    if ([bool]$Receipt.valid -ne [bool]$current.valid) { $issues.Add('Requirement verdict drifted.') }
    if ([string]$Receipt.modelHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Requirement model hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0 -and [bool]$Receipt.valid; integrityValid = $issues.Count -eq 0; issues = @($issues) }
}

if ($Action -eq 'expand') {
    if ([string]::IsNullOrWhiteSpace($Reason)) { throw 'expand requires Reason.' }
    $assessment = Get-Assessment
    $acceptance = $assessment.acceptance
    $nextNumber = @($acceptance.criteria | ForEach-Object { if ([string]$_.id -match '^AC-(\d+)$') { [int]$Matches[1] } } | Measure-Object -Maximum).Maximum
    if ($null -eq $nextNumber) { $nextNumber = 0 }
    $added = [Collections.Generic.List[object]]::new()
    $expandedCriteria = [Collections.Generic.List[object]]::new()
    foreach ($criterion in @($acceptance.criteria)) {
        $analysis = @($assessment.classification.criteria | Where-Object { $_.id -eq $criterion.id })[0]
        $parts = @(if ($null -ne $analysis -and -not $analysis.atomic) { @(Split-CompoundCriterion ([string]$criterion.text)) } else { @([string]$criterion.text) })
        for ($partIndex = 0; $partIndex -lt $parts.Count; $partIndex++) {
            if ($partIndex -eq 0) {
                $criterion.text = $parts[$partIndex]
                $criterion | Add-Member -NotePropertyName origin -NotePropertyValue ([pscustomobject][ordered]@{ kind = 'compound-split'; sourceCriterionId = [string]$criterion.id }) -Force
                $expandedCriteria.Add($criterion)
                continue
            }
            $nextNumber++
            $splitCriterion = [pscustomobject][ordered]@{
                id = 'AC-{0:d3}' -f ([int]$nextNumber)
                text = $parts[$partIndex]
                status = 'pending'
                origin = [pscustomobject][ordered]@{ kind = 'compound-split'; sourceCriterionId = [string]$criterion.id }
                mapping = $criterion.mapping
                resolution = [pscustomobject][ordered]@{ reason = $null; evidenceNote = $null }
            }
            $expandedCriteria.Add($splitCriterion)
            $added.Add($splitCriterion)
        }
    }
    $acceptance.criteria = @($expandedCriteria)
    foreach ($recommendation in @($assessment.recommendations)) {
        $nextNumber++
        $criterion = [pscustomobject][ordered]@{
            id = 'AC-{0:d3}' -f ([int]$nextNumber)
            text = [string]$recommendation.text
            status = 'pending'
            origin = [pscustomobject][ordered]@{ kind = 'requirement-recommendation'; recommendationId = [string]$recommendation.id; rationale = [string]$recommendation.rationale }
            mapping = [pscustomobject][ordered]@{ changedPaths = @(); scenarioIds = @(); checkIds = @(); reviewIds = @(); testPaths = @() }
            resolution = [pscustomobject][ordered]@{ reason = $null; evidenceNote = $null }
        }
        $acceptance.criteria = @($acceptance.criteria) + $criterion
        $added.Add($criterion)
    }
    $temporaryPath = "$acceptancePath.$([guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($temporaryPath, (($acceptance | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        [IO.File]::Copy($temporaryPath, $acceptancePath, $true)
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [IO.File]::Delete($temporaryPath) }
    }
    if (Test-Path -LiteralPath $proofPath) { [IO.File]::Delete($proofPath) }
    $expandedModel = New-Receipt (Get-Assessment)
    $temporaryModelPath = "$receiptPath.$([guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($temporaryModelPath, (($expandedModel | ConvertTo-Json -Depth 40) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        if (Test-Path -LiteralPath $receiptPath) { [IO.File]::Delete($receiptPath) }
        [IO.File]::Move($temporaryModelPath, $receiptPath)
    } finally {
        if (Test-Path -LiteralPath $temporaryModelPath) { [IO.File]::Delete($temporaryModelPath) }
    }
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') add -WorkspacePath $normalizedWorkspace -JournalType decision -Text "Expanded acceptance with $($added.Count) requirement recommendation(s)." -Rationale $Reason | Out-Null
    $result = [pscustomobject][ordered]@{ action = 'expand'; valid = $expandedModel.valid; addedCount = $added.Count; addedCriteria = @($added); issues = @(); model = $expandedModel; savedPath = "$normalizedWorkspace/requirement-model.json" }
} elseif ($Action -in @('assess', 'create')) {
    $receipt = New-Receipt (Get-Assessment)
    $temporaryPath = "$receiptPath.$([guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($temporaryPath, (($receipt | ConvertTo-Json -Depth 40) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        if (Test-Path -LiteralPath $receiptPath) { [IO.File]::Delete($receiptPath) }
        [IO.File]::Move($temporaryPath, $receiptPath)
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [IO.File]::Delete($temporaryPath) }
    }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $receipt.valid; issues = @(); model = $receipt; savedPath = "$normalizedWorkspace/requirement-model.json" }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Requirement model is absent: $normalizedWorkspace/requirement-model.json" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $validation = Test-Receipt $receipt
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; integrityValid = $validation.integrityValid; issues = @($validation.issues); model = $receipt }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 40 } else {
    Write-Host "Requirement model: action=$($result.action), valid=$($result.valid), criteria=$($result.model.classification.criteriaCount), recommendations=$(@($result.model.recommendations).Count)"
    foreach ($finding in @($result.model.findings)) { Write-Host " - [$($finding.severity)] $($finding.criterionId): $($finding.id)" }
    foreach ($recommendation in @($result.model.recommendations)) { Write-Host " - [suggest] $($recommendation.id): $($recommendation.text)" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
