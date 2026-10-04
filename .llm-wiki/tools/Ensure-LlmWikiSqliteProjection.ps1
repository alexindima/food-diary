function Ensure-LlmWikiSqliteProjection {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('architecture-health', 'contracts', 'domain', 'frontend-contracts', 'risks', 'runtime', 'sensitive')]
        [string]$Category
    )

    $manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
    $status = & $manager -Action status -SkipRefresh -Format Json | ConvertFrom-Json
    $categoryReady = @($status.querySources | Where-Object {
        [string]$_.category -eq $Category -and -not [string]::IsNullOrWhiteSpace([string]$_.contentHash)
    }).Count -gt 0
    if ([bool]$status.changeSetFresh -and $categoryReady) { return }

    try {
        & $manager -Action build -BackendOnlyRefresh -Format Json | Out-Null
    } catch {
        throw "SQLite Wiki projection '$Category' could not be prepared. $($_.Exception.Message) Run ./.llm-wiki/wiki.ps1 graph-build and retry."
    }

    $refreshedStatus = & $manager -Action status -SkipRefresh -Format Json | ConvertFrom-Json
    $refreshedCategoryReady = @($refreshedStatus.querySources | Where-Object {
        [string]$_.category -eq $Category -and -not [string]::IsNullOrWhiteSpace([string]$_.contentHash)
    }).Count -gt 0
    if (-not [bool]$refreshedStatus.changeSetFresh -or -not $refreshedCategoryReady) {
        throw "SQLite Wiki projection '$Category' is still unavailable after backend-only refresh. Run ./.llm-wiki/wiki.ps1 graph-build after checking its runtime dependencies."
    }
}

function Invoke-LlmWikiSqliteQuery {
    [CmdletBinding()]
    param([Parameter(Mandatory)][hashtable]$Arguments)

    $manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
    $queryArguments = @{} + $Arguments
    $queryArguments.SkipRefresh = $true
    $queryArguments.Format = 'Json'
    $result = & $manager @queryArguments | ConvertFrom-Json
    if ([bool]$result.ready) { return $result }
    try {
        $null = & $manager -Action build -BackendOnlyRefresh -Format Json
    } catch {
        throw "SQLite query '$($queryArguments.Action)' could not prepare its projection. $($_.Exception.Message) Run ./.llm-wiki/wiki.ps1 graph-build and retry."
    }
    $result = & $manager @queryArguments | ConvertFrom-Json
    if (-not [bool]$result.ready) {
        throw "SQLite query '$($queryArguments.Action)' remains unavailable ($($result.unavailableReason)). Run ./.llm-wiki/wiki.ps1 update and graph-build, then retry."
    }
    return $result
}
