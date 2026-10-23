. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')

function ConvertTo-LlmWikiReviewRequirements([object[]]$Criteria) {
    foreach ($criterion in @($Criteria | Sort-Object -Property id -CaseSensitive)) {
        $mapping = [ordered]@{}
        foreach ($name in @('changedPaths', 'checkIds', 'reviewIds', 'scenarioIds', 'testPaths')) {
            $property = if ($null -ne $criterion.mapping) { $criterion.mapping.PSObject.Properties[$name] } else { $null }
            $values = if ($null -ne $property) { @($property.Value) } else { @() }
            $mapping[$name] = @($values | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object {
                if ($name -in @('changedPaths', 'testPaths')) { ([string]$_).Replace('\', '/') } else { [string]$_ }
            } | Sort-Object -Unique -CaseSensitive)
        }
        if ($null -ne $criterion.mapping) {
            foreach ($property in @($criterion.mapping.PSObject.Properties | Sort-Object -Property Name -CaseSensitive)) {
                if (-not $mapping.Contains($property.Name)) { $mapping[$property.Name] = $property.Value }
            }
        }
        [pscustomobject][ordered]@{ id = [string]$criterion.id; text = [string]$criterion.text; mapping = [pscustomobject]$mapping }
    }
}

function Get-LlmWikiReviewProductPaths([object]$Evidence) {
    @($Evidence.change.changedPaths | Where-Object {
        $_ -notmatch '^\.llm-wiki/(generated|reviews)/|^\.artifacts/' -and $_ -ne '.llm-wiki/knowledge/verification-telemetry.json'
    } | ForEach-Object { ([string]$_).Replace('\', '/') } | Sort-Object -Unique)
}

function Get-LlmWikiReviewBinding([object]$Evidence, [string]$EvidencePath, [string[]]$InspectedPath) {
    $repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
    $paths = @(Get-LlmWikiReviewProductPaths $Evidence)
    $acceptancePath = Join-Path (Split-Path -Parent $EvidencePath) 'acceptance-matrix.json'
    $absoluteAcceptance = if ([IO.Path]::IsPathRooted($acceptancePath)) { $acceptancePath } else { Join-Path $repositoryRoot $acceptancePath }
    $acceptance = if (Test-Path -LiteralPath $absoluteAcceptance -PathType Leaf) { Get-Content -LiteralPath $absoluteAcceptance -Raw | ConvertFrom-LlmWikiJson } else { $null }
    # Bind requirements and their mappings, not mutable satisfaction bookkeeping.
    $requirements = @(if ($null -ne $acceptance) { ConvertTo-LlmWikiReviewRequirements @($acceptance.criteria) })
    $content = & (Join-Path $PSScriptRoot 'Get-LlmWikiContentFingerprint.ps1') -Path @($paths + $InspectedPath | Sort-Object -Unique) -Format Json | ConvertFrom-LlmWikiJson
    [pscustomobject][ordered]@{
        head = ([string](& git -C $repositoryRoot rev-parse HEAD)).Trim()
        base = [string]$Evidence.git.base
        changedPaths = $paths
        contentFingerprint = [string]$content.fingerprint
        requirements = $requirements
    }
}

function Get-LlmWikiReviewContentIssues([object]$Content, [object]$Binding) {
    $issues = [Collections.Generic.List[string]]::new()
    foreach ($name in @('inspectedPaths', 'requirements', 'codeQuality', 'findings')) {
        if (-not $Content.PSObject.Properties[$name]) { $issues.Add("Substantive review requires $name.") }
    }
    if ($issues.Count -gt 0) { return @($issues) }
    if ([string]::IsNullOrWhiteSpace([string]$Content.codeQuality)) { $issues.Add('Explain implementation correctness, failure behavior and test gaps in codeQuality.') }
    $paths = @($Content.inspectedPaths)
    foreach ($path in $Binding.changedPaths) {
        if ($path -notin $paths) { $issues.Add("Review has not inspected changed source '$path'.") }
    }
    $requirements = @($Content.requirements)
    if ($requirements.Count -eq 0) { $issues.Add('Review at least one observable requirement.') }
    $ids = @($requirements | ForEach-Object { [string]$_.criterionId })
    if (@($ids | Sort-Object -Unique).Count -ne $ids.Count) { $issues.Add('Requirement review IDs must be distinct.') }
    foreach ($id in @($Binding.requirements | ForEach-Object id)) {
        if ($id -notin $ids) { $issues.Add("Review acceptance criterion '$id'.") }
    }
    foreach ($item in $requirements) {
        if (-not $item.PSObject.Properties['criterionId'] -or [string]::IsNullOrWhiteSpace([string]$item.criterionId) -or
            -not $item.PSObject.Properties['conclusion'] -or [string]$item.conclusion -notin @('satisfied', 'gap', 'not-applicable') -or
            -not $item.PSObject.Properties['evidence'] -or [string]::IsNullOrWhiteSpace([string]$item.evidence)) { $issues.Add('Every requirement needs criterionId, conclusion (satisfied/gap/not-applicable), and concrete source or test evidence.') }
        if (@($Binding.requirements).Count -gt 0 -and [string]$item.criterionId -notin @($Binding.requirements | ForEach-Object id)) { $issues.Add("Unknown reviewed criterion '$($item.criterionId)'.") }
    }
    foreach ($finding in @($Content.findings)) {
        foreach ($name in @('id', 'severity', 'kind', 'location', 'trigger', 'consequence', 'testGap', 'remediation', 'status')) {
            if (-not $finding.PSObject.Properties[$name] -or [string]::IsNullOrWhiteSpace([string]$finding.$name)) { $issues.Add("Review finding requires $name.") }
        }
        if (-not $finding.PSObject.Properties['severity'] -or $finding.severity -notin @('critical', 'major', 'warning', 'info')) { $issues.Add('Unknown finding severity.') }
        if (-not $finding.PSObject.Properties['kind'] -or $finding.kind -notin @('defect', 'suggestion', 'question')) { $issues.Add('Unknown finding kind.') }
        if (-not $finding.PSObject.Properties['status'] -or $finding.status -notin @('open', 'resolved')) { $issues.Add('Finding status must be open or resolved.') }
        if ($finding.PSObject.Properties['location']) {
            $location = $finding.location
            if ($null -eq $location -or -not $location.PSObject.Properties['path'] -or [string]$location.path -notin $paths -or
                -not $location.PSObject.Properties['startLine'] -or [int]$location.startLine -lt 1) { $issues.Add('Finding location must name an inspected path and a positive source line.') }
        }
        if ($finding.PSObject.Properties['status'] -and $finding.status -eq 'resolved' -and
            (-not $finding.PSObject.Properties['resolution'] -or [string]::IsNullOrWhiteSpace([string]$finding.resolution))) { $issues.Add('Resolved findings require a concrete resolution.') }
    }
    $findingIds = @($Content.findings | ForEach-Object { if ($_.PSObject.Properties['id']) { [string]$_.id } })
    if (@($findingIds | Sort-Object -Unique).Count -ne $findingIds.Count) { $issues.Add('Review finding IDs must be distinct.') }
    @($issues)
}

function New-LlmWikiSubstantiveReview([object]$Content, [object]$Evidence, [string]$EvidencePath, [string]$Reason) {
    $contentCopy = $Content | ConvertTo-Json -Depth 20 | ConvertFrom-LlmWikiJson
    if (-not $contentCopy.PSObject.Properties['inspectedPaths']) { throw 'Substantive review requires inspectedPaths.' }
    $paths = @($contentCopy.inspectedPaths | ForEach-Object {
        $path = ([string]$_).Replace('\', '/')
        if ([string]::IsNullOrWhiteSpace($path) -or [IO.Path]::IsPathRooted($path) -or $path -match '(^|/)\.\.(/|$)') { throw 'Inspected paths must be repository-relative source files.' }
        $path
    } | Sort-Object -Unique)
    $contentCopy.inspectedPaths = $paths
    $binding = Get-LlmWikiReviewBinding $Evidence $EvidencePath $paths
    $repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
    foreach ($path in $paths) {
        if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $path) -PathType Leaf) -and $path -notin $binding.changedPaths) { throw "Inspected source does not exist: $path" }
    }
    $issues = @(Get-LlmWikiReviewContentIssues $contentCopy $binding)
    if ($issues.Count -gt 0) { throw ($issues -join ' ') }
    $payload = [pscustomobject][ordered]@{ kind = 'substantive-review-attestation'; reviewScope = $Reason; binding = $binding; content = $contentCopy }
    [pscustomobject][ordered]@{ payload = $payload; fingerprint = Get-LlmWikiJsonFingerprint $payload }
}

function Test-LlmWikiSubstantiveReview([object]$Evidence, [string]$EvidencePath, [switch]$Required) {
    $issues = [Collections.Generic.List[string]]::new()
    $findings = @()
    $requirements = @()
    $codeQuality = ''
    $record = if ($Evidence.PSObject.Properties['substantiveReview']) { $Evidence.substantiveReview } else { $null }
    if ($null -eq $record) {
        if ($Required) { $issues.Add('Record substantive-code-review through evidence-review with requirement conclusions and code findings.') }
    } else {
        try {
            $payload = $record.payload
            $findings = @($payload.content.findings)
            $requirements = @($payload.content.requirements)
            $codeQuality = [string]$payload.content.codeQuality
            if ([string]$payload.kind -cne 'substantive-review-attestation' -or [string]$record.fingerprint -cne (Get-LlmWikiJsonFingerprint $payload)) { $issues.Add('Substantive review integrity is invalid.') }
            $current = Get-LlmWikiReviewBinding $Evidence $EvidencePath @($payload.content.inspectedPaths)
            # Keep the original integrity check above. Normalize a copy so legacy
            # attestations survive equivalent scalar/null/collection formatting.
            $recordedBinding = $payload.binding | ConvertTo-Json -Depth 30 | ConvertFrom-LlmWikiJson
            $recordedBinding.requirements = @(ConvertTo-LlmWikiReviewRequirements @($recordedBinding.requirements))
            if ((Get-LlmWikiJsonFingerprint $current) -cne (Get-LlmWikiJsonFingerprint $recordedBinding)) { $issues.Add('Substantive review is stale: sources, scope, HEAD or acceptance requirements changed.') }
            foreach ($issue in @(Get-LlmWikiReviewContentIssues $payload.content $current)) { $issues.Add($issue) }
            foreach ($item in @($payload.content.requirements | Where-Object conclusion -eq 'gap')) { $issues.Add("Requirement '$($item.criterionId)' has an unresolved behavior gap.") }
            foreach ($finding in @($payload.content.findings | Where-Object { $_.severity -in @('critical', 'major') -and $_.status -ne 'resolved' })) { $issues.Add("Blocking code finding '$($finding.id)' remains open.") }
        } catch { $issues.Add("Substantive review cannot be validated: $($_.Exception.Message)") }
    }
    [pscustomobject][ordered]@{ present = $null -ne $record; required = [bool]$Required; valid = $issues.Count -eq 0; issues = @($issues); requirements = $requirements; codeQuality = $codeQuality; findings = $findings; semantics = 'Recorded reviewer judgment; automatic validation checks integrity and closure, not semantic truth.' }
}
