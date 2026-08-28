function Ensure-LlmWikiSqliteProjection {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('architecture-health', 'contracts', 'domain', 'frontend-contracts', 'risks', 'runtime', 'sensitive')]
        [string]$Category
    )

    $manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
    $status = & $manager -Action status -SkipRefresh -Format Json | ConvertFrom-Json
    $categoryReady = @($status.queryCategories | Where-Object {
        [string]$_.category -eq $Category -and [int]$_.count -gt 0
    }).Count -gt 0
    if ([bool]$status.changeSetFresh -and $categoryReady) { return }

    try {
        & $manager -Action build -BackendOnlyRefresh -Format Json | Out-Null
    } catch {
        throw "SQLite Wiki projection '$Category' could not be prepared. $($_.Exception.Message) Use explicit -CompiledIndexSource Json only when a read-only baseline is acceptable."
    }

    $refreshedStatus = & $manager -Action status -SkipRefresh -Format Json | ConvertFrom-Json
    $refreshedCategoryReady = @($refreshedStatus.queryCategories | Where-Object {
        [string]$_.category -eq $Category -and [int]$_.count -gt 0
    }).Count -gt 0
    if (-not [bool]$refreshedStatus.changeSetFresh -or -not $refreshedCategoryReady) {
        throw "SQLite Wiki projection '$Category' is still unavailable after backend-only refresh. Use explicit -CompiledIndexSource Json for a read-only baseline or run wiki.ps1 graph-build after installing frontend dependencies."
    }
}
