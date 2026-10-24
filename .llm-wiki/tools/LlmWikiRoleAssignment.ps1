function Get-LlmWikiRoleAssignment {
    param([Parameter(Mandatory)][object]$Bundle, [Parameter(Mandatory)][ValidateSet('researcher', 'implementer', 'reviewer')][string]$Role)
    $contracts = @{
        researcher = @{
            purpose = 'Establish current behavior, owners, consumers and uncertainties before editing.'
            deliverables = @('Source-anchored flow and consumer map.', 'Reproduction or minimal experiment with observed results.', 'Open decisions and a bounded implementation recommendation.')
            preferredKinds = @('wiki', 'symbol')
        }
        implementer = @{
            purpose = 'Implement the declared outcomes inside the current task scope and preserve compatibility.'
            deliverables = @('Small complete change with acceptance-to-file mapping.', 'Actual focused verification logs; for fixes, inspected failing-before and passing-after evidence.', 'Remaining gaps and exact changed paths for review.')
            preferredKinds = @('changed-source', 'test')
        }
        reviewer = @{
            purpose = 'Assess the final diff independently against each requirement and runtime boundary.'
            deliverables = @('Requirement-by-requirement conclusions with source anchors.', 'Concrete correctness, consumer, failure and test-gap findings.', 'A substantive review record; distinguish executed proof from claims.')
            preferredKinds = @('test', 'changed-source')
        }
    }
    $contract = $contracts[$Role]
    # Reorder the already redacted selection. Never drop mandatory context or
    # retrieve additional raw sources outside the bundle security/budget gates.
    $items = @($Bundle.items | Sort-Object @{ Expression = { [bool]$_.required }; Descending = $true }, @{ Expression = { $_.kind -in $contract.preferredKinds }; Descending = $true }, @{ Expression = { [int]$_.score }; Descending = $true }, path)
    $payload = [ordered]@{ role = $Role; bundleHash = $Bundle.bundleHash; packetFingerprint = $Bundle.packetFingerprint; generatorSha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash; purpose = $contract.purpose; deliverables = $contract.deliverables; paths = @($items.path) }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Depth 12 -Compress))))).Replace('-', '').ToLowerInvariant() } finally { $sha.Dispose() }
    [pscustomobject][ordered]@{
        role = $Role
        objective = $Bundle.query.objective
        purpose = $contract.purpose
        deliverables = $contract.deliverables
        taskArtifacts = @('workspace.json', 'change-packet.json', 'acceptance-matrix.json', 'change-manifest.json', 'evidence.json') | ForEach-Object { "$($Bundle.workspace)/$_" }
        bundleHash = $Bundle.bundleHash
        packetFingerprint = $Bundle.packetFingerprint
        assignmentHash = $hash
        budgets = $Bundle.budgets
        items = $items
        memories = $Bundle.memories
        omitted = $Bundle.omitted
        handoffRule = 'Use current task artifacts for acceptance and scope. Missing context is a retrieval request; stale context requires regeneration. Stop at unresolved decisions or unverified proof.'
        execution = 'Prepared assignment only; no agent is launched and no lease or additional task state is created.'
    }
}
