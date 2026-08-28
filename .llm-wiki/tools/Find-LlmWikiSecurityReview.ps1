[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$qualityPath = Join-Path $wikiRoot 'generated/quality-index.json'
$sensitiveDataPath = Join-Path $wikiRoot 'generated/sensitive-data-index.json'
$runtimeTopologyPath = Join-Path $wikiRoot 'generated/runtime-topology.json'

foreach ($requiredPath in @($qualityPath, $sensitiveDataPath, $runtimeTopologyPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required compiled index is missing: $requiredPath. Run ./.llm-wiki/wiki.ps1 update and retry."
    }
}

$normalizedQuery = ([string]$Query).ToLowerInvariant()
$assessmentDimensionCount = @(
    [regex]::Matches($normalizedQuery, '\b(correctness|reliability|concurrency|architecture|privacy|security|ci|operations|operational|project|repository|cross-layer|system-wide|vulnerability|vulnerabilities)\b|корректност|над[её]жност|конкурент|архитектур|приватност|конфиденциальност|безопасност|уязвимост|операц|проект|репозитор') |
        ForEach-Object Value | Sort-Object -Unique
).Count
$repositoryAssessment = $normalizedQuery -match '\b(audit|assessment|evaluate|review)\b|аудит|оцен|провер' -and $assessmentDimensionCount -ge 2
$reviewQueries = if ([string]::IsNullOrWhiteSpace($Query) -or $repositoryAssessment) {
    @(
        'WebPush connect-time DNS address control SocketsHttpHandlerFactory'
        'Mailgun ProviderWebhookAuthorizer webhook replay idempotency signature timestamp'
        'browser refresh token localStorage CSP XSS'
        'nginx TLS SSL proxy transport security'
    )
} else {
    @($Query)
}

$contextLeadsByPath = [ordered]@{}
$queryAssessments = [System.Collections.Generic.List[object]]::new()
foreach ($reviewQuery in $reviewQueries) {
    $context = & (Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1') `
        -Query $reviewQuery `
        -CompiledIndexSource $CompiledIndexSource `
        -Limit ([Math]::Min(12, $Limit)) `
        -Format Json | ConvertFrom-Json
    $queryAssessments.Add([pscustomobject][ordered]@{
        query = $reviewQuery
        confidence = [string]$context.confidence
        conclusive = [bool]$context.conclusive
        ambiguityReason = $context.ambiguityReason
        candidateCount = @($context.candidates).Count
    })
    foreach ($candidate in @($context.candidates)) {
        $path = [string]$candidate.path
        if ([string]::IsNullOrWhiteSpace($path)) { continue }
        $key = $path.ToLowerInvariant()
        $lead = [pscustomobject][ordered]@{
            query = $reviewQuery
            path = $path
            rank = [int]$candidate.rank
            score = [double]$candidate.score
            confidence = [string]$candidate.confidence
            reasons = @($candidate.reasons)
        }
        if (-not $contextLeadsByPath.Contains($key) -or [int]$contextLeadsByPath[$key].rank -gt [int]$lead.rank) {
            $contextLeadsByPath[$key] = $lead
        }
    }
}
$contextLeads = @($contextLeadsByPath.Values | Sort-Object rank, @{ Expression = 'score'; Descending = $true }, path | Select-Object -First $Limit)

$quality = Get-Content -LiteralPath $qualityPath -Raw | ConvertFrom-Json
$securitySymbolPattern = '(?i)(authenticat|authoriz|access.?token|refresh.?token|tokenhash|secret|apikey|signingkey|password|webhook|securityheader|idempot|deduplic|replay|signature|hmac|csp|cors|ssrf|webpush|upload|dmarc|smtp|ratelimit|rate.?limit|permission|access.?guard)'
$securityTestSignals = @($quality.criticalSymbols | Where-Object {
    $_.path -notmatch '^\.llm-wiki/' -and "$($_.name) $($_.path) $($_.role)" -match $securitySymbolPattern
} | ForEach-Object {
    $classificationText = "$($_.name) $($_.path)".ToLowerInvariant()
    $controlFamily = if ($classificationText -match 'webhook|mailgun|idempot|deduplic|replay|signature|hmac') { 'webhook-authenticity-replay' }
        elseif ($classificationText -match 'webpush|ssrf') { 'outbound-endpoint-validation' }
        elseif ($classificationText -match 'csp|cors|xss|securityheader') { 'browser-boundary' }
        elseif ($classificationText -match 'upload|image') { 'untrusted-content' }
        elseif ($classificationText -match 'smtp|dmarc') { 'mail-ingress' }
        elseif ($classificationText -match 'ratelimit|rate.?limit') { 'resource-abuse-control' }
        elseif ($classificationText -match 'authoriz|permission|access.?guard') { 'authorization' }
        elseif ($classificationText -match 'authenticat|token|secret|apikey|signingkey|password') { 'authentication-token' }
        else { 'security-control' }
    [pscustomobject][ordered]@{
        controlFamily = $controlFamily
        name = $_.name
        role = $_.role
        path = $_.path
        line = $_.line
        testReferenceCount = [int]$_.testReferenceCount
        testReferences = @($_.testReferences)
        coverageClassification = $(if ([int]$_.testReferenceCount -gt 0) { 'direct-test-reference-present' } else { 'direct-test-reference-absent' })
        confidence = 'medium'
        caveat = 'Static symbol-name references are navigation evidence, not proof that the security behavior is executed or asserted.'
    }
} | Sort-Object testReferenceCount, controlFamily, path | Select-Object -First $Limit)

$runtime = Get-Content -LiteralPath $runtimeTopologyPath -Raw | ConvertFrom-Json
$runtimeEvidence = [pscustomobject][ordered]@{
    webhooks = @($runtime.webhooks | Select-Object -First $Limit)
    networkPolicies = @($runtime.networkPolicies | Select-Object -First $Limit)
    composeServices = @($runtime.composeServices | Where-Object {
        @($_.ports).Count -gt 0 -or @($_.environmentKeys).Count -gt 0 -or @($_.networks).Count -gt 0
    } | Select-Object -First $Limit)
}

$sensitive = Get-Content -LiteralPath $sensitiveDataPath -Raw | ConvertFrom-Json
$privacyEvidence = [pscustomobject][ordered]@{
    summary = $sensitive.summary
    externalTransfers = @($sensitive.externalTransfers | Select-Object -First $Limit)
    potentialLogging = @($sensitive.potentialLogging | Select-Object -First $Limit)
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    query = $Query
    queryMode = $(if ($repositoryAssessment) { 'repository-assessment-expanded' } elseif ([string]::IsNullOrWhiteSpace($Query)) { 'curated-default' } else { 'focused-query' })
    queryAssessments = @($queryAssessments)
    contextLeads = $contextLeads
    securityTestSignals = $securityTestSignals
    runtimeEvidence = $runtimeEvidence
    privacyEvidence = $privacyEvidence
    limitations = @(
        'This command compiles security-review navigation evidence; it is not a vulnerability scanner and does not prove absence of vulnerabilities.'
        'Repository declarations do not prove effective production exposure, IAM, grants, DNS resolution behavior, network enforcement, or webhook idempotency.'
        'Static test references do not prove that the security property is executed or asserted; inspect and run the referenced tests.'
        'Validate every lead in current code, tests, deployed configuration, and provider controls before making a security conclusion.'
    )
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 12
    exit 0
}

Write-Host "Security review evidence: $($contextLeads.Count) context lead(s), $($securityTestSignals.Count) test signal(s)."
foreach ($assessment in $queryAssessments) {
    Write-Host " - query '$($assessment.query)': confidence=$($assessment.confidence), conclusive=$($assessment.conclusive), candidates=$($assessment.candidateCount)"
}
Write-Host ''
Write-Host 'Top context leads:'
foreach ($lead in $contextLeads) {
    Write-Host " - #$($lead.rank) [$($lead.confidence)] $($lead.path)"
}
Write-Host ''
Write-Host 'Security-oriented test signals:'
foreach ($signal in $securityTestSignals) {
    Write-Host " - [$($signal.controlFamily)] $($signal.name): $($signal.coverageClassification) ($($signal.path):$($signal.line))"
}
Write-Host ''
Write-Host "Runtime evidence: webhooks=$(@($runtimeEvidence.webhooks).Count), network policies=$(@($runtimeEvidence.networkPolicies).Count), exposed/configured compose services=$(@($runtimeEvidence.composeServices).Count)."
Write-Host "Privacy inventory: credential=$($privacyEvidence.summary.credential), identity=$($privacyEvidence.summary.identity), health=$($privacyEvidence.summary.health), financial=$($privacyEvidence.summary.financial), privateContent=$($privacyEvidence.summary.privateContent)."
Write-Host 'Evidence boundary: this is investigation guidance, not a security verdict; validate findings against current source, tests, deployed configuration, and provider/IAM state.'
