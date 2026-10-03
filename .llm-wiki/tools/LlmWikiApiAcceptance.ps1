function Get-LlmWikiApiContractFingerprint {
    param([AllowEmptyString()][string]$Content)
    $normalized = $Content.Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd()
    $bytes = [Text.Encoding]::UTF8.GetBytes($normalized)
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-LlmWikiApiChangeIdentity {
    param([object]$Change)
    ConvertTo-Json -InputObject @(
        [string]$Change.kind, [string]$Change.location, [string]$Change.description, [string]$Change.dimension
    ) -Compress
}

function Get-LlmWikiApiAcceptance {
    param(
        [object]$Manifest,
        [string]$BaseCommit,
        [object[]]$Snapshots,
        [object[]]$BreakingChanges
    )

    $accepted = @()
    $status = 'not-applicable'
    if ($null -ne $Manifest) {
        if ($Manifest.schemaVersion -ne 1 -or
            [string]::IsNullOrWhiteSpace([string]$Manifest.id) -or
            [string]::IsNullOrWhiteSpace([string]$Manifest.decision) -or
            [string]::IsNullOrWhiteSpace([string]$Manifest.clientRollout) -or
            [string]$Manifest.baseCommit -cnotmatch '^[0-9a-f]{40}$') {
            throw 'API acceptance manifest is missing its version, identity, decision, rollout or exact base commit.'
        }
        $approved = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($change in @($Manifest.acceptedChanges)) {
            if ([string]::IsNullOrWhiteSpace([string]$change.kind) -or
                [string]::IsNullOrWhiteSpace([string]$change.location) -or
                [string]::IsNullOrWhiteSpace([string]$change.description) -or
                [string]$change.dimension -cne 'structural' -or
                -not $approved.Add((Get-LlmWikiApiChangeIdentity $change))) {
                throw 'API acceptance contains an incomplete or duplicate change identity.'
            }
        }
        if ($approved.Count -eq 0 -or @($Manifest.snapshots).Count -ne 2) {
            throw 'API acceptance must name its breaking changes and both contract snapshots.'
        }
        $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($snapshot in @($Manifest.snapshots)) {
            if ([string]::IsNullOrWhiteSpace([string]$snapshot.path) -or
                [string]$snapshot.beforeSha256 -cnotmatch '^[0-9a-f]{64}$' -or
                [string]$snapshot.afterSha256 -cnotmatch '^[0-9a-f]{64}$' -or
                -not $paths.Add([string]$snapshot.path)) {
                throw 'API acceptance contains an invalid or duplicate snapshot fingerprint.'
            }
        }

        if ($BaseCommit -ceq [string]$Manifest.baseCommit) {
            $status = 'fingerprint-mismatch'
            $fingerprintsMatch = @($Snapshots).Count -eq 2
            $actualPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
            foreach ($snapshot in @($Snapshots)) {
                if (-not $actualPaths.Add([string]$snapshot.path)) { $fingerprintsMatch = $false }
                $expected = @($Manifest.snapshots | Where-Object { [string]$_.path -ceq [string]$snapshot.path })
                if ($expected.Count -ne 1 -or
                    (Get-LlmWikiApiContractFingerprint $snapshot.beforeContent) -cne [string]$expected[0].beforeSha256 -or
                    (Get-LlmWikiApiContractFingerprint $snapshot.afterContent) -cne [string]$expected[0].afterSha256) {
                    $fingerprintsMatch = $false
                }
            }
            if (-not $actualPaths.SetEquals($paths)) { $fingerprintsMatch = $false }
            if ($fingerprintsMatch) {
                $actual = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
                $uniqueChanges = $true
                foreach ($change in @($BreakingChanges)) {
                    if (-not $actual.Add((Get-LlmWikiApiChangeIdentity $change))) { $uniqueChanges = $false }
                }
                $status = 'stale-change-list'
                if ($uniqueChanges -and $approved.IsSubsetOf($actual)) {
                    $accepted = @($BreakingChanges | Where-Object {
                        $approved.Contains((Get-LlmWikiApiChangeIdentity $_))
                    })
                    $status = 'applied'
                }
            }
        }
    }
    $acceptedIdentities = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($change in $accepted) { $null = $acceptedIdentities.Add((Get-LlmWikiApiChangeIdentity $change)) }
    [pscustomobject]@{
        status = $status
        manifestId = $(if ($null -ne $Manifest) { [string]$Manifest.id } else { $null })
        acceptedChanges = @($accepted)
        unacceptedChanges = @($BreakingChanges | Where-Object {
            -not $acceptedIdentities.Contains((Get-LlmWikiApiChangeIdentity $_))
        })
    }
}
